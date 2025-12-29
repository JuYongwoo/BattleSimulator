using System.Collections.Generic;
using UnityEngine;

// Simple grid-based A* pathfinder with 8-direction movement and robust obstacle handling.
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
    static Collider[] sOverlapBuffer = new Collider[24];

    // Simple per-frame caches to reduce Physics overlap cost
    static readonly Dictionary<Vector2Int, bool> sWalkableCache = new Dictionary<Vector2Int, bool>(1024);
    static int sWalkableFrame = -1;
    static readonly Dictionary<(Vector2Int, Vector2Int), bool> sSegmentBlockedCache = new Dictionary<(Vector2Int, Vector2Int), bool>(1024);
    static int sSegmentFrame = -1;

    public static bool IsWalkable(Vector2Int cell, float y, float cellRadius = 0.45f)
    {
        // reset cache each frame
        int frame = Time.frameCount;
        if (frame != sWalkableFrame)
        {
            sWalkableFrame = frame;
            sWalkableCache.Clear();
        }
        if (sWalkableCache.TryGetValue(cell, out bool cached))
        {
            return cached;
        }

        Vector3 center = GridToWorld(cell, y);
        int count = Physics.OverlapSphereNonAlloc(center, cellRadius, sOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var c = sOverlapBuffer[i];
            if (c == null) continue;
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (c.isTrigger) continue;
            if (c.gameObject.CompareTag("Obstacle")) { sWalkableCache[cell] = false; return false; }
        }
        Vector3 halfExtents = new Vector3(0.49f, 0.5f, 0.49f);
        Quaternion orientation = Quaternion.identity;
        count = Physics.OverlapBoxNonAlloc(center, halfExtents, sOverlapBuffer, orientation);
        for (int i = 0; i < count; i++)
        {
            var c = sOverlapBuffer[i];
            if (c == null) continue;
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (c.isTrigger) continue;
            if (c.gameObject.CompareTag("Obstacle")) { sWalkableCache[cell] = false; return false; }
        }
        sWalkableCache[cell] = true;
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

    static bool IsSegmentBlocked(Vector3 fromWorld, Vector3 toWorld)
    {
        // reset cache each frame
        int frame = Time.frameCount;
        if (frame != sSegmentFrame)
        {
            sSegmentFrame = frame;
            sSegmentBlockedCache.Clear();
        }
        var key = (WorldToGrid(fromWorld), WorldToGrid(toWorld));
        if (sSegmentBlockedCache.TryGetValue(key, out bool cached))
        {
            return cached;
        }

        Vector3 dir = toWorld - fromWorld;
        dir.y = 0f;
        float len = dir.magnitude;
        if (len < 0.001f) { sSegmentBlockedCache[key] = false; return false; }
        dir /= len;
        // cast a small box along the movement segment to detect walls between cells
        Vector3 center = fromWorld + dir * (len * 0.5f);
        Vector3 halfExtents = new Vector3(0.45f, 0.5f, 0.02f) + new Vector3(Mathf.Abs(dir.x) * 0.02f, 0f, Mathf.Abs(dir.z) * 0.02f);
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, sOverlapBuffer, rot);
        for (int i = 0; i < count; i++)
        {
            var c = sOverlapBuffer[i];
            if (c == null) continue;
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (c.isTrigger) continue;
            if (c.gameObject.CompareTag("Obstacle")) { sSegmentBlockedCache[key] = true; return true; }
        }
        sSegmentBlockedCache[key] = false;
        return false;
    }

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

                // Prevent diagonal corner cutting: require both adjacent orthogonals to be walkable
                if (d.x != 0 && d.y != 0)
                {
                    var adj1 = new Vector2Int(current.Pos.x + d.x, current.Pos.y);
                    var adj2 = new Vector2Int(current.Pos.x, current.Pos.y + d.y);
                    if (!IsWalkable(adj1, y) || !IsWalkable(adj2, y))
                    {
                        continue;
                    }
                }

                // Prevent crossing a wall between cells
                Vector3 fromWorld = GridToWorld(current.Pos, y);
                Vector3 toWorld = GridToWorld(npos, y);
                if (IsSegmentBlocked(fromWorld, toWorld)) continue;

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

    // Global LRU cache for grid path distances (ga->gb). Avoids re-running A* for repeated queries.
    class PathDistanceCache
    {
        private readonly int mCapacity;
        private readonly Dictionary<(Vector2Int, Vector2Int), float> mMap = new Dictionary<(Vector2Int, Vector2Int), float>(256);
        private readonly LinkedList<(Vector2Int, Vector2Int)> mOrder = new LinkedList<(Vector2Int, Vector2Int)>();

        public PathDistanceCache(int capacity)
        {
            mCapacity = Mathf.Max(64, capacity);
        }

        public bool TryGet(Vector2Int a, Vector2Int b, out float dist)
        {
            var key = (a, b);
            if (mMap.TryGetValue(key, out dist))
            {
                // move to front (MRU)
                var node = mOrder.Find(key);
                if (node != null)
                {
                    mOrder.Remove(node);
                    mOrder.AddFirst(key);
                }
                return true;
            }
            dist = 0f;
            return false;
        }

        public void Put(Vector2Int a, Vector2Int b, float dist)
        {
            var key = (a, b);
            if (mMap.ContainsKey(key))
            {
                mMap[key] = dist;
                var node = mOrder.Find(key);
                if (node != null)
                {
                    mOrder.Remove(node);
                }
                mOrder.AddFirst(key);
                return;
            }
            if (mOrder.Count >= mCapacity)
            {
                var lru = mOrder.Last.Value;
                mOrder.RemoveLast();
                mMap.Remove(lru);
            }
            mMap[key] = dist;
            mOrder.AddFirst(key);
        }
    }

    static readonly PathDistanceCache sGlobalCache = new PathDistanceCache(512);

    // Compute path distance (A* grid path length) between two world positions.
    // Bounded A*: runs FindPath with an adaptive radius/iteration budget; falls back to heuristic if needed.
    public static float ComputePathDistance(Vector3 sourcePosition, Vector3 targetPosition)
    {
        // Project to XZ plane for grid logic
        Vector3 src = new Vector3(sourcePosition.x, 0f, sourcePosition.z);
        Vector3 dst = new Vector3(targetPosition.x, 0f, targetPosition.z);
        if ((src - dst).sqrMagnitude < 0.0001f) return 0f;

        var ga = WorldToGrid(src);
        var gb = WorldToGrid(dst);

        // Global cache hit
        if (sGlobalCache.TryGet(ga, gb, out float cached))
        {
            return cached;
        }

        // Manhattan steps on grid for budget/heuristic
        int steps = GridManhattanDistance(src, dst);

        // Very close: avoid A* altogether
        if (steps <= 1)
        {
            float d = Vector3.Distance(src, dst);
            sGlobalCache.Put(ga, gb, d);
            return d;
        }

        // Adaptive bounds to prevent stalls on maze-like maps
        int radius = Mathf.Clamp(steps + 12, 24, 128);
        int maxIter = Mathf.Clamp(steps * 80, 1500, 12000);

        try
        {
            var path = FindPath(src, dst, radius, maxIter);
            if (path != null && path.Count > 0)
            {
                float total = 0f;
                Vector3 prev = src;
                for (int i = 0; i < path.Count; i++)
                {
                    total += Vector3.Distance(prev, path[i]);
                    prev = path[i];
                }
                float tail = Vector3.Distance(prev, dst);
                if (IsSegmentBlocked(prev, dst)) tail *= 1.5f;
                total += tail;
                total = Mathf.Max(total, 0.001f);
                sGlobalCache.Put(ga, gb, total);
                return total;
            }
        }
        catch
        {
            // ignore and fallback
        }

        // Fallback: octile heuristic with wall-awareness penalty (cache it to avoid repeated work)
        int dx = Mathf.Abs(ga.x - gb.x);
        int dz = Mathf.Abs(ga.y - gb.y);
        int dMin = Mathf.Min(dx, dz);
        int dMax = Mathf.Max(dx, dz);
        const float diag = 1.41421356f;
        float heuristic = dMin * diag + (dMax - dMin) * 1.0f;
        if (IsSegmentBlocked(src, dst)) heuristic += Mathf.Max(heuristic, 1f) * 2.0f + 5.0f;
        heuristic = Mathf.Max(heuristic, 0.001f);
        sGlobalCache.Put(ga, gb, heuristic);
        return heuristic;
    }
}
