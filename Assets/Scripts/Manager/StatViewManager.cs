using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class StatViewManager
{
    public UIBase_StatUI mScoreUI = null;

    public void Awake()
    {
        mScoreUI = GameObject.FindFirstObjectByType<UIBase_StatUI>(); //첫번째 GamaManager 스크립트를 가진 오브젝트를 찾는다. //GameManager는 하나만 존재한다
    }

    public void Start()
    {
    }
    public void Update()
    {
        if (mScoreUI != null) uiUpdate();
    }

    public void uiUpdate()
    {

        //좌측 상단 점수판
        mScoreUI.GetComponent<Text>().text = "";
        foreach(var obj in GameManager.mAll_Of_Game_Objects)
        {
            ObjectBase_AIBase lAI = obj.Value.GetComponent<ObjectBase_AIBase>();
            if (lAI == null) continue;

            mScoreUI.GetComponent<Text>().text +=
                lAI.name
                + "'s HP: "
                + lAI.mCurrentHP
                + "  /  Ammo:"
                + lAI.mCurrentAmmo[lAI.mUsingWeapon]
                + "\n";

        }


    }

}
