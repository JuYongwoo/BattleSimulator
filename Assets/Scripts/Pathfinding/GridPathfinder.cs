using System.Collections.Generic;
using UnityEngine;

// Simple grid-based A* pathfinder with 8-direction movement.
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

    // Heuristic: Octile distance for 8-dir movement
    static int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        int dMin = Mathf.Min(dx, dy);
        int dMax = Mathf.Max(dx, dy);
        // cost: diagonal=14, straight=10 (scaled by 10 to keep ints)
        return dMin * 14 + (dMax - dMin) * 10;
    }

    // Non-alloc collider buffer
    static Collider[] sOverlapBuffer = new Collider[16];

    // Check if a grid cell is walkable. Only blocks Obstacles; allows multiple AIs to share a cell.
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

    static readonly Vector2Int[] Neighbors8 = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };

    // Find nearest walkable cell around origin (including origin). Expands in rings up to maxRadius.
    static Vector2Int FindNearestWalkable(Vector2Int origin, float y, int maxRadius)
    {
        if (IsWalkable(origin, y)) return origin;
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int dy1 = r;
                int dy2 = -r;
                var p1 = new Vector2Int(origin.x + dx, origin.y + dy1);
                var p2 = new Vector2Int(origin.x + dx, origin.y + dy2);
                if (IsWalkable(p1, y)) return p1;
                if (IsWalkable(p2, y)) return p2;
            }
            for (int dy = -r + 1; dy <= r - 1; dy++)
            {
                int dx1 = r;
                int dx2 = -r;
                var p1 = new Vector2Int(origin.x + dx1, origin.y + dy);
                var p2 = new Vector2Int(origin.x + dx2, origin.y + dy);
                if (IsWalkable(p1, y)) return p1;
                if (IsWalkable(p2, y)) return p2;
            }
        }
        return origin; // fallback (may be unwalkable)
    }

    public static List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld, int searchRadius = 64, int maxIterations = 20000)
    {
        float y = startWorld.y;
        Vector2Int start = WorldToGrid(startWorld);
        Vector2Int goal = WorldToGrid(goalWorld);

        // Snap start/goal to nearest walkable to avoid immediate failure when spawned on blocked cells
        start = FindNearestWalkable(start, y, 8);
        goal = FindNearestWalkable(goal, y, 8);

        if (start == goal)
        {
            return new List<Vector3> { GridToWorld(goal, y) };
        }

        // Adapt search radius to distance (scaled for octile)
        int dist = Heuristic(start, goal);
        int approxCells = dist / 10; // convert to approx straight steps
        int adaptiveRadius = Mathf.Clamp(approxCells + 16, 32, Mathf.Max(searchRadius, 256));

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
        Node bestNode = startNode; // closest to goal seen so far
        while (openHeap.Count > 0)
        {
            iterations++;
            if (iterations > maxIterations)
            {
                break;
            }

            var current = openHeap.Pop();
            if (!openMap.TryGetValue(current.Pos, out var curCheck))
            {
                continue;
            }
            openMap.Remove(current.Pos);
            closed.Add(current.Pos);

            // track best node by heuristic
            if (current.H < bestNode.H)
            {
                bestNode = current;
            }

            if (current.Pos == goal)
            {
                // reconstruct full path
                return ReconstructPath(current.Pos, parents, y);
            }

            foreach (var d in Neighbors8)
            {
                var npos = current.Pos + d;
                if (!InBounds(npos, minX, maxX, minY, maxY)) continue;
                if (closed.Contains(npos)) continue;

                if (!IsWalkable(npos, y)) continue;

                int stepCost = (d.x != 0 && d.y != 0) ? 14 : 10;
                int tentativeG = current.G + stepCost;
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

        // No path to goal; return best-effort partial path toward goal
        return ReconstructPath(bestNode.Pos, parents, y);
    }

    static List<Vector3> ReconstructPath(Vector2Int endPos, Dictionary<Vector2Int, Vector2Int> parents, float y)
    {
        var pathCells = new List<Vector2Int>();
        var c = endPos;
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
}
