
public class RespawnPlaceBase_CSRespawnPlace : ObjectBase_RespawnPlaceBase
{
    protected override void Awake()
    {
        base.Awake();
        mPlaceOwner = new AIBase_CS();
    }
}