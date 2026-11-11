using System.Collections.Generic;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    public static class GridPathfinder
    {
        private struct Candidate
        {
            public GridCoordinate Cell;
            public int Cost;
            public long Priority;
            public long Sequence;
            public Candidate(GridCoordinate cell, int cost, long priority, long sequence)
            { Cell = cell; Cost = cost; Priority = priority; Sequence = sequence; }
        }

        public static Dictionary<GridCoordinate, int> Reachable(GameplayGrid2D grid, GridCoordinate start, int budget)
        {
            Search(grid, start, null, Mathf.Max(0, budget), out Dictionary<GridCoordinate, int> costs);
            return costs;
        }

        public static List<GridCoordinate> Path(GameplayGrid2D grid, GridCoordinate start, GridCoordinate goal, int budget = int.MaxValue)
        {
            Dictionary<GridCoordinate, GridCoordinate> previous = Search(grid, start, goal, Mathf.Max(0, budget), out Dictionary<GridCoordinate, int> costs);
            List<GridCoordinate> path = new List<GridCoordinate>();
            if (!costs.ContainsKey(goal)) return path;
            for (GridCoordinate step = goal; step != start; step = previous[step]) path.Add(step);
            path.Reverse();
            return path;
        }

        private static Dictionary<GridCoordinate, GridCoordinate> Search(GameplayGrid2D grid, GridCoordinate start,
            GridCoordinate? goal, int budget, out Dictionary<GridCoordinate, int> costs)
        {
            costs = new Dictionary<GridCoordinate, int>();
            Dictionary<GridCoordinate, GridCoordinate> previous = new Dictionary<GridCoordinate, GridCoordinate>();
            if (grid == null || !grid.IsWalkable(start, out _)) return previous;

            long sequence = 0;
            SortedSet<Candidate> frontier = new SortedSet<Candidate>(Comparer<Candidate>.Create((a, b) =>
            {
                int order = a.Priority.CompareTo(b.Priority);
                return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
            })) { new Candidate(start, 0, 0, sequence++) };
            costs[start] = 0;
            while (frontier.Count > 0)
            {
                Candidate candidate = frontier.Min;
                frontier.Remove(candidate);
                GridCoordinate current = candidate.Cell;
                if (candidate.Cost != costs[current]) continue;
                if (goal.HasValue && current == goal.Value) break;

                foreach (GridCoordinate offset in ModularGrid.GridLayout.GetNeighbors(GridShape.Square))
                {
                    GridCoordinate next = current + offset;
                    if (!grid.TryTraversal(current, next, out int stepCost) || costs[current] > budget - stepCost)
                        continue;
                    int newCost = costs[current] + stepCost;
                    if (costs.TryGetValue(next, out int oldCost) && oldCost <= newCost) continue;
                    costs[next] = newCost;
                    previous[next] = current;
                    long estimate = goal.HasValue
                        ? ((long)Mathf.Abs(next.X - goal.Value.X) + Mathf.Abs(next.Y - goal.Value.Y)) * grid.MinimumCost
                        : 0;
                    frontier.Add(new Candidate(next, newCost, newCost + estimate, sequence++));
                }
            }
            return previous;
        }
    }
}
