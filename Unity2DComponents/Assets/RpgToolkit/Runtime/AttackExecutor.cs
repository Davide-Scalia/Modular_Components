using System;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    [RequireComponent(typeof(CombatActor), typeof(EntityStats), typeof(Health))]
    [RequireComponent(typeof(GridMover2D))]
    public sealed class AttackExecutor : MonoBehaviour
    {
        private CombatActor actor;
        private EntityStats stats;
        private GameplayGrid2D grid;
        private CombatTimeline timeline;
        public event Action<AttackDefinition, bool> PhaseResolved;

        private void Awake()
        {
            actor = GetComponent<CombatActor>();
            stats = GetComponent<EntityStats>();
        }

        public void Bind(GameplayGrid2D combatGrid, CombatTimeline combatTimeline)
        {
            grid = combatGrid;
            timeline = combatTimeline;
        }

        public bool TryUse(int slot, Vector2Int direction)
        {
            if (grid == null || timeline == null || actor == null || stats == null || actor.IsMoving ||
                timeline.Current != actor || actor.HasAttacked || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
                return false;
            AttackDefinition attack = actor.GetAttack(slot);
            if (attack == null || attack.CompletionPattern == null ||
                (attack.PreparationHits > 0 && attack.PreparationPattern == null)) return false;
            if (!actor.TryMarkAttack()) return false;

            if (!timeline.SpendTime(actor, attack.PreparationCost) || timeline.Current != actor) return true;
            Resolve(attack, attack.PreparationPattern, attack.PreparationHits, direction);
            if (grid == null || timeline.Current != actor) return true;
            PhaseResolved?.Invoke(attack, true);
            if (timeline.Current != actor) return true;
            if (!timeline.SpendTime(actor, attack.CompletionCost) || timeline.Current != actor) return true;
            Resolve(attack, attack.CompletionPattern, attack.CompletionHits, direction);
            if (grid == null || timeline.Current != actor) return true;
            PhaseResolved?.Invoke(attack, false);
            if (grid != null && timeline.Current == actor && GetComponent<GridMover2D>().RemainingMovement == 0)
                timeline.FinishActivation(actor);
            return true;
        }

        private void Resolve(AttackDefinition attack, AttackPattern pattern, int hits, Vector2Int facing)
        {
            if (pattern == null || hits <= 0) return;
            GridCoordinate origin = GetComponent<GridMover2D>().Cell;
            for (int hit = 0; hit < hits; hit++)
                foreach (GridCoordinate cell in pattern.Cells(origin, facing))
                {
                        if (grid == null || timeline == null || timeline.Current != actor) return;
                        if (!grid.TryGetOccupant(cell, out Component occupant) || occupant == null) continue;
                        CombatActor target = occupant.GetComponent<CombatActor>();
                        if (target != null && target.Team == actor.Team) continue;
                        IDamageable damageable = occupant.GetComponent<IDamageable>();
                        if (damageable == null || !damageable.IsAlive) continue;
                        EntityStats targetStats = occupant.GetComponent<EntityStats>();
                        int power = attack.DamageKind == DamageKind.Physical ? stats.Attack : stats.SpecialAttack;
                        int defense = targetStats == null ? 0 : attack.DamageKind == DamageKind.Physical
                            ? targetStats.Defense : targetStats.SpecialDefense;
                        float amount = power * attack.ScalingPercent / 100f * 100f / (100f + defense);
                        if (UnityEngine.Random.Range(0, 100) < stats.CritRate) amount *= stats.CritDamage / 100f;
                        if (attack.ScalingPercent > 0) damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount)));
                        if (grid == null || timeline.Current != actor) return;
                }
        }
    }
}
