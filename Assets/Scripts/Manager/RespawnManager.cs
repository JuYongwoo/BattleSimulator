using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : Singleton<RespawnManager>
{
    private Dictionary<int, float> itemRespawnTimers = new Dictionary<int, float>();
    private Dictionary<int, float> aiRespawnTimers = new Dictionary<int, float>();

    public void Update()
    {
        var currentTime = Time.time;

        // Handle item respawning
        foreach (var obj in ObjectManager.mAll_Of_Game_Objects)
        {
            if (obj.Value.GetComponent<ObjectBase_ItemBase>() == null) continue;

            if (!obj.Value.activeSelf && !obj.Value.GetComponent<ObjectBase_ItemBase>().mIsRespawning) //시간 등록
            {
                obj.Value.GetComponent<ObjectBase_ItemBase>().mIsRespawning = true;
                itemRespawnTimers[obj.Value.GetComponent<ObjectBase>().mID] = currentTime + obj.Value.GetComponent<ObjectBase_ItemBase>().mRespawnCount;
            }
            else if (obj.Value.GetComponent<ObjectBase_ItemBase>().mIsRespawning
                && itemRespawnTimers.ContainsKey(obj.Value.GetComponent<ObjectBase>().mID)
                && currentTime >= itemRespawnTimers[obj.Value.GetComponent<ObjectBase>().mID]) //시간 완료 리스폰
            {
                obj.Value.GetComponent<ObjectBase_ItemBase>().mIsRespawning = false;
                obj.Value.SetActive(true);
                itemRespawnTimers.Remove(obj.Value.GetComponent<ObjectBase>().mID);
            }
        }

        // Handle AI respawning
        foreach (var obj in ObjectManager.mAll_Of_Game_Objects)
        {
            if (obj.Value.GetComponent<ObjectBase_AIBase>() == null) continue;

            if (!obj.Value.activeSelf && !obj.Value.GetComponent<ObjectBase_AIBase>().mIsRespawning)
            {
                obj.Value.GetComponent<ObjectBase_AIBase>().mIsRespawning = true;
                aiRespawnTimers[obj.Value.GetComponent<ObjectBase>().mID] = currentTime + 10;
            }
            else if (obj.Value.GetComponent<ObjectBase_AIBase>().mIsRespawning
                && aiRespawnTimers.ContainsKey(obj.Value.GetComponent<ObjectBase>().mID)
                && currentTime >= aiRespawnTimers[obj.Value.GetComponent<ObjectBase>().mID])
            {
                obj.Value.GetComponent<ObjectBase_AIBase>().mIsRespawning = false;
                obj.Value.GetComponent<ObjectBase_AIBase>().Respawn();
                aiRespawnTimers.Remove(obj.Value.GetComponent<ObjectBase>().mID);
            }
        }
    }
}
