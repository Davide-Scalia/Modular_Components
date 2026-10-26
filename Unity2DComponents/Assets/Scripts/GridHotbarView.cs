using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ModularGrid
{
    /// <summary>Builds and displays a horizontally scrollable hotbar. It does not perform placement.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class GridHotbarView : MonoBehaviour
    {
        [SerializeField, Min(24f)] private float slotSize = 64f;
        [SerializeField, Min(0f)] private float slotSpacing = 6f;
        [SerializeField, Range(0f, 0.35f)] private float iconPadding = 0.12f;
        [SerializeField] private Color slotColor = new Color(0.16f, 0.16f, 0.16f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.2f, 1f);

        private readonly List<SlotVisual> slots = new List<SlotVisual>();
        private RectTransform content;
        private int selectedIndex = -1;

        public event Action<int> SelectionRequested;

        public void Build(IReadOnlyList<GridHotbarItem> items)
        {
            ClearSlots();
            EnsureEventSystem();
            BuildScrollView();

            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                GridHotbarItem item = items[i];
                if (item == null)
                    continue;

                slots.Add(CreateSlot(item, i));
            }

            RefreshHighlights();
        }

        public void SetSelected(int index)
        {
            selectedIndex = index;
            RefreshHighlights();
        }

        private void BuildScrollView()
        {
            ScrollRect scroll = GetComponent<ScrollRect>();
            if (scroll == null)
                scroll = gameObject.AddComponent<ScrollRect>();

            RectTransform root = (RectTransform)transform;
            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport, 0f, 16f, 0f, 0f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = slotSpacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            RectTransform scrollbarRect = CreateRect("Scrollbar", root);
            scrollbarRect.anchorMin = new Vector2(0f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 0f);
            scrollbarRect.pivot = new Vector2(0.5f, 0f);
            scrollbarRect.sizeDelta = new Vector2(0f, 14f);
            Image track = scrollbarRect.gameObject.AddComponent<Image>();
            track.color = new Color(0f, 0f, 0f, 0.35f);
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.LeftToRight;

            RectTransform slidingArea = CreateRect("Sliding Area", scrollbarRect);
            Stretch(slidingArea, 4f, 0f, 4f, 0f);
            RectTransform handle = CreateRect("Handle", slidingArea);
            Stretch(handle, 0f, 0f, 0f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.7f);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.horizontalScrollbar = scrollbar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbar = null;
        }

        private SlotVisual CreateSlot(GridHotbarItem item, int index)
        {
            RectTransform slotRect = CreateRect($"Slot {index + 1}", content);
            LayoutElement layout = slotRect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = slotSize;
            layout.preferredHeight = slotSize;

            Image background = slotRect.gameObject.AddComponent<Image>();
            background.color = slotColor;
            Button button = slotRect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.interactable = item.Footprint != null;

            RectTransform iconRect = CreateRect("Icon", slotRect);
            float padding = slotSize * iconPadding;
            Stretch(iconRect, padding, padding, padding, padding);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = item.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            SlotVisual visual = new SlotVisual(index, background, iconRect, icon);
            button.onClick.AddListener(() => SelectionRequested?.Invoke(index));
            SlotPointerEvents pointer = slotRect.gameObject.AddComponent<SlotPointerEvents>();
            pointer.Initialize(() => SetHovered(index, true), () => SetHovered(index, false));
            return visual;
        }

        private void SetHovered(int index, bool hovered)
        {
            SlotVisual slot = FindSlot(index);
            if (slot != null)
            {
                slot.Hovered = hovered;
                ApplyHighlight(slot);
            }
        }

        private SlotVisual FindSlot(int index)
        {
            foreach (SlotVisual slot in slots)
                if (slot.Index == index)
                    return slot;
            return null;
        }

        private void RefreshHighlights()
        {
            foreach (SlotVisual slot in slots)
                ApplyHighlight(slot);
        }

        private void ApplyHighlight(SlotVisual slot)
        {
            slot.Background.color = slot.Index == selectedIndex ? selectedColor : slot.Hovered ? hoverColor : slotColor;
            slot.IconRect.localScale = slot.Hovered ? Vector3.one * 1.08f : Vector3.one;
        }

        private void ClearSlots()
        {
            slots.Clear();
            ScrollRect oldScroll = GetComponent<ScrollRect>();
            if (oldScroll != null)
            {
                oldScroll.content = null;
                oldScroll.viewport = null;
                oldScroll.horizontalScrollbar = null;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            DontDestroyOnLoad(eventSystemObject);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private sealed class SlotVisual
        {
            public readonly int Index;
            public readonly Image Background;
            public readonly RectTransform IconRect;
            public readonly Image Icon;
            public bool Hovered;

            public SlotVisual(int index, Image background, RectTransform iconRect, Image icon)
            {
                Index = index;
                Background = background;
                IconRect = iconRect;
                Icon = icon;
            }
        }
    }

    public sealed class SlotPointerEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Action onEnter;
        private Action onExit;

        public void Initialize(Action enter, Action exit)
        {
            onEnter = enter;
            onExit = exit;
        }

        public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
    }
}
