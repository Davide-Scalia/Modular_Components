using UnityEngine;

namespace RpgToolkit
{
    public enum CombatTeam { Player, Enemy }

    public sealed class CombatActor : MonoBehaviour
    {
        [SerializeField, Min(1)] private int velocity = 100;
        [SerializeField, Min(0)] private int maxMovement = 100;
        [SerializeField] private GridMover2D mover;
        [SerializeField] private CharacterInput2D input;
        [SerializeField] private FreeMover2D freeMover;
        [SerializeField] private PointClickMover2D pointClickMover;
        [SerializeField] private ExplorationMode explorationMode;
        [SerializeField] private CombatTeam team;
        [SerializeField] private AttackDefinition[] attacks = new AttackDefinition[4];
        private EntityStats stats;
        public CombatTeam Team => team;
        public CombatTimeline Timeline { get; private set; }
        public bool HasAttacked { get; private set; }
        public int Velocity => stats != null ? stats.Velocity : Mathf.Max(1, velocity);
        public int MaxMovement => stats != null ? stats.MaxMovement : Mathf.Max(0, maxMovement);
        public bool IsMoving => mover != null && mover.IsMoving;
        public AttackDefinition GetAttack(int index) => index >= 0 && index < 4 && attacks != null && index < attacks.Length ? attacks[index] : null;
        public bool TryMarkAttack()
        {
            if (HasAttacked || Timeline == null || Timeline.Current != this) return false;
            HasAttacked = true;
            return true;
        }

        private void Awake() => stats = GetComponent<EntityStats>();
        public void SetVelocity(int value) { if (stats != null) stats.SetVelocity(value); else velocity = Mathf.Max(1, value); }
        public void SetMaxMovement(int value) { if (stats != null) stats.SetMaxMovement(value); else maxMovement = Mathf.Max(0, value); }
        public void Configure(GridMover2D gridMover, CharacterInput2D adapter, ExplorationMode mode, int maxMove)
        {
            mover = gridMover;
            input = adapter;
            explorationMode = mode;
            maxMovement = Mathf.Max(0, maxMove);
            freeMover = GetComponent<FreeMover2D>();
            pointClickMover = GetComponent<PointClickMover2D>();
        }

        public void EnterCombat()
        {
            if (freeMover != null) freeMover.enabled = false;
            if (pointClickMover != null) pointClickMover.enabled = false;
            if (mover != null) { mover.enabled = true; mover.EnterCombat(); }
            if (input != null) { input.SetMode(ExplorationMode.Grid); input.enabled = false; }
        }
        public void BindTimeline(CombatTimeline value) => Timeline = value;
        public void Activate()
        {
            HasAttacked = false;
            if (mover != null) mover.BeginActivation(MaxMovement);
            if (input != null) input.enabled = team == CombatTeam.Player;
        }
        public void Deactivate()
        {
            if (mover != null) mover.EndActivation();
            if (input != null) input.enabled = false;
        }
        public void ExitCombat()
        {
            Timeline = null;
            if (mover != null) { mover.LeaveCombat(); mover.enabled = explorationMode == ExplorationMode.Grid; }
            if (input != null) { input.SetMode(explorationMode); input.enabled = true; }
            if (freeMover != null) freeMover.enabled = true;
            if (pointClickMover != null) pointClickMover.enabled = true;
        }
    }
}
