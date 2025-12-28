using UnityEngine;

public class ObjectBase_RespawnPlaceBase : ObjectBase //무언가에 닿았을 때 이벤트를 처리해야 하는 것은 아이템으로 본다.
{
    [HideInInspector]
    public ObjectBase mPlaceOwner;

    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.RespawnPlace;
    }

}
