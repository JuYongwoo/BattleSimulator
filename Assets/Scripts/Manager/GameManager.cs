using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    static public int mIDIndex = 0;


    static public Dictionary<int, GameObject> mAll_Of_Game_Objects = new Dictionary<int, GameObject>();



    CameraManager mCameraManager = new CameraManager();
    OccupyScoreManager mOccupyScoreManager = new OccupyScoreManager();
    StatViewManager mKillScoreManager = new StatViewManager();
    RespawnManager mRespawnManager = new RespawnManager();




    ///
    /// 
    /// 
    ///
    protected void Awake()
    {
        mCameraManager.Awake();
        mKillScoreManager.Awake();
        mOccupyScoreManager.Awake();
        mRespawnManager.Awake();

    }

    private void Start()
    {
        mCameraManager.Start();
        mKillScoreManager.Start();
        mOccupyScoreManager.Start();
        mRespawnManager.Start();
    }


    // Update is called once per frame
    private void Update()
    {
        mCameraManager.Update();
        mKillScoreManager.Update();
        mOccupyScoreManager.Update();
        mRespawnManager.Update();


    }






}
