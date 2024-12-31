using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectBase_OccupyPlaceBase : ObjectBase
{
    public ObjectBase_AIBase mPlaceOwner;
    public float mOccupyRange = 20.0f; // 점령 범위

    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.OccupyPlace;
    }
    public virtual void action(int pReachedAIsIDNumber)
    {
    }
}
