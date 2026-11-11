using System;
using System.Collections.Generic;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterIntent))]
    public sealed class GridMover2D : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float cellsPerSecond = 4f;
        [SerializeField, Min(0)] private int maxMovement = 100;
        private readonly Queue<GridCoordinate> pending = new Queue<GridCoordinate>();
        private GameplayGrid2D world;
        private CharacterIntent intent;
        private Rigidbody2D body;
        private GridCoordinate cell;
        private Vector2 destination;
        private bool moving;
        private bool combat;
        private bool turnActive;
        private bool registered;
        private GridCoordinate stepFrom;
        private int stepCost;

        public int RemainingMovement { get; private set; }
        public GridCoordinate Cell => cell;
        public bool IsMoving => moving;
        public event Action<int> MovementChanged;
        public event Action DestinationBlocked;
        public event Action<GridCoordinate, GridCoordinate, int> StepCompleted;

        public void Configure(float speed, int movement)
        {
            cellsPerSecond = Mathf.Max(0.01f, speed);
            maxMovement = Mathf.Max(0, movement);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            intent = GetComponent<CharacterIntent>();
        }

        private void OnEnable()
        {
            intent.CellStepRequested += OnStep;
            intent.DestinationRequested += OnDestination;
            if (world != null && !registered) registered = world.TryRegister(cell, this);
        }

        private void OnDisable()
        {
            intent.CellStepRequested -= OnStep;
            intent.DestinationRequested -= OnDestination;
            pending.Clear();
            moving = false;
            if (world != null && registered) body.position = world.CellToWorld(cell);
            if (world != null && registered) world.Unregister(cell, this);
            registered = false;
        }

        public bool Bind(GameplayGrid2D grid)
        {
            if (world != null && registered) world.Unregister(cell, this);
            world = grid;
            registered = false;
            if (world == null) return false;
            cell = world.WorldToCell(transform.position);
            registered = world.TryRegister(cell, this);
            if (!registered) Debug.LogError("Character must start on an unoccupied walkable gameplay cell.", this);
            else body.position = world.CellToWorld(cell);
            return registered;
        }

        public void EnterCombat()
        {
            combat = true;
            turnActive = false;
            RemainingMovement = 0;
            pending.Clear();
            MovementChanged?.Invoke(RemainingMovement);
        }

        public void LeaveCombat()
        {
            combat = false;
            turnActive = false;
            pending.Clear();
        }

        public void BeginActivation(int effectiveMaxMovement)
        {
            if (!combat) return;
            turnActive = true;
            RemainingMovement = Mathf.Max(0, effectiveMaxMovement);
            MovementChanged?.Invoke(RemainingMovement);
        }

        public void EndActivation()
        {
            turnActive = false;
            pending.Clear();
        }

        private void OnStep(Vector2Int step)
        {
            if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1 || moving || (combat && !turnActive)) return;
            pending.Clear();
            StartNext(new GridCoordinate(cell.X + step.x, cell.Y + step.y));
        }

        private void OnDestination(Vector2 target)
        {
            if (world == null || !registered || moving || (combat && !turnActive)) return;
            GridCoordinate goal = world.WorldToCell(target);
            List<GridCoordinate> path = GridPathfinder.Path(world, cell, goal,
                combat ? RemainingMovement : int.MaxValue);
            pending.Clear();
            foreach (GridCoordinate step in path) pending.Enqueue(step);
            if (path.Count == 0 && goal != cell) DestinationBlocked?.Invoke();
            else if (pending.Count > 0) StartNext(pending.Dequeue());
        }

        private void StartNext(GridCoordinate next)
        {
            if (!registered || (combat && !turnActive) || !world.TryTraversal(cell, next, out int cost) ||
                (combat && cost > RemainingMovement) || !world.TryMove(cell, next, this, out cost))
            {
                pending.Clear();
                DestinationBlocked?.Invoke();
                return;
            }
            stepFrom = cell;
            cell = next;
            destination = world.CellToWorld(next);
            moving = true;
            if (combat)
            {
                stepCost = cost;
                RemainingMovement -= cost;
                MovementChanged?.Invoke(RemainingMovement);
            }
        }

        private void FixedUpdate()
        {
            if (!moving) return;
            Vector2 next = Vector2.MoveTowards(body.position, destination,
                cellsPerSecond * Time.fixedDeltaTime);
            body.MovePosition(next);
            if ((next - destination).sqrMagnitude > 0.0001f) return;
            moving = false;
            StepCompleted?.Invoke(stepFrom, cell, combat ? stepCost : 0);
            if (pending.Count > 0 && (!combat || turnActive)) StartNext(pending.Dequeue());
        }
    }

}
