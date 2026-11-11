using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularGrid
{
    [Serializable]
    public sealed class GridHotbarItem
    {
        public GridFootprint Footprint;
        public Sprite Icon;
    }

    [CreateAssetMenu(fileName = "Grid Hotbar Catalog", menuName = "Modular Grid/Hotbar Catalog")]
    public sealed class GridHotbarCatalog : ScriptableObject
    {
        [SerializeField] private List<GridHotbarItem> items = new List<GridHotbarItem>();

        public IReadOnlyList<GridHotbarItem> Items => items;
    }
}
