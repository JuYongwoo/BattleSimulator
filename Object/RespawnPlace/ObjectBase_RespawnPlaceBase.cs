using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectBase_RespawnPlaceBase : ObjectBase //무언가에 닿았을 때 이벤트를 처리해야 하는 것은 아이템으로 본다.
{
    public ObjectBase mPlaceOwner;

    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.RespawnPlace;
    }

}
