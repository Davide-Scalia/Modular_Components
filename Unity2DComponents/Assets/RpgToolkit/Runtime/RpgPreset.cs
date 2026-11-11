using UnityEngine;
using UnityEngine.InputSystem;

namespace RpgToolkit
{
    public enum ExplorationMode { Free, PointAndClick, Grid, NodeMap }

    [CreateAssetMenu(menuName = "RPG Toolkit/Character Preset")]
    public sealed class RpgPreset : ScriptableObject
    {
        [SerializeField] private string description;
        [SerializeField] private Sprite preview;
        [SerializeField] private ExplorationMode explorationMode = ExplorationMode.Free;
        [SerializeField] private bool tacticalCombat = true;
        [SerializeField, Min(0.01f)] private float movementSpeed = 4f;
        [SerializeField, Min(0)] private int maxMovement = 100;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private InputActionAsset inputActions;

        public string Description => description;
        public Sprite Preview => preview;
        public ExplorationMode Mode => explorationMode;
        public bool TacticalCombat => tacticalCombat;
        public float MovementSpeed => movementSpeed;
        public int MaxMovement => maxMovement;
        public Sprite CharacterSprite => characterSprite;
        public InputActionAsset InputActions => inputActions;
    }
}
