using UnityEngine;
using UnityEngine.InputSystem;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellDemoInput2D : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpellCaster2D caster;
        [SerializeField] private SpellTrajectoryPreview2D preview;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private SpellDefinition[] spells;
        private int selected;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) selected = 0;
                else if (keyboard.digit2Key.wasPressedThisFrame) selected = 1;
                else if (keyboard.digit3Key.wasPressedThisFrame) selected = 2;
                else if (keyboard.digit4Key.wasPressedThisFrame) selected = 3;
            }

            Mouse mouse = Mouse.current;
            if (worldCamera == null || caster == null || preview == null || mouse == null ||
                spells == null || selected < 0 || selected >= spells.Length || spells[selected] == null) return;
            Vector2 origin = castOrigin != null ? castOrigin.position : transform.position;
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                -worldCamera.transform.position.z));
            Vector2 direction = (Vector2)world - origin;
            if (direction.sqrMagnitude <= 0.0001f) { preview.Hide(); return; }
            preview.Show(spells[selected], origin, direction.normalized);
            if (mouse.leftButton.wasPressedThisFrame)
                caster.TryCast(spells[selected], origin, direction.normalized);
        }

        private void OnDisable()
        {
            if (preview != null) preview.Hide();
        }

        private void OnGUI()
        {
            string spellName = spells != null && selected >= 0 && selected < spells.Length && spells[selected] != null
                ? spells[selected].name : "None";
            GUI.Label(new Rect(12f, 12f, 500f, 24f),
                $"Spells: 1 Piercing  2 Gravity Bounce  3 Homing  4 Timed Area | Selected: {spellName}");
            GUI.Label(new Rect(12f, 36f, 400f, 24f), "Aim with the mouse. Left click casts.");
        }
    }
}
