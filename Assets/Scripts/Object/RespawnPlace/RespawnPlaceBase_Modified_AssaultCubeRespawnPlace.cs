using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RespawnPlaceBase_Modified_AssaultCubeRespawnPlace : ObjectBase_RespawnPlaceBase
{
    protected override void Awake()
    {
        base.Awake();
        mPlaceOwner = new AIBase_Modified_AssaultCube();
    }
}
