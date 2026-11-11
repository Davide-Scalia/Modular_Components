using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    /// <summary>Validates movement through scene-authored node references.</summary>
    public sealed class NodeMapNavigator : MonoBehaviour
    {
        [SerializeField] private MapNode startNode;
        [SerializeField] private List<MapNode> nodes = new List<MapNode>();
        private MapNode currentNode;

        public IReadOnlyList<MapNode> Nodes => nodes;
        public MapNode Current => currentNode;
        public event Action<EncounterDefinition, MapNode> EncounterRequested;
        public event Action<MapNode> NodeSelected;

        private void Awake()
        {
            if (startNode == null || !Contains(startNode) || nodes.Count == 0)
            {
                Debug.LogError($"{nameof(NodeMapNavigator)} on '{name}' needs a Start Node included in its scene node list.", this);
                enabled = false;
                return;
            }
            var unique = new HashSet<MapNode>();
            foreach (MapNode node in nodes)
            {
                if (node == null || node.gameObject.scene != gameObject.scene || !unique.Add(node))
                {
                    Debug.LogError($"Node list on '{name}' contains a missing, duplicate, or cross-scene node.", this);
                    enabled = false;
                    return;
                }
                foreach (MapNode link in node.Links)
                    if (link == null || !Contains(link))
                    {
                        Debug.LogError($"Node '{node.name}' has a link outside the navigator's scene node list.", node);
                        enabled = false;
                        return;
                    }
            }
            currentNode = startNode;
        }

        public bool Contains(MapNode node) => node != null && nodes.Contains(node);

        public bool TrySelect(MapNode target)
        {
            if (!enabled || !Contains(target) || !Contains(currentNode) || !currentNode.ConnectsTo(target)) return false;
            MapNode previous = currentNode;
            currentNode = target;
            NodeSelected?.Invoke(target);
            if (target.Encounter != null) EncounterRequested?.Invoke(target.Encounter, previous);
            return true;
        }

        public void Restore(MapNode node)
        {
            if (Contains(node))
            {
                currentNode = node;
                NodeSelected?.Invoke(node);
            }
        }
    }
}
