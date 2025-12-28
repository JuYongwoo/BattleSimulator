using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemBase_Heal1 : ObjectBase_ItemBase
{
    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.Heal;
        mRespawnCount = 10;
    }
    public override void action(int pReachedAIsIDNumber)
    {
        base.action(pReachedAIsIDNumber);

        ObjectBase_AIBase mAIClass = ObjectManager.mAll_Of_Game_Objects[pReachedAIsIDNumber].GetComponent<ObjectBase_AIBase>();

        mAIClass.mCurrentHP += 30;
        if (mAIClass.mCurrentHP > GameData.mMaxHP)
        {
            mAIClass.mCurrentHP = GameData.mMaxHP;
        }
        this.gameObject.SetActive(false);
    }

}
