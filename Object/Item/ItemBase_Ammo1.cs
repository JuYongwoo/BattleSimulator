using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemBase_Ammo1 : ObjectBase_ItemBase
{

    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.Ammo;
        mRespawnCount = 10;
    }
    public override void action(int pReachedAIsIDNumber)
    {
        base.action(pReachedAIsIDNumber);
        ObjectBase_AIBase mAIClass = GameManager.mAll_Of_Game_Objects[pReachedAIsIDNumber].GetComponent<ObjectBase_AIBase>();

        mAIClass.mCurrentAmmo[mAIClass.mUsingWeapon] += GameData.mWeaponDataDictionary[mAIClass.mUsingWeapon].mInitBullets; //들고 있는 무기 탄창을 무기 종류의 Initbullet만큼 더한다
        if (mAIClass.mCurrentAmmo[mAIClass.mUsingWeapon]
            > GameData.mWeaponDataDictionary[mAIClass.mUsingWeapon].mMaxBullets) //등고있는 무기 종류의Max 치를 넘어서면 Max로 만든다.
        {
            mAIClass.mCurrentAmmo[mAIClass.mUsingWeapon] = GameData.mWeaponDataDictionary[mAIClass.mUsingWeapon].mMaxBullets;
        }
        this.gameObject.SetActive(false); // Deactivates the object
    }


}
