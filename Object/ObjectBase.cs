
using UnityEngine;

public class ObjectBase : MonoBehaviour
{
    public int mID = -1;
    public GameData.ObjectType mObjectType = GameData.ObjectType.None;

    protected virtual void Awake() //자식 클래스에서 override 할 땐 반드시 base.Awake 이후 추가할 것
    {
        GameManager.mAll_Of_Game_Objects[GameManager.mIDIndex] = this.gameObject; //풀링
        mID = GameManager.mIDIndex++; //편의를 위해 리스트 인덱스와 ID를 같게한다
    }
    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

}

