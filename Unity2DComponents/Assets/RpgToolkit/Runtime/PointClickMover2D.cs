using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(CharacterIntent))]
    public sealed class PointClickMover2D : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float speed = 4f;
        [SerializeField, Min(0.1f)] private float sampleSize = 0.5f;
        [SerializeField, Min(1f)] private float searchMargin = 8f;
        [SerializeField] private LayerMask obstacles = ~0;
        private Rigidbody2D body;
        private CircleCollider2D ownCollider;
        private CharacterIntent intent;
        private readonly Queue<Vector2> waypoints = new Queue<Vector2>();
        private Vector2 goal;
        private Vector2 previousPosition;
        private float stalledTime;
        private float replanAt;

        public event System.Action DestinationUnreachable;

        public void Configure(float movementSpeed) => speed = Mathf.Max(0.01f, movementSpeed);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownCollider = GetComponent<CircleCollider2D>();
            intent = GetComponent<CharacterIntent>();
        }

        private void OnEnable() => intent.DestinationRequested += OnDestination;
        private void OnDisable()
        {
            intent.DestinationRequested -= OnDestination;
            waypoints.Clear();
        }

        private bool ClearPoint(Vector2 point)
        {
            float radius = Mathf.Max(0.05f, ownCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(point, radius, obstacles))
                if (hit != ownCollider && !hit.isTrigger) return false;
            return true;
        }

        private bool ClearSegment(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            if (!ClearPoint(b)) return false;
            float radius = Mathf.Max(0.05f, ownCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(a, radius, delta.normalized, delta.magnitude, obstacles))
                if (hit.collider != ownCollider && !hit.collider.isTrigger) return false;
            return true;
        }

        private void OnDestination(Vector2 target)
        {
            goal = target;
            if (!Plan()) DestinationUnreachable?.Invoke();
        }

        private bool Plan()
        {
            waypoints.Clear();
            Vector2 start = body.position;
            if (!ClearPoint(goal)) return false;
            if (ClearSegment(start, goal)) { waypoints.Enqueue(goal); return true; }

            float left = Mathf.Min(start.x, goal.x) - searchMargin;
            float bottom = Mathf.Min(start.y, goal.y) - searchMargin;
            int width = Mathf.CeilToInt((Mathf.Max(start.x, goal.x) - left + searchMargin) / sampleSize) + 1;
            int height = Mathf.CeilToInt((Mathf.Max(start.y, goal.y) - bottom + searchMargin) / sampleSize) + 1;
            if (width < 1 || height < 1 || (long)width * height > 16384) return false;

            Vector2Int ToCell(Vector2 p) => new Vector2Int(Mathf.Clamp(Mathf.RoundToInt((p.x - left) / sampleSize), 0, width - 1),
                Mathf.Clamp(Mathf.RoundToInt((p.y - bottom) / sampleSize), 0, height - 1));
            Vector2 ToPoint(Vector2Int c) => new Vector2(left + c.x * sampleSize, bottom + c.y * sampleSize);
            Vector2Int first = ToCell(start), last = ToCell(goal);
            List<Vector2Int> open = new List<Vector2Int> { first };
            Dictionary<Vector2Int, float> costs = new Dictionary<Vector2Int, float> { [first] = 0f };
            Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            Vector2Int[] offsets = {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
                new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (costs[open[i]] + Vector2.Distance(ToPoint(open[i]), goal) <
                        costs[open[best]] + Vector2.Distance(ToPoint(open[best]), goal)) best = i;
                Vector2Int current = open[best];
                open.RemoveAt(best);
                if (!closed.Add(current)) continue;
                if (current == last) break;
                foreach (Vector2Int offset in offsets)
                {
                    Vector2Int next = current + offset;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || closed.Contains(next)) continue;
                    Vector2 from = current == first ? start : ToPoint(current);
                    Vector2 to = ToPoint(next);
                    if (!ClearSegment(from, to)) continue;
                    float cost = costs[current] + Vector2.Distance(from, to);
                    if (costs.TryGetValue(next, out float old) && old <= cost) continue;
                    costs[next] = cost;
                    previous[next] = current;
                    open.Add(next);
                }
            }
            if (!costs.ContainsKey(last) || !ClearSegment(ToPoint(last), goal)) return false;
            List<Vector2> raw = new List<Vector2> { goal };
            for (Vector2Int cursor = last; cursor != first; cursor = previous[cursor])
                raw.Add(ToPoint(cursor));
            raw.Reverse();
            Vector2 anchor = start;
            for (int i = 0; i < raw.Count;)
            {
                int furthest = i;
                for (int j = raw.Count - 1; j > i; j--)
                    if (ClearSegment(anchor, raw[j])) { furthest = j; break; }
                waypoints.Enqueue(raw[furthest]);
                anchor = raw[furthest];
                i = furthest + 1;
            }
            previousPosition = body.position;
            stalledTime = 0f;
            return true;
        }

        private void FixedUpdate()
        {
            if (waypoints.Count == 0) return;
            Vector2 next = waypoints.Peek();
            Vector2 position = Vector2.MoveTowards(body.position, next, speed * Time.fixedDeltaTime);
            body.MovePosition(position);
            if (Vector2.Distance(position, next) < 0.03f) waypoints.Dequeue();
            if (Vector2.Distance(body.position, previousPosition) < 0.005f) stalledTime += Time.fixedDeltaTime;
            else stalledTime = 0f;
            previousPosition = body.position;
            if (stalledTime < 0.3f || Time.time < replanAt) return;
            replanAt = Time.time + 0.5f;
            if (!Plan()) DestinationUnreachable?.Invoke();
        }
    }
}
