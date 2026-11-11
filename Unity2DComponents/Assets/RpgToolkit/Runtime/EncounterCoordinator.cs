using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RpgToolkit
{
    /// <summary>Keeps the map scene alive while an encounter scene is open.</summary>
    public sealed class EncounterCoordinator : MonoBehaviour
    {
        [SerializeField] private NodeMapNavigator navigator;
        [SerializeField] private NodeMapPresenter presenter;
        private readonly List<Camera> mapCameras = new List<Camera>();
        private readonly List<bool> cameraStates = new List<bool>();
        private Scene mapScene;
        private Scene encounterScene;
        private string destinationPath;
        private MapNode previousNode;
        private EncounterDefinition currentEncounter;
        private ArenaSetup arena;
        private bool rewardsRolled;
        private bool returning;

        private void OnEnable()
        {
            if (navigator != null) navigator.EncounterRequested += Enter;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            if (navigator != null) navigator.EncounterRequested -= Enter;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Enter(EncounterDefinition encounter, MapNode previous)
        {
            if (encounter == null || navigator == null || presenter == null || returning || !string.IsNullOrEmpty(destinationPath))
                return;
            currentEncounter = encounter;
            rewardsRolled = false;
            string destination = encounter.ScenePath;
            if (string.IsNullOrWhiteSpace(destination))
            {
                GrantRewards();
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                navigator.Restore(previous);
                Debug.LogError($"Encounter scene '{destination}' must be included in Build Settings.", this);
                return;
            }

            mapScene = gameObject.scene;
            previousNode = previous;
            destinationPath = destination;
            CaptureAndDisableMapCameras();
            presenter.SetVisible(false);
            if (SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive) != null) return;

            Debug.LogError($"Could not load encounter scene '{destination}'.", this);
            destinationPath = null;
            navigator.Restore(previous);
            RestoreMap();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            navigator = FindAnyObjectByType<NodeMapNavigator>();
            presenter = FindAnyObjectByType<NodeMapPresenter>();
            if (scene.path != destinationPath) return;
            encounterScene = scene;
            SceneManager.SetActiveScene(scene);
            if (currentEncounter != null && currentEncounter.Kind != EncounterKind.Combat)
            {
                GrantRewards();
                ReturnToMap(false);
                return;
            }
            arena = FindAnyObjectByType<ArenaSetup>();
            if (arena == null || !arena.Begin())
            {
                Debug.LogError("Encounter scene needs a valid ArenaSetup; returning to the map.", this);
                navigator.Restore(previousNode);
                ReturnToMap(false);
            }
            else arena.CombatEnded += ReturnToMap;
        }

        public void ReturnToMap(bool victory = true)
        {
            if (string.IsNullOrEmpty(destinationPath) || returning || !encounterScene.IsValid()) return;
            if (victory) GrantRewards();
            if (arena != null) arena.CombatEnded -= ReturnToMap;
            arena?.End();
            arena = null;
            returning = true;
            SceneManager.SetActiveScene(mapScene);
            if (SceneManager.UnloadSceneAsync(encounterScene) != null) return;

            Debug.LogError("Could not unload encounter scene.", this);
            returning = false;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (!returning || scene != encounterScene) return;
            returning = false;
            encounterScene = default;
            destinationPath = null;
            RestoreMap();
        }

        private void CaptureAndDisableMapCameras()
        {
            mapCameras.Clear();
            cameraStates.Clear();
            foreach (GameObject root in mapScene.GetRootGameObjects())
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                {
                    mapCameras.Add(camera);
                    cameraStates.Add(camera.enabled);
                    camera.enabled = false;
                }
        }

        private void RestoreMap()
        {
            for (int i = 0; i < mapCameras.Count; i++)
                if (mapCameras[i] != null) mapCameras[i].enabled = cameraStates[i];
            mapCameras.Clear();
            cameraStates.Clear();
            if (presenter != null) presenter.SetVisible(true);
        }

        private void GrantRewards()
        {
            if (rewardsRolled || currentEncounter == null) return;
            rewardsRolled = true;
            if (currentEncounter.LootPool != null)
                RunLootLedger.Instance.Add(currentEncounter.LootPool.Roll(new System.Random()));
        }
    }
}
