using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularGrid
{
    public sealed class GridHotbarSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform iconTransform;
        [SerializeField] private Color normalColor = new Color(0.16f, 0.16f, 0.16f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.2f, 1f);
        private bool hovered;
        private bool selected;
        public event Action SelectionRequested;

        private void Awake()
        {
            if (button == null || background == null || icon == null || iconTransform == null)
            {
                Debug.LogError($"{nameof(GridHotbarSlotView)} on '{name}' needs Button, Background, Icon, and Icon Transform prefab references.", this);
                enabled = false;
                return;
            }
            button.onClick.AddListener(RequestSelection);
        }

        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(RequestSelection); }

        public void Present(GridHotbarItem item, bool isSelected)
        {
            gameObject.SetActive(item != null);
            if (item == null) return;
            icon.sprite = item.Icon;
            button.interactable = item.Footprint != null;
            SetSelected(isSelected);
        }

        public void SetSelected(bool value) { selected = value; Refresh(); }
        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
        private void RequestSelection() => SelectionRequested?.Invoke();
        private void Refresh()
        {
            if (background == null || iconTransform == null) return;
            background.color = selected ? selectedColor : hovered ? hoverColor : normalColor;
            iconTransform.localScale = hovered ? Vector3.one * 1.08f : Vector3.one;
        }
    }
}
