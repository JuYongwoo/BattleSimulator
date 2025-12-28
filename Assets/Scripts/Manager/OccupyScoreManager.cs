using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OccupyScoreManager : Singleton<OccupyScoreManager>
{
    public GameObject occupyPlace = null; // 점령 장소 오브젝트
    private Dictionary<System.Type, float> teamScores; // 팀별 점수
    public UIBase_OccupyScoreUI mOccupyUI = null;
    float mDataSaveCount = 0.0f;

    protected override void Awake() {

        base.Awake();
        mOccupyUI = GameObject.FindFirstObjectByType<UIBase_OccupyScoreUI>(); //첫번째 GamaManager 스크립트를 가진 오브젝트를 찾는다. //GameManager는 하나만 존재한다

    }

    public void Start()
    {
        foreach(var obj in ObjectManager.mAll_Of_Game_Objects)
        {
            if (obj.Value.GetComponent<ObjectBase_OccupyPlaceBase>() != null)
            {
                occupyPlace = obj.Value;
            }
        }

        InitializeTeamScores();
    }

    void InitializeTeamScores()
    {
        teamScores = new Dictionary<System.Type, float>();

        // AI_Base를 상속받는 모든 타입을 검색
        var aiTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(ObjectBase_AIBase)));

        // 각 타입에 대해 팀 점수를 0으로 초기화
        foreach (var aiType in aiTypes)
        {
            if (!teamScores.ContainsKey(aiType))
            {
                teamScores[aiType] = 0;
            }
        }
    }

    public void Update()
    {
        if (occupyPlace == null) return;

        // 모든 AI를 순회하면서 점령 범위 안에 있는지 확인
        foreach (var obj in ObjectManager.mAll_Of_Game_Objects)
        {
            ObjectBase_AIBase aiBase = obj.Value.GetComponent<ObjectBase_AIBase>();
            if (aiBase != null
                && aiBase.gameObject.activeSelf
                && Vector3.Distance(obj.Value.transform.position, occupyPlace.transform.position) <= occupyPlace.GetComponent<ObjectBase_OccupyPlaceBase>().mOccupyRange)
            {
                // 해당 AI 타입의 점수를 증가 (중복 팀은 한 번만 계산)
                System.Type aiType = aiBase.GetType();
                if (teamScores.ContainsKey(aiType))
                {
                    teamScores[aiType]+= Time.deltaTime;
                }
            }
        }



        ///////////30초마다 결과 저장

        mDataSaveCount += Time.deltaTime;
        if(mDataSaveCount > 20)
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


        //////////////////UI
        ///
        mOccupyUI.GetComponent<Text>().text = "";

        foreach (var score in teamScores)
        {
            mOccupyUI.GetComponent<Text>().text += score.Key.Name + " Team Score: " + (int)score.Value + "\n";
        }
    }

}
