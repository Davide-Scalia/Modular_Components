using System.Collections;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    // Demo opponent: approach the nearest player, use an attack that covers them, then yield.
    public sealed class ArenaDemoEnemyTurns : MonoBehaviour
    {
        [SerializeField] private CombatTimeline timeline;
        [SerializeField] private GameplayGrid2D grid;
        [SerializeField] private CombatActor[] actors;

        private static readonly Vector2Int[] Directions =
            { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private void OnEnable()
        {
            if (timeline != null) timeline.ActorActivated += OnActorActivated;
        }

        private void OnDisable()
        {
            if (timeline != null) timeline.ActorActivated -= OnActorActivated;
        }

        private void OnActorActivated(CombatActor actor)
        {
            if (actor != null && actor.Team == CombatTeam.Enemy)
                StartCoroutine(TakeTurn(actor));
        }

        private IEnumerator TakeTurn(CombatActor actor)
        {
            yield return null; // Let the timeline finish activating the actor before acting.
            if (timeline == null || grid == null || timeline.Current != actor) yield break;

            GridMover2D mover = actor.GetComponent<GridMover2D>();
            if (mover == null) { timeline.FinishActivation(actor); yield break; }

            if (!TryAttack(actor, mover.Cell))
            {
                GridCoordinate destination = ClosestReachableCell(mover);
                if (destination != mover.Cell)
                {
                    actor.GetComponent<CharacterIntent>().GoTo(grid.CellToWorld(destination));
                    while (timeline.Current == actor && mover.IsMoving) yield return null;
                }
                if (timeline.Current == actor) TryAttack(actor, mover.Cell);
            }
            if (timeline.Current == actor) timeline.FinishActivation(actor);
        }

        private bool TryAttack(CombatActor actor, GridCoordinate origin)
        {
            AttackExecutor executor = actor.GetComponent<AttackExecutor>();
            if (executor == null || actor.HasAttacked || actors == null) return false;
            for (int slot = 0; slot < 4; slot++)
            {
                AttackDefinition attack = actor.GetAttack(slot);
                if (attack == null || attack.CompletionPattern == null) continue;
                foreach (Vector2Int direction in Directions)
                    foreach (GridCoordinate cell in attack.CompletionPattern.Cells(origin, direction))
                        foreach (CombatActor target in actors)
                        {
                            Health health = target != null ? target.GetComponent<Health>() : null;
                            if (target == null || target.Team == actor.Team || health == null || !health.IsAlive) continue;
                            GridMover2D targetMover = target.GetComponent<GridMover2D>();
                            if (targetMover != null && targetMover.Cell == cell)
                                return executor.TryUse(slot, direction);
                        }
            }
            return false;
        }

        private GridCoordinate ClosestReachableCell(GridMover2D mover)
        {
            GridCoordinate best = mover.Cell;
            int bestDistance = DistanceToPlayer(best);
            int bestCost = 0;
            foreach (var pair in GridPathfinder.Reachable(grid, best, mover.RemainingMovement))
            {
                int distance = DistanceToPlayer(pair.Key);
                if (distance > bestDistance || (distance == bestDistance && pair.Value >= bestCost)) continue;
                best = pair.Key;
                bestDistance = distance;
                bestCost = pair.Value;
            }
            return best;
        }

        private int DistanceToPlayer(GridCoordinate cell)
        {
            int distance = int.MaxValue;
            if (actors == null) return distance;
            foreach (CombatActor target in actors)
            {
                Health health = target != null ? target.GetComponent<Health>() : null;
                if (target == null || target.Team != CombatTeam.Player || health == null || !health.IsAlive) continue;
                GridMover2D mover = target.GetComponent<GridMover2D>();
                if (mover != null)
                    distance = Mathf.Min(distance, Mathf.Abs(cell.X - mover.Cell.X) + Mathf.Abs(cell.Y - mover.Cell.Y));
            }
            return distance;
        }
    }
}
