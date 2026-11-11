using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularGrid
{
    /// <summary>Displays catalog items through prefab-authored slot views.</summary>
    public sealed class GridHotbarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Assign every slot authored in the hotbar prefab. Add entries to support more catalog items.")]
        private List<GridHotbarSlotView> slots = new List<GridHotbarSlotView>();
        private int selectedIndex = -1;

        public event Action<int> SelectionRequested;

        private void Awake()
        {
            if (slots.Count == 0)
            {
                Debug.LogError($"{nameof(GridHotbarView)} on '{name}' needs prefab-authored slot references.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                {
                    Debug.LogError($"{nameof(GridHotbarView)} on '{name}' has an empty slot reference at index {i}.", this);
                    enabled = false;
                    return;
                }
                int index = i;
                slots[i].SelectionRequested += () => SelectionRequested?.Invoke(index);
            }
        }

        public void Build(IReadOnlyList<GridHotbarItem> items)
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].Present(items != null && i < items.Count ? items[i] : null, i == selectedIndex);

            if (items != null && items.Count > slots.Count)
                Debug.LogWarning($"Catalog has {items.Count} items but the prefab provides {slots.Count} slots. Add and assign more slot views.", this);
        }

        public void SetSelected(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetSelected(i == selectedIndex);
        }
    }
}
