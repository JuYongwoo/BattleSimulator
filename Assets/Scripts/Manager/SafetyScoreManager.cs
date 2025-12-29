using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafetyScoreManager : Singleton<SafetyScoreManager>
{
    private readonly Dictionary<Type, Dictionary<Vector2Int, int>> mSafetyBoards = new Dictionary<Type, Dictionary<Vector2Int, int>>();

    private readonly Dictionary<Type, Dictionary<Vector2Int, int>> mCache = new Dictionary<Type, Dictionary<Vector2Int, int>>();

    private float mNextUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 10f;

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

    private bool mIsRebuilding = false;
    private const int NODES_PER_FRAME = 2000;

    private int mTotalCachedEntries = 0;
    public bool IsReady => mTotalCachedEntries >= 50; // threshold can be tuned

    public void Update()
    {
        if (mIsRebuilding) return;
        if (Time.time < mNextUpdateTime) return;
        mNextUpdateTime = Time.time + UPDATE_INTERVAL;
        StartCoroutine(RebuildAllBoardsIncremental());
    }

    public int GetSafetyScore(Type aiType, Vector3 worldPos)
    {
        Vector2Int cell = GridPathfinder.WorldToGrid(worldPos);
        if (!mCache.TryGetValue(aiType, out var map))
        {
            map = new Dictionary<Vector2Int, int>(256);
            mCache[aiType] = map;
        }
        if (map.TryGetValue(cell, out int val))
        {
            return val;
        }
        int score = ComputeSafetyScoreInternal(aiType, worldPos);
        map[cell] = score;
        mTotalCachedEntries++;
        return score;
    }

    private int ComputeSafetyScoreInternal(Type aiType, Vector3 worldPos)
    {
        // Placeholder: should be replaced by real computation
        // For now, use a simple distance-from-enemies heuristic if available
        int baseScore = 50;
        // If ObjectManager has enemies, boost score by distance to nearest enemy
        float minEnemyDist = float.MaxValue;
        foreach (var id in ObjectManager.mAIIds)
        {
            var go = ObjectManager.mAll_Of_Game_Objects[id];
            if (!go.activeSelf) continue;
            if (go.GetComponent(aiType) != null) continue; // same team
            float d = Vector3.Distance(go.transform.position, worldPos);
            if (d < minEnemyDist) minEnemyDist = d;
        }
        if (minEnemyDist < float.MaxValue)
        {
            baseScore += Mathf.Clamp((int)minEnemyDist, 0, 100);
        }
        return Mathf.Clamp(baseScore, 0, 100);
    }

    public void ClearCache()
    {
        mCache.Clear();
        mTotalCachedEntries = 0;
    }

    private IEnumerator RebuildAllBoardsIncremental()
    {
        mIsRebuilding = true;
        var types = new HashSet<Type>();
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            var ai = pair.Value.GetComponent<ObjectBase_AIBase>();
            if (ai == null) continue;
            types.Add(ai.GetType());
        }
        foreach (var t in types)
        {
            yield return BuildBoardForTypeIncremental(t);
        }
        mIsRebuilding = false;
    }

    private IEnumerator BuildBoardForTypeIncremental(Type aiType)
    {
        var board = new Dictionary<Vector2Int, int>();
        var enemyCells = new List<Vector2Int>();
        float yRef = 0f;
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            var ai = pair.Value.GetComponent<ObjectBase_AIBase>();
            if (ai == null) continue;
            var cell = GridPathfinder.WorldToGrid(pair.Value.transform.position);
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
            yRef = pair.Value.transform.position.y;
            if (ai.GetType() != aiType)
            {
                enemyCells.Add(cell);
            }
        }
        int margin = 32;
        minX -= margin; maxX += margin; minY -= margin; maxY += margin;
        if (enemyCells.Count == 0)
        {
            mSafetyBoards[aiType] = board;
            yield break;
        }
        var q = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        foreach (var c in enemyCells)
        {
            q.Enqueue(c);
            visited.Add(c);
            board[c] = 0;
        }
        int processedThisFrame = 0;
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int curDist = board[cur];
            foreach (var d in Neighbors8)
            {
                var npos = cur + d;
                if (npos.x < minX || npos.x > maxX || npos.y < minY || npos.y > maxY) continue;
                if (visited.Contains(npos)) continue;
                if (!GridPathfinder.IsWalkable(npos, yRef)) continue;
                visited.Add(npos);
                board[npos] = curDist + 1;
                q.Enqueue(npos);
            }
            processedThisFrame++;
            if (processedThisFrame >= NODES_PER_FRAME)
            {
                processedThisFrame = 0;
                yield return null;
            }
        }
        mSafetyBoards[aiType] = board;
    }
}
