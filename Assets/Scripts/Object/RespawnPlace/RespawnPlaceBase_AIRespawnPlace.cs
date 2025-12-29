public class RespawnPlaceBase_AIRespawnPlace : ObjectBase_RespawnPlaceBase
{
    public ObjectBase_AIBase mPlaceOwner_AI;

    protected override void Awake()
    {
        base.Awake();
        mPlaceOwner = mPlaceOwner_AI;
    }
}
