using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafetyScoreManager : Singleton<SafetyScoreManager>
{
    // Precomputed safety boards per AI type (grid cell -> score)
    private readonly Dictionary<Type, Dictionary<Vector2Int, int>> mSafetyBoards = new Dictionary<Type, Dictionary<Vector2Int, int>>();

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

    // Ready when boards exist for all current AI types
    public bool IsReady { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();
        IsReady = false;
    }

    public void Update()
    {
        if (mIsRebuilding) return;
        if (Time.time < mNextUpdateTime) return;
        mNextUpdateTime = Time.time + UPDATE_INTERVAL;
        StartCoroutine(RebuildAllBoardsIncremental());
    }

    // Query precomputed score: determine requester AI type by pID and return board value.
    public int GetSafetyScore(int pID, Vector3 worldPos)
    {
        // Find requester type
        if (!ObjectManager.mAll_Of_Game_Objects.TryGetValue(pID, out var me)) return 50;
        var ai = me.GetComponent<ObjectBase_AIBase>();
        if (ai == null) return 50;
        Type aiType = ai.GetType();
        Vector2Int cell = GridPathfinder.WorldToGrid(worldPos);
        if (mSafetyBoards.TryGetValue(aiType, out var board))
        {
            if (board.TryGetValue(cell, out int score)) return score;
            // If cell missing, approximate by nearest known neighbor (simple fallback)
            // Find closest enemy distance heuristic as minimal fallback cost
            return 50; // neutral fallback
        }
        return 50; // not ready fallback
    }

    public void ClearBoards()
    {
        mSafetyBoards.Clear();
        IsReady = false;
    }

    private IEnumerator RebuildAllBoardsIncremental()
    {
        mIsRebuilding = true;
        IsReady = false;
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
        IsReady = true;
        mIsRebuilding = false;
    }

    private IEnumerator BuildBoardForTypeIncremental(Type aiType)
    {
        var board = new Dictionary<Vector2Int, int>(4096);
        var enemyCells = new List<Vector2Int>(256);
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
        var q = new Queue<Vector2Int>(enemyCells.Count * 8);
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
        // Convert distance to score (higher is safer). Clamp 0-100.
        foreach (var kv in new List<KeyValuePair<Vector2Int,int>>(board))
        {
            board[kv.Key] = Mathf.Clamp(kv.Value, 0, 100);
        }
        mSafetyBoards[aiType] = board;
    }
}
