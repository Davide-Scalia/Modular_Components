using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RpgToolkit
{
    /// <summary>Presentation and button input for one scene-authored map node.</summary>
    public sealed class NodeMapNodeView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color currentColor = new Color(0.3f, 0.7f, 0.8f);
        [SerializeField] private Color reachableColor = new Color(0.25f, 0.65f, 0.35f);
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.32f, 0.38f);
        private NodeMapNavigator navigator;
        private MapNode node;

        private void Awake()
        {
            node = GetComponent<MapNode>();
            if (node == null || button == null || image == null || label == null)
            {
                Debug.LogError($"{nameof(NodeMapNodeView)} on '{name}' needs a MapNode on the same object and Button, Image, and TextMeshProUGUI references.", this);
                enabled = false;
                return;
            }
            button.onClick.AddListener(SelectNode);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(SelectNode);
        }

        public void Bind(NodeMapNavigator owner)
        {
            navigator = owner;
            if (node == null) node = GetComponent<MapNode>();
            if (node == null || navigator == null || button == null || image == null || label == null)
            {
                Debug.LogError($"Node view '{name}' needs a scene MapNode, navigator, Button, Image, and TextMeshProUGUI.", this);
                enabled = false;
                return;
            }
            label.text = node.Label;
        }

        public void Present(MapNode current)
        {
            if (!enabled || node == null || current == null) return;
            bool isCurrent = node == current;
            bool reachable = current.ConnectsTo(node);
            image.color = isCurrent ? currentColor : reachable ? reachableColor : lockedColor;
            button.interactable = reachable;
        }

        private void SelectNode()
        {
            if (navigator != null) navigator.TrySelect(node);
        }
    }
}
