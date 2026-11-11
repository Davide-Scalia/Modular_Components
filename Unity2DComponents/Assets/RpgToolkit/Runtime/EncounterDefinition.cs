using UnityEngine;
using UnityEngine.Serialization;

namespace RpgToolkit
{
    public enum EncounterKind { Combat, Event, Rest }

    [CreateAssetMenu(menuName = "RPG Toolkit/Encounter")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [SerializeField] private EncounterKind kind;
        [FormerlySerializedAs("arenaSceneName")]
        [SerializeField] private string scenePath;
        [SerializeField] private LootPool lootPool;
        public EncounterKind Kind => kind;
        public string ScenePath => scenePath;
        public LootPool LootPool => lootPool;
    }
}
