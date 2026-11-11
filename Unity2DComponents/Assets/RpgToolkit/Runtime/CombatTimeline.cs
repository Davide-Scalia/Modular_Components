using System;
using System.Collections.Generic;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    // Owns logical time and actor readiness. Action effects stay with their submitting components.
    public sealed class CombatTimeline : MonoBehaviour
    {
        public struct GridStepEvent
        {
            public CombatActor Actor;
            public GridCoordinate From;
            public GridCoordinate To;
            public int TerrainCost;
            public long Time;
        }
        [SerializeField] private CombatActor[] startingActors;
        [SerializeField, Min(1)] private int speedScale = 100;
        private readonly Dictionary<CombatActor, long> readyAt = new Dictionary<CombatActor, long>();
        private readonly List<CombatActor> actorOrder = new List<CombatActor>();
        private readonly List<ScheduledEffect> effects = new List<ScheduledEffect>();
        private readonly Dictionary<GridMover2D, Action<GridCoordinate, GridCoordinate, int>> stepHandlers =
            new Dictionary<GridMover2D, Action<GridCoordinate, GridCoordinate, int>>();
        private long nextSequence;
        private long clock;
        private bool advancingTime;

        private struct ScheduledEffect
        {
            public long Time;
            public long Sequence;
            public Action Effect;
        }

        public long Time => clock;
        public CombatActor Current { get; private set; }
        public event Action<CombatActor> ActorActivated;
        public event Action<GridStepEvent> GridStepResolved;

        public void Begin(CombatActor[] actors = null)
        {
            End();
            clock = 0;
            nextSequence = 0;
            foreach (CombatActor actor in actors ?? startingActors)
            {
                if (actor == null || readyAt.ContainsKey(actor)) continue;
                readyAt.Add(actor, 0);
                actorOrder.Add(actor);
                actor.BindTimeline(this);
                actor.EnterCombat();
                GridMover2D mover = actor.GetComponent<GridMover2D>();
                if (mover != null)
                {
                    Action<GridCoordinate, GridCoordinate, int> handler = (from, to, cost) => OnStep(actor, from, to, cost);
                    stepHandlers.Add(mover, handler);
                    mover.StepCompleted += handler;
                }
            }
            Advance();
        }

        private void OnStep(CombatActor actor, GridCoordinate from, GridCoordinate to, int cost)
        {
            if (cost <= 0 || !SpendTime(actor, cost)) return;
            GridStepResolved?.Invoke(new GridStepEvent { Actor = actor, From = from, To = to, TerrainCost = cost, Time = clock });
            ProcessDue(clock);
            GridMover2D mover = actor != null ? actor.GetComponent<GridMover2D>() : null;
            if (Current == actor && actor.HasAttacked && mover != null && mover.RemainingMovement == 0)
                FinishActivation(actor);
        }

        public bool FinishActivation(CombatActor actor)
        {
            if (actor == null || actor != Current || actor.IsMoving || !readyAt.ContainsKey(actor)) return false;
            long nextOther = long.MaxValue;
            foreach (CombatActor other in actorOrder)
                if (other != actor && readyAt.TryGetValue(other, out long time) && time < nextOther)
                    nextOther = time;
            // Ending an activation yields at least one turn to another actor, even if their
            // previous action put their readiness far ahead of the current clock.
            readyAt[actor] = Math.Max(clock, nextOther == long.MaxValue ? clock : nextOther) + 1;
            actor.Deactivate();
            Current = null;
            Advance();
            return true;
        }

        public bool SpendTime(CombatActor actor, int cost)
        {
            if (actor == null || actor != Current || cost <= 0 || actor.IsMoving) return false;
            long scaled = ((long)cost * speedScale + actor.Velocity - 1) / actor.Velocity;
            clock += Math.Max(1, scaled);
            advancingTime = true;
            try { ProcessDue(clock); }
            finally { advancingTime = false; }
            if (Current == null) Advance();
            return true;
        }

        private void ProcessDue(long limit)
        {
            while (true)
            {
                int selected = -1;
                for (int i = 0; i < effects.Count; i++)
                    if (effects[i].Time <= limit && (selected < 0 || effects[i].Time < effects[selected].Time ||
                        (effects[i].Time == effects[selected].Time && effects[i].Sequence < effects[selected].Sequence)))
                        selected = i;
                if (selected < 0) return;
                ScheduledEffect effect = effects[selected];
                effects.RemoveAt(selected);
                try { effect.Effect(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        public bool Submit(CombatActor actor, int actionCost, int recovery, Action effect)
        {
            if (actor == null || actor != Current || actor.IsMoving || actionCost <= 0 || recovery < 0) return false;
            // A callback can change velocity; that new value affects the next readiness.
            try { effect?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception, this); return false; }
            if (!readyAt.ContainsKey(actor)) return false;
            long scaled = ((long)actionCost * speedScale + actor.Velocity - 1) / actor.Velocity;
            readyAt[actor] = clock + Math.Max(1, scaled) + recovery;
            actor.Deactivate();
            Current = null;
            Advance();
            return true;
        }

        // Encounter hazards or other non-actor systems may schedule their own effect.
        public bool ScheduleExternal(int delay, Action effect)
        {
            if (delay < 0 || effect == null || readyAt.Count == 0) return false;
            effects.Add(new ScheduledEffect { Time = clock + delay, Sequence = nextSequence++, Effect = effect });
            return true;
        }

        public void Remove(CombatActor actor)
        {
            if (actor == null || !readyAt.Remove(actor)) return;
            actorOrder.Remove(actor);
            GridMover2D mover = actor.GetComponent<GridMover2D>();
            if (mover != null && stepHandlers.TryGetValue(mover, out Action<GridCoordinate, GridCoordinate, int> handler))
            {
                mover.StepCompleted -= handler;
                stepHandlers.Remove(mover);
            }
            actor.ExitCombat();
            if (Current == actor) { Current = null; if (!advancingTime) Advance(); }
        }

        public void End()
        {
            foreach (var pair in stepHandlers)
                if (pair.Key != null) pair.Key.StepCompleted -= pair.Value;
            stepHandlers.Clear();
            foreach (CombatActor actor in readyAt.Keys)
                if (actor != null) actor.ExitCombat();
            readyAt.Clear();
            actorOrder.Clear();
            effects.Clear();
            Current = null;
        }

        private void OnDestroy() => End();

        private void Advance()
        {
            while (Current == null && (readyAt.Count > 0 || effects.Count > 0))
            {
                CombatActor next = null;
                long nextTime = long.MaxValue;
                foreach (CombatActor actor in actorOrder)
                    if (actor != null && readyAt.TryGetValue(actor, out long time) && time < nextTime)
                    { next = actor; nextTime = time; }

                int firstEffect = -1;
                for (int i = 0; i < effects.Count; i++)
                    if (firstEffect < 0 || effects[i].Time < effects[firstEffect].Time ||
                        (effects[i].Time == effects[firstEffect].Time && effects[i].Sequence < effects[firstEffect].Sequence))
                        firstEffect = i;

                if (firstEffect >= 0 && effects[firstEffect].Time <= nextTime)
                {
                    ScheduledEffect scheduled = effects[firstEffect];
                    effects.RemoveAt(firstEffect);
                    clock = Math.Max(clock, scheduled.Time);
                    try { scheduled.Effect(); }
                    catch (Exception exception) { Debug.LogException(exception, this); }
                    continue;
                }
                if (next == null) break;
                clock = Math.Max(clock, nextTime);
                Current = next;
                next.Activate();
                ActorActivated?.Invoke(next);
            }
        }
    }
}
