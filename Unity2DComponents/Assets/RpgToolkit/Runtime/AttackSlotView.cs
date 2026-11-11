using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RpgToolkit
{


    public class AttackSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color selectedColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color normalColor = new Color(0.25f, 0.39f, 0.56f);
        public event Action SelectionRequested;

        private void Awake()
        {
            if (button == null || image == null || label == null)
            {
                Debug.LogError($"{nameof(AttackSlotView)} on '{name}' needs Button, Image, and TextMeshProUGUI references from its prefab.", this);
                enabled = false;
                return;
            }
            button.onClick.AddListener(RequestSelection);
        }

        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(RequestSelection); }
        public void Present(AttackDefinition attack, bool actorCanAttack, bool selected, int index)
        {
            button.interactable = attack != null && actorCanAttack;
            image.sprite = attack != null ? attack.Icon : null;
            image.color = selected ? selectedColor : normalColor + new Color(index * 0.08f, 0f, 0f, 0f);
            label.text = attack != null ? attack.name : "Empty";
        }
        private void RequestSelection() => SelectionRequested?.Invoke();
    }
}
  
