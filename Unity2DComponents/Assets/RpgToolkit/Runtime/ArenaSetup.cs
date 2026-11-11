using System;
using UnityEngine;

namespace RpgToolkit
{
    public sealed class ArenaSetup : MonoBehaviour
    {
        [SerializeField] private GameplayGrid2D grid;
        [SerializeField] private GridMover2D[] gridActors;
        [SerializeField] private CombatActor[] combatActors;
        [SerializeField] private CombatTimeline timeline;
        private bool begun;
        public event Action<bool> CombatEnded;

        private void Start()
        {
            if (!begun && !Begin()) Debug.LogError("Arena setup is incomplete.", this);
        }

        public bool Begin()
        {
            if (begun) return true;
            if (grid == null || timeline == null || gridActors == null || combatActors == null ||
                gridActors.Length == 0 || combatActors.Length == 0) return false;
            for (int i = 0; i < gridActors.Length; i++)
                if (gridActors[i] == null || !gridActors[i].Bind(grid))
                {
                    for (int j = 0; j < i; j++) gridActors[j].Bind(null);
                    return false;
                }
            foreach (CombatActor actor in combatActors)
            {
                Health health = actor != null ? actor.GetComponent<Health>() : null;
                if (health != null) health.Died += OnActorDied;
            }
            timeline.Begin(combatActors);
            foreach (CombatActor actor in combatActors)
            {
                AttackExecutor attacks = actor != null ? actor.GetComponent<AttackExecutor>() : null;
                if (attacks != null) attacks.Bind(grid, timeline);
            }
            if (timeline.Current != null)
            {
                begun = true;
                return true;
            }
            End();
            return false;
        }

        private void OnActorDied()
        {
            bool playersAlive = false, enemiesAlive = false;
            foreach (CombatActor actor in combatActors)
            {
                if (actor == null || actor.GetComponent<Health>() is not Health health || !health.IsAlive) continue;
                if (actor.Team == CombatTeam.Player) playersAlive = true;
                else enemiesAlive = true;
            }
            if (!playersAlive || !enemiesAlive) CombatEnded?.Invoke(playersAlive);
        }

        public void End()
        {
            begun = false;
            if (combatActors != null)
                foreach (CombatActor actor in combatActors)
                {
                    Health health = actor != null ? actor.GetComponent<Health>() : null;
                    if (health != null) health.Died -= OnActorDied;
                }
            if (timeline != null) timeline.End();
            if (combatActors != null)
                foreach (CombatActor actor in combatActors)
                {
                    AttackExecutor attacks = actor != null ? actor.GetComponent<AttackExecutor>() : null;
                    if (attacks != null) attacks.Bind(null, null);
                }
            if (gridActors == null) return;
            foreach (GridMover2D actor in gridActors)
                if (actor != null) actor.Bind(null);
        }
    }
}
