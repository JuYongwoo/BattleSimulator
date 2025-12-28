using System;
using System.Collections.Generic;
using UnityEngine;

public class SafetyScoreManager : Singleton<SafetyScoreManager>
{
    // Safety boards per AI type (keyed by System.Type of AI component)
    private readonly Dictionary<Type, Dictionary<Vector2Int, int>> mSafetyBoards = new Dictionary<Type, Dictionary<Vector2Int, int>>();

    private float mNextUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 5f; // seconds

    // Cached neighbor directions (8-dir)
    private static readonly Vector2Int[] Neighbors8 = new Vector2Int[]
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

    public void Update()
    {
        if (Time.time < mNextUpdateTime) return;
        mNextUpdateTime = Time.time + UPDATE_INTERVAL;
        RebuildAllBoards();
    }

    public int GetSafetyScore(Type aiType, Vector3 worldPos)
    {
        var grid = GridPathfinder.WorldToGrid(worldPos);
        if (!mSafetyBoards.TryGetValue(aiType, out var board)) return 0;
        if (board.TryGetValue(grid, out int score)) return score;
        return 0;
    }

    private void RebuildAllBoards()
    {
        // Find distinct AI types present in scene
        var types = new HashSet<Type>();
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            var ai = pair.Value.GetComponent<ObjectBase_AIBase>();
            if (ai == null) continue;
            types.Add(ai.GetType());
        }
        foreach (var t in types)
        {
            mSafetyBoards[t] = BuildBoardForType(t);
        }
    }

    private Dictionary<Vector2Int, int> BuildBoardForType(Type aiType)
    {
        var board = new Dictionary<Vector2Int, int>();
        // Gather enemy positions (all AI not of aiType)
        var enemyCells = new List<Vector2Int>();
        float yRef = 0f;
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            var ai = pair.Value.GetComponent<ObjectBase_AIBase>();
            if (ai == null) continue;
            if (ai.GetType() == aiType) continue; // same type = teammate
            var cell = GridPathfinder.WorldToGrid(pair.Value.transform.position);
            enemyCells.Add(cell);
            yRef = pair.Value.transform.position.y;
        }
        // If no enemies found, return empty board
        if (enemyCells.Count == 0) return board;

        // Determine bounds around all objects to limit BFS (adaptive rectangle)
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            var pos = GridPathfinder.WorldToGrid(pair.Value.transform.position);
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        int margin = 32;
        minX -= margin; maxX += margin; minY -= margin; maxY += margin;

        // Multi-source BFS
        var q = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        foreach (var c in enemyCells)
        {
            q.Enqueue(c);
            visited.Add(c);
            board[c] = 0; // distance 0 at enemy
        }
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int curDist = board[cur];
            foreach (var d in Neighbors8)
            {
                var npos = cur + d;
                if (npos.x < minX || npos.x > maxX || npos.y < minY || npos.y > maxY) continue;
                if (visited.Contains(npos)) continue;
                // Only traverse walkable cells
                if (!GridPathfinder.IsWalkable(npos, yRef)) continue;
                visited.Add(npos);
                board[npos] = curDist + 1; // increasing positive safety with distance
                q.Enqueue(npos);
            }
        }
        // No negation: safety score is positive and increases farther from enemies
        return board;
    }
}
