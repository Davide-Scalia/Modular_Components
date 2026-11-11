using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RpgToolkit
{
    /// <summary>Creates one visual line and keeps it connected between two prefab-authored node views.</summary>
    public sealed class NodeMapLinkView : MonoBehaviour
    {
        [SerializeField] private NodeMapNodeView from;
        [SerializeField] private NodeMapNodeView to;
        [SerializeField, Min(1f)] private float thickness = 3f;
        [SerializeField] private Color color = new Color(0.5f, 0.65f, 0.75f, 1f);

        private RectTransform line;

        public NodeMapNodeView From => from;
        public NodeMapNodeView To => to;

        private void Awake()
        {
            if (from == null || to == null || from == to)
            {
                Debug.LogError($"{nameof(NodeMapLinkView)} on '{name}' needs different From/To node views.", this);
                enabled = false;
                return;
            }

            RectTransform linkRoot = FindSharedRectTransform(from.transform, to.transform);
            if (linkRoot == null)
            {
                Debug.LogError($"{nameof(NodeMapLinkView)} endpoints need a shared RectTransform ancestor.", this);
                enabled = false;
                return;
            }

            GameObject lineObject = new GameObject("Node Link Line", typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(linkRoot, false);
            line = (RectTransform)lineObject.transform;
            line.SetAsFirstSibling();
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0f, 0.5f);

            Image image = lineObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Connect();
        }

        private void LateUpdate() => Connect();

        private void OnDestroy()
        {
            if (line != null) Destroy(line.gameObject);
        }

        private void Connect()
        {
            if (line == null || from == null || to == null) return;
            RectTransform fromRect = from.transform as RectTransform;
            RectTransform toRect = to.transform as RectTransform;
            RectTransform linkRoot = line.parent as RectTransform;
            if (fromRect == null || toRect == null)
            {
                Debug.LogError($"{nameof(NodeMapLinkView)} endpoints must be attached to RectTransforms.", this);
                enabled = false;
                return;
            }

            Vector3 fromWorld = fromRect.TransformPoint(fromRect.rect.center);
            Vector3 toWorld = toRect.TransformPoint(toRect.rect.center);
            Vector2 start = linkRoot.InverseTransformPoint(fromWorld);
            Vector2 end = linkRoot.InverseTransformPoint(toWorld);
            Vector2 delta = end - start;

            line.localPosition = new Vector3(start.x, start.y, 0f);
            line.sizeDelta = new Vector2(delta.magnitude, thickness);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static RectTransform FindSharedRectTransform(Transform first, Transform second)
        {
            var secondAncestors = new HashSet<Transform>();
            for (Transform current = second; current != null; current = current.parent)
                secondAncestors.Add(current);
            for (Transform current = first; current != null; current = current.parent)
                if (secondAncestors.Contains(current) && current is RectTransform rect) return rect;
            return null;
        }
    }
}
