using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OccupyScoreManager : Singleton<OccupyScoreManager>
{
    public GameObject occupyPlace = null;
    private Dictionary<System.Type, float> teamScores;
    public UIBase_OccupyScoreUI mOccupyUI = null;
    float mDataSaveCount = 0.0f;

    // cache frequently used components
    private ObjectBase_OccupyPlaceBase mOccupyPlaceBase;
    private Transform mOccupyTransform;

    protected override void Awake() {
        base.Awake();
        mOccupyUI = GameObject.FindFirstObjectByType<UIBase_OccupyScoreUI>();
    }

    public void Start()
    {
        // find occupy place once
        foreach (var id in ObjectManager.mOccupyIds)
        {
            var go = ObjectManager.mAll_Of_Game_Objects[id];
            occupyPlace = go;
            mOccupyPlaceBase = ObjectManager.mObjectBaseCache[id] as ObjectBase_OccupyPlaceBase;
            if (mOccupyPlaceBase == null) mOccupyPlaceBase = go.GetComponent<ObjectBase_OccupyPlaceBase>();
            mOccupyTransform = ObjectManager.mTransformCache[id];
            break; // use the first
        }
        InitializeTeamScores();
    }

    void InitializeTeamScores()
    {
        teamScores = new Dictionary<System.Type, float>();
        var aiTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(ObjectBase_AIBase)));
        foreach (var aiType in aiTypes)
        {
            if (!teamScores.ContainsKey(aiType)) teamScores[aiType] = 0;
        }
    }

    public void Update()
    {
        if (occupyPlace == null || mOccupyPlaceBase == null) return;
        float range = mOccupyPlaceBase.mOccupyRange;
        Vector3 op = mOccupyTransform != null ? mOccupyTransform.position : occupyPlace.transform.position;

        // iterate only AIs
        foreach (var id in ObjectManager.mAIIds)
        {
            var go = ObjectManager.mAll_Of_Game_Objects[id];
            if (!go.activeSelf) continue;
            var aiBase = ObjectManager.mObjectBaseCache[id] as ObjectBase_AIBase;
            if (aiBase == null) continue;
            Vector3 pos = ObjectManager.mTransformCache[id].position;
            if ((pos - op).sqrMagnitude <= range * range)
            {
                var aiType = aiBase.GetType();
                if (teamScores.ContainsKey(aiType))
                {
                    teamScores[aiType] += Time.deltaTime;
                    // Aggregate occupy time per AI type
                    StatsAggregator.Instance.AddOccupyTime(aiType.Name, Time.deltaTime);
                }
            }
        }

        mDataSaveCount += Time.deltaTime;
        if (mDataSaveCount > 20)
        {
            mDataSaveCount = 0.0f;
            GameDataSaver.SaveOccupyResultsToCSV(("CS", (int)teamScores[typeof(AIBase_CS)]),
                ("OW", (int)teamScores[typeof(AIBase_OW)]),
                ("AssaultCube", (int)teamScores[typeof(AIBase_AssaultCube)]),
                ("Xonotic", (int)teamScores[typeof(AIBase_Xonotic)]),
                ("Modified_AssaultCube", (int)teamScores[typeof(AIBase_Modified_AssaultCube)]),
                ("Modified_Xonotic", (int)teamScores[typeof(AIBase_Modified_Xonotic)])
            );
        }

        // UI update (cached Text)
        var text = mOccupyUI != null ? mOccupyUI.GetComponent<Text>() : null;
        if (text != null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var score in teamScores)
            {
                sb.Append(score.Key.Name).Append(" Team Score: ").Append((int)score.Value).Append('\n');
            }
            text.text = sb.ToString();
        }
    }
}
