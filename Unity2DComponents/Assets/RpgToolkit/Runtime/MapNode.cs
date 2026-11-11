using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    /// <summary>A map location configured on a scene object.</summary>
    public sealed class MapNode : MonoBehaviour
    {
        [SerializeField] private string label;
        [SerializeField] private EncounterDefinition encounter;
        [SerializeField] private List<MapNode> links = new List<MapNode>();

        public string Label => string.IsNullOrWhiteSpace(label) ? name : label;
        public EncounterDefinition Encounter => encounter;
        public IReadOnlyList<MapNode> Links => links;

        public bool ConnectsTo(MapNode node) => node != null && links.Contains(node);
    }
}
