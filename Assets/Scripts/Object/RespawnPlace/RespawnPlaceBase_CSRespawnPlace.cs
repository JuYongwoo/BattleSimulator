using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RespawnPlaceBase_CSRespawnPlace : ObjectBase_RespawnPlaceBase
{
    protected override void Awake()
    {
        base.Awake();
        mPlaceOwner = new AIBase_CS();
    }
}