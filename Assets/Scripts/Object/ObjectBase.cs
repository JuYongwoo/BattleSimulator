using UnityEngine;

public class ObjectBase : MonoBehaviour
{
    [HideInInspector]
    public int mID = -1;
    [HideInInspector]
    public GameData.ObjectType mObjectType = GameData.ObjectType.None;

    protected virtual void Awake() //자식 클래스에서 override 할 땐 반드시 base.Awake 이후 추가할 것
    {
        ObjectManager.mAll_Of_Game_Objects[ObjectManager.mIDIndex] = this.gameObject; //풀링
        mID = ObjectManager.mIDIndex++; //편의를 위해 리스트 인덱스와 ID를 같게한다
    }
}

