using System.Collections.Generic;
using UnityEngine;

// Simple grid-based A* pathfinder.
// Rounds world positions to integer grid coordinates using: [-0.5, 0.499..] -> 0, [0.5, 1.499..] -> 1
// Assumes Y is irrelevant for movement (uses XZ plane). Walkability is determined by Physics.OverlapSphere around cell center with layer/tag filters.
public static class GridPathfinder
{
    public struct Node
    {
        public Vector2Int Pos;
        public int G; // cost from start
        public int H; // heuristic to goal
        public int F => G + H;
        public Vector2Int Parent;
        public Node(Vector2Int pos)
        {
            Pos = pos;
            G = 0;
            H = 0;
            Parent = new Vector2Int(int.MinValue, int.MinValue);
        }
    }

    // Round a world position to grid coordinate
    public static Vector2Int WorldToGrid(Vector3 world)
    {
        int gx = Mathf.FloorToInt(world.x + 0.5f);
        int gz = Mathf.FloorToInt(world.z + 0.5f);
        return new Vector2Int(gx, gz);
    }

    // Convert grid coord back to world position at cell center
    public static Vector3 GridToWorld(Vector2Int grid, float y)
    {
        return new Vector3(grid.x, y, grid.y);
    }

    // Heuristic: Manhattan distance
    static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Non-alloc collider buffer
    static Collider[] sOverlapBuffer = new Collider[8];

    // Check if a grid cell is walkable. You can customize by layers/tags.
    public static bool IsWalkable(Vector2Int cell, float y, float cellRadius = 0.45f)
    {
        Vector3 center = GridToWorld(cell, y);
        int count = Physics.OverlapSphereNonAlloc(center, cellRadius, sOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var c = sOverlapBuffer[i];
            if (c == null) continue;
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (c.isTrigger) continue;
            if (c.gameObject.CompareTag("Obstacle")) return false;
        }
        return true;
    }

    static readonly Vector2Int[] Neighbors4 = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    public static List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld, int searchRadius = 64, int maxIterations = 10000)
    {
        float y = startWorld.y;
        Vector2Int start = WorldToGrid(startWorld);
        Vector2Int goal = WorldToGrid(goalWorld);

        if (start == goal)
        {
            return new List<Vector3> { GridToWorld(goal, y) };
        }

        // Adapt search radius to distance
        int dist = Heuristic(start, goal);
        int adaptiveRadius = Mathf.Clamp(dist + 16, 32, Mathf.Max(searchRadius, 256));

        // Use parent-aware implementation directly
        return FindPathWithParents(start, goal, y, adaptiveRadius, maxIterations);
    }

    static bool InBounds(Vector2Int p, int minX, int maxX, int minY, int maxY)
    {
        return p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
    }

    public static int GridManhattanDistance(Vector3 a, Vector3 b)
    {
        var ga = GridPathfinder.WorldToGrid(a);
        var gb = GridPathfinder.WorldToGrid(b);
        return Mathf.Abs(ga.x - gb.x) + Mathf.Abs(ga.y - gb.y);
    }

    // Lightweight priority queue by F cost
    class MinHeap
    {
        public readonly List<Node> data = new List<Node>();
        public void Push(Node n)
        {
            data.Add(n);
            HeapifyUp(data.Count - 1);
        }
        public Node Pop()
        {
            int last = data.Count - 1;
            Node root = data[0];
            data[0] = data[last];
            data.RemoveAt(last);
            if (data.Count > 0) HeapifyDown(0);
            return root;
        }
        public int Count => data.Count;
        void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (data[i].F < data[p].F)
                {
                    (data[i], data[p]) = (data[p], data[i]);
                    i = p;
                }
                else break;
            }
        }
        void HeapifyDown(int i)
        {
            int n = data.Count;
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int smallest = i;
                if (l < n && data[l].F < data[smallest].F) smallest = l;
                if (r < n && data[r].F < data[smallest].F) smallest = r;
                if (smallest != i)
                {
                    (data[i], data[smallest]) = (data[smallest], data[i]);
                    i = smallest;
                }
                else break;
            }
        }
    }

    static List<Vector3> FindPathWithParents(Vector2Int start, Vector2Int goal, float y, int searchRadius, int maxIterations)
    {
        // Bound the search to a rectangle around start/goal to avoid infinite expansion
        int minX = Mathf.Min(start.x, goal.x) - searchRadius;
        int maxX = Mathf.Max(start.x, goal.x) + searchRadius;
        int minY = Mathf.Min(start.y, goal.y) - searchRadius;
        int maxY = Mathf.Max(start.y, goal.y) + searchRadius;

        var openHeap = new MinHeap();
        var openMap = new Dictionary<Vector2Int, Node>();
        var closed = new HashSet<Vector2Int>();
        var parents = new Dictionary<Vector2Int, Vector2Int>();

        var startNode = new Node(start)
        {
            G = 0,
            H = Heuristic(start, goal)
        };
        openHeap.Push(startNode);
        openMap[start] = startNode;

        int iterations = 0;
        while (openHeap.Count > 0)
        {
            iterations++;
            if (iterations > maxIterations)
            {
                // abort to prevent freezing
                break;
            }

            var current = openHeap.Pop();
            if (!openMap.TryGetValue(current.Pos, out var curCheck))
            {
                // already processed with better cost
                continue;
            }
            openMap.Remove(current.Pos);
            closed.Add(current.Pos);

            if (current.Pos == goal)
            {
                // reconstruct
                var pathCells = new List<Vector2Int>();
                var c = current.Pos;
                pathCells.Add(c);
                while (parents.TryGetValue(c, out var par))
                {
                    c = par;
                    pathCells.Add(c);
                }
                pathCells.Reverse();
                var pathWorld = new List<Vector3>(pathCells.Count);
                foreach (var cell in pathCells)
                {
                    pathWorld.Add(GridToWorld(cell, y));
                }
                return pathWorld;
            }

            foreach (var d in Neighbors4)
            {
                var npos = current.Pos + d;
                if (!InBounds(npos, minX, maxX, minY, maxY)) continue;
                if (closed.Contains(npos)) continue;
                if (!IsWalkable(npos, y)) continue;

                int tentativeG = current.G + 1;
                if (openMap.TryGetValue(npos, out var existing))
                {
                    if (tentativeG < existing.G)
                    {
                        existing.G = tentativeG;
                        existing.H = Heuristic(npos, goal);
                        existing.Parent = current.Pos;
                        parents[npos] = current.Pos;
                        openMap[npos] = existing;
                        openHeap.Push(existing);
                    }
                }
                else
                {
                    var node = new Node(npos)
                    {
                        G = tentativeG,
                        H = Heuristic(npos, goal),
                        Parent = current.Pos
                    };
                    parents[npos] = current.Pos;
                    openMap[npos] = node;
                    openHeap.Push(node);
                }
            }
        }

        // no path or aborted
        return new List<Vector3>();
    }
}
