using UnityEngine;

namespace RpgToolkit
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Loot Item")]
    public sealed class LootItem : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
    }
}
