using System.Collections.Generic;
using ModularGrid;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RpgToolkit
{
    // Demo controller for a prefab-authored arena UI.
    public sealed class ArenaDemoControls : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private CombatTimeline timeline;
        [SerializeField] private EncounterCoordinator coordinator;
        [SerializeField] private GameplayGrid2D grid;
        [SerializeField] private CombatActor[] actors;

        [Header("Prefab-authored View (all references are required)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private TextMeshProUGUI status;
        [SerializeField] private TextMeshProUGUI feedback;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button retreatButton;
        [SerializeField, Tooltip("One prefab-authored view per attack slot.")]
        private List<AttackSlotView> attackSlots = new List<AttackSlotView>();
        [SerializeField, Tooltip("One prefab-authored health view per actor, in actor order.")]
        private List<HealthVisual> healthViews = new List<HealthVisual>();
        [SerializeField, Tooltip("Legacy prefab overlays; hidden while the arena uses grid cell drawing.")]
        private List<Image> overlays = new List<Image>();

        private CombatActor current;
        private GridMover2D mover;
        private int selectedSlot = -1;
        private Vector2Int facing = Vector2Int.right;
        private bool viewReady;

        private void Awake()
        {
            viewReady = ValidateView();
            if (!viewReady) { enabled = false; return; }
            HideOverlays(0);
            waitButton.onClick.AddListener(Wait);
            itemButton.onClick.AddListener(UseItem);
            retreatButton.onClick.AddListener(Retreat);
            for (int i = 0; i < attackSlots.Count; i++)
            {
                int slot = i;
                attackSlots[i].SelectionRequested += () => SelectAttack(slot);
            }
            int healthCount = Mathf.Min(actors != null ? actors.Length : 0, healthViews.Count);
            for (int i = 0; i < healthCount; i++)
                if (actors[i] != null) healthViews[i].Bind(actors[i].GetComponent<Health>());
        }

        private bool ValidateView()
        {
            if (canvas == null || status == null || feedback == null || waitButton == null || itemButton == null || retreatButton == null)
            {
                Debug.LogError($"{nameof(ArenaDemoControls)} on '{name}' needs Canvas, labels, and action Button references from the UI prefab.", this);
                return false;
            }
            for (int i = 0; i < attackSlots.Count; i++)
                if (attackSlots[i] == null) { Debug.LogError($"Arena attack slot reference {i} is empty.", this); return false; }
            return true;
        }

        private void Start()
        {
            if (coordinator == null) coordinator = FindAnyObjectByType<EncounterCoordinator>();
            RefreshAttacks();
        }

        private void OnEnable()
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            if (timeline != null) timeline.ActorActivated += OnActorActivated;
        }
        private void OnDisable()
        {
            if (timeline != null) timeline.ActorActivated -= OnActorActivated;
            if (mover != null) mover.DestinationBlocked -= OnBlocked;
            mover = null;
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (!viewReady) return;
            waitButton.onClick.RemoveListener(Wait);
            itemButton.onClick.RemoveListener(UseItem);
            retreatButton.onClick.RemoveListener(Retreat);
        }

        private void Wait() { if (current != null && current.Team == CombatTeam.Player) timeline.Submit(current, 100, 0, null); }
        private void UseItem() { if (current != null && current.Team == CombatTeam.Player) timeline.Submit(current, 25, 0, () => Debug.Log("Demo item used")); }
        private void Retreat() { if (coordinator != null) coordinator.ReturnToMap(false); }
        private void OnBlocked() => feedback.text = "Destination blocked or beyond movement budget";

        private void OnActorActivated(CombatActor actor)
        {
            current = actor;
            selectedSlot = -1;
            RefreshAttacks();
        }

        private void Update()
        {
            if (!viewReady || timeline == null || grid == null) return;
            CombatActor actor = timeline.Current;
            GridMover2D activeMover = actor != null ? actor.GetComponent<GridMover2D>() : null;
            if (activeMover != mover)
            {
                if (mover != null) mover.DestinationBlocked -= OnBlocked;
                mover = activeMover;
                if (mover != null) mover.DestinationBlocked += OnBlocked;
            }
            if (actor != current) { current = actor; selectedSlot = -1; RefreshAttacks(); }
            status.text = actor == null ? "Combat ended" : $"Time {timeline.Time}  |  {actor.name}  |  Move {mover.RemainingMovement}";
            Camera camera = Camera.main;
            Mouse mouse = Mouse.current;
            if (camera == null || mouse == null || actor == null || mover == null || actor.Team != CombatTeam.Player) return;
            Vector2 screen = mouse.position.ReadValue();
            Vector3 pointer = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            if (selectedSlot >= 0 && !actor.HasAttacked)
            {
                Vector2 delta = screen - (Vector2)camera.WorldToScreenPoint(grid.CellToWorld(mover.Cell));
                if (delta.sqrMagnitude > 25f)
                    facing = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? new Vector2Int(delta.x >= 0 ? 1 : -1, 0) : new Vector2Int(0, delta.y >= 0 ? 1 : -1);
            }
            if (!mouse.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            if (selectedSlot >= 0)
            {
                AttackExecutor executor = actor.GetComponent<AttackExecutor>();
                if (executor == null || !executor.TryUse(selectedSlot, facing)) feedback.text = "Attack unavailable";
                else { selectedSlot = -1; feedback.text = ""; RefreshAttacks(); }
            }
            else if (!mover.IsMoving) { feedback.text = ""; actor.GetComponent<CharacterIntent>().GoTo(pointer); }
        }

        private void SelectAttack(int slot)
        {
            if (current == null || current.Team != CombatTeam.Player || current.HasAttacked || current.IsMoving || current.GetAttack(slot) == null) return;
            selectedSlot = selectedSlot == slot ? -1 : slot;
            feedback.text = selectedSlot < 0 ? "" : "Aim with cursor; click arena to attack";
            RefreshAttacks();
        }

        private void RefreshAttacks()
        {
            bool playerTurn = current != null && current.Team == CombatTeam.Player;
            waitButton.interactable = playerTurn;
            itemButton.interactable = playerTurn;
            for (int i = 0; i < attackSlots.Count; i++)
            {
                AttackDefinition attack = current != null ? current.GetAttack(i) : null;
                attackSlots[i].Present(attack, playerTurn && !current.HasAttacked, i == selectedSlot, i);
            }
        }

        private void OnGUI()
        {
            if (!viewReady || grid == null || current == null || current.Team != CombatTeam.Player || mover == null || Event.current.type != EventType.Repaint)
                return;
            Camera camera = Camera.main;
            Mouse mouse = Mouse.current;
            if (camera == null || mouse == null) return;
            if (selectedSlot >= 0 && !current.HasAttacked)
                DrawAttack(current.GetAttack(selectedSlot), mover.Cell, camera);
            else
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector3 pointer = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
                DrawMovement(pointer, camera);
            }
        }

        private void DrawAttack(AttackDefinition attack, GridCoordinate origin, Camera camera)
        {
            var cells = new HashSet<GridCoordinate>();
            if (attack != null && attack.PreparationHits > 0 && attack.PreparationPattern != null)
                foreach (GridCoordinate cell in attack.PreparationPattern.Cells(origin, facing)) cells.Add(cell);
            if (attack != null && attack.CompletionPattern != null)
                foreach (GridCoordinate cell in attack.CompletionPattern.Cells(origin, facing)) cells.Add(cell);
            foreach (GridCoordinate cell in cells) DrawCell(cell, new Color(0.95f, 0.08f, 0.08f, 0.65f), camera);
        }

        private void DrawMovement(Vector3 pointer, Camera camera)
        {
            Dictionary<GridCoordinate, int> reachable = GridPathfinder.Reachable(grid, mover.Cell, mover.RemainingMovement);
            GridCoordinate hovered = grid.WorldToCell(pointer);
            List<GridCoordinate> path = reachable.ContainsKey(hovered) ? GridPathfinder.Path(grid, mover.Cell, hovered, mover.RemainingMovement) : null;
            foreach (GridCoordinate cell in reachable.Keys)
            {
                if (cell == mover.Cell) continue;
                DrawCell(cell, path != null && path.Contains(cell) ? new Color(1f, 0.75f, 0.1f, 0.45f) : new Color(0.2f, 0.9f, 0.5f, 0.25f), camera);
            }
        }

        private void DrawCell(GridCoordinate cell, Color color, Camera camera)
        {
            Vector3 center = camera.WorldToScreenPoint(grid.CellToWorld(cell));
            Vector3 east = camera.WorldToScreenPoint(grid.CellToWorld(new GridCoordinate(cell.X + 1, cell.Y)));
            Vector3 north = camera.WorldToScreenPoint(grid.CellToWorld(new GridCoordinate(cell.X, cell.Y + 1)));
            float width = Mathf.Abs(east.x - center.x) * 0.8f;
            float height = Mathf.Abs(north.y - center.y) * 0.8f;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - width * 0.5f,
                Screen.height - center.y - height * 0.5f, width, height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void HideOverlays(int count)
        {
            for (int i = count; i < overlays.Count; i++)
                if (overlays[i] != null) overlays[i].gameObject.SetActive(false);
        }
    }
}
