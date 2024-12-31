using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectBase_ItemBase : ObjectBase //무언가에 닿았을 때 이벤트를 처리해야 하는 것은 아이템으로 본다.
{
    public bool mIsRespawning = false;
    public int mRespawnCount = 9999;

    protected override void Awake()
    {
        base.Awake();

    }

    public virtual void action(int pReachedAIsIDNumber) //모든 아이템은 도착하면 도착한 AI에게 도착 이벤트, 세부적인 것은 override하여 구현
    {
    }

}
