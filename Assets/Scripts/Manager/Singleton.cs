using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    //Debug.LogWarning($"[Singleton<{typeof(T).Name}>] 씬에 인스턴스가 없습니다. 자동 생성합니다. Inspector 값은 기본값으로 초기화됩니다.");
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 인스턴스가 없으면 이 객체를 전역 인스턴스로 등록하고 파괴되지 않게 유지
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            return;
        }

        // 동일한 인스턴스라면 아무 작업도 하지 않음
        if (_instance == this) return;

        var existingMb = _instance as MonoBehaviour;
        var existingGo = existingMb?.gameObject;

        bool existingIsPersistent = existingGo != null && IsInDontDestroyOnLoadScene(existingGo);
        bool thisIsPersistent = IsInDontDestroyOnLoadScene(gameObject);

        // 특별 규칙:
        // - 기존 인스턴스가 DontDestroyOnLoad로 넘어온 전역 인스턴스이고
        // - 현재 Awake 중인(this) 객체는 씬에 배치된(즉, DontDestroy가 아닌) 오브젝트라면
        //   기존 전역 인스턴스를 삭제하고 씬에 배치된 객체를 전역 인스턴스로 사용한다.
        if (existingIsPersistent && !thisIsPersistent)
        {
            try
            {
                if (existingGo != null)
                    Destroy(existingGo);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Singleton<{typeof(T).Name}>] 기존 전역 인스턴스 파괴 중 예외: {ex.Message}");
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            return;
        }

        // 그 외의 경우(기존 인스턴스 유지 우선) 새로 생긴 객체는 파괴한다.
        Destroy(gameObject);
    }

    // DontDestroyOnLoad로 이동된 객체는 특별한 씬 이름을 갖습니다.
    // Unity의 내부 특수 씬 이름을 사용해서 체크합니다.
    private bool IsInDontDestroyOnLoadScene(GameObject go)
    {
        // Unity가 DontDestroyOnLoad로 이동한 오브젝트의 씬 이름은 "DontDestroyOnLoad"입니다.
        // 안전하게 널 체크 후 비교.
        return go != null && go.scene.name == "DontDestroyOnLoad";
    }

    private static bool HasAssignedSerializedFields(T obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var field in fields)
        {
            // public 이거나 [SerializeField]인 필드만 검사
            bool isPublic = field.IsPublic;
            bool isSerializedField = field.GetCustomAttributes(typeof(SerializeField), true).Any();
            if (!isPublic && !isSerializedField) continue;

            var value = field.GetValue(obj);
            if (value == null) continue;

            // 레퍼런스 타입이고 null이 아니면 할당된 것으로 간주
            if (!field.FieldType.IsValueType) return true;

            // 값 타입의 경우 기본값과 다른지 체크 (int,float,bool 등)
            var defaultValue = Activator.CreateInstance(field.FieldType);
            if (!Equals(value, defaultValue)) return true;
        }
        return false;
    }
}

// 자동 초기화를 씬 로드 이후로 유지합니다.
// (AfterSceneLoad에서 Instance 접근해도, 씬에 배치된 객체가 Awake에서 기존 인스턴스를 대체하도록 구현되어 있습니다.)
internal static class SingletonAutoInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitAllSingletons()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract) continue;

                    var cur = t.BaseType;
                    while (cur != null && cur != typeof(object))
                    {
                        if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(Singleton<>))
                        {
                            try
                            {
                                var singletonClosed = typeof(Singleton<>).MakeGenericType(t);
                                var prop = singletonClosed.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                                prop?.GetValue(null);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Singleton 자동 초기화 실패: {t.FullName} -> {ex.Message}");
                            }
                            break;
                        }
                        cur = cur.BaseType;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SingletonAutoInitializer 실패: {ex.Message}");
        }
    }
}