using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RpgToolkit
{
    /// <summary>Presents the scene node graph through prefab-authored views.</summary>
    public sealed class NodeMapPresenter : MonoBehaviour
    {
        [SerializeField] private NodeMapNavigator navigator;
        [SerializeField] private Canvas canvas;
        [SerializeField] private EventSystem input;
        [SerializeField] private TextMeshProUGUI lootLabel;
        private bool viewReady;

        private void Awake()
        {
            viewReady = navigator != null && canvas != null && input != null && lootLabel != null;
            if (!viewReady)
            {
                Debug.LogError($"{nameof(NodeMapPresenter)} on '{name}' needs Navigator, Canvas, EventSystem, and Loot Label references.", this);
                enabled = false;
                return;
            }
            foreach (MapNode node in navigator.Nodes)
            {
                NodeMapNodeView view = node != null ? node.GetComponent<NodeMapNodeView>() : null;
                if (view == null)
                {
                    Debug.LogError($"Scene node '{(node == null ? "Missing" : node.name)}' needs a NodeMapNodeView on the same object.", this);
                    viewReady = false;
                    enabled = false;
                    return;
                }
                view.Bind(navigator);
            }
        }

        private void OnEnable()
        {
            if (!viewReady) return;
            navigator.NodeSelected += OnSelected;
            Refresh();
        }

        private void OnDisable()
        {
            if (navigator != null) navigator.NodeSelected -= OnSelected;
        }

        public void SetVisible(bool visible)
        {
            if (!viewReady) return;
            canvas.enabled = visible;
            input.enabled = visible;
            if (visible) Refresh();
        }

        private void OnSelected(MapNode node) => Refresh();

        private void Refresh()
        {
            if (navigator.Current == null) return;
            foreach (MapNode node in navigator.Nodes)
                if (node != null) node.GetComponent<NodeMapNodeView>().Present(navigator.Current);
            lootLabel.text = BuildLootText();
        }

        private static string BuildLootText()
        {
            if (RunLootLedger.Instance.Drops.Count == 0) return "Loot: none";
            var names = new List<string>();
            foreach (LootDrop drop in RunLootLedger.Instance.Drops)
                if (drop.Item != null) names.Add(drop.Item.DisplayName + " x" + drop.Count);
            return "Loot: " + string.Join(", ", names);
        }
    }
}
