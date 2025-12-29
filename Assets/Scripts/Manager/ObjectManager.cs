using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : Singleton<ObjectManager>
{
    static public int mIDIndex = 0;

    // Master registry
    static public readonly Dictionary<int, GameObject> mAll_Of_Game_Objects = new Dictionary<int, GameObject>(256);

    // Fast indices by type
    static public readonly List<int> mAIIds = new List<int>(128);
    static public readonly List<int> mHealIds = new List<int>(64);
    static public readonly List<int> mAmmoIds = new List<int>(64);
    static public readonly List<int> mRespawnIds = new List<int>(32);
    static public readonly List<int> mOccupyIds = new List<int>(16);

    // Component caches to avoid GetComponent in hot loops
    static public readonly Dictionary<int, ObjectBase> mObjectBaseCache = new Dictionary<int, ObjectBase>(256);
    static public readonly Dictionary<int, Collider> mColliderCache = new Dictionary<int, Collider>(256);
    static public readonly Dictionary<int, Transform> mTransformCache = new Dictionary<int, Transform>(256);

    // Register a new game object into manager and build indices/caches
    public static int Register(GameObject go)
    {
        if (go == null) return -1;
        var obj = go.GetComponent<ObjectBase>();
        if (obj == null) return -1;
        if (obj.mID < 0) obj.mID = mIDIndex++;
        int id = obj.mID;
        mAll_Of_Game_Objects[id] = go;
        mObjectBaseCache[id] = obj;
        mTransformCache[id] = go.transform;
        var col = go.GetComponent<Collider>();
        if (col != null) mColliderCache[id] = col;

        switch (obj.mObjectType)
        {
            case GameData.ObjectType.AI:
                if (!mAIIds.Contains(id)) mAIIds.Add(id);
                break;
            case GameData.ObjectType.Heal:
                if (!mHealIds.Contains(id)) mHealIds.Add(id);
                break;
            case GameData.ObjectType.Ammo:
                if (!mAmmoIds.Contains(id)) mAmmoIds.Add(id);
                break;
            case GameData.ObjectType.RespawnPlace:
                if (!mRespawnIds.Contains(id)) mRespawnIds.Add(id);
                break;
            case GameData.ObjectType.OccupyPlace:
                if (!mOccupyIds.Contains(id)) mOccupyIds.Add(id);
                break;
        }
        return id;
    }

    // Unregister and clean caches
    public static void Unregister(int id)
    {
        if (!mAll_Of_Game_Objects.TryGetValue(id, out var go)) return;
        var obj = mObjectBaseCache.TryGetValue(id, out var ob) ? ob : go.GetComponent<ObjectBase>();
        mAll_Of_Game_Objects.Remove(id);
        mObjectBaseCache.Remove(id);
        mColliderCache.Remove(id);
        mTransformCache.Remove(id);
        if (obj != null)
        {
            switch (obj.mObjectType)
            {
                case GameData.ObjectType.AI: mAIIds.Remove(id); break;
                case GameData.ObjectType.Heal: mHealIds.Remove(id); break;
                case GameData.ObjectType.Ammo: mAmmoIds.Remove(id); break;
                case GameData.ObjectType.RespawnPlace: mRespawnIds.Remove(id); break;
                case GameData.ObjectType.OccupyPlace: mOccupyIds.Remove(id); break;
            }
        }
    }
}
