using TB.GameState;
using UnityEngine;

public abstract class StateSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static GameState _conditionState;

    public void Initialize(GameState conditionState)
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        _conditionState = conditionState;
    }

    public static T Instance
    {
        get
        {
            if(_conditionState != GameState.Ingame)
            {
                Debug.LogWarning("비정상적인 상황에서 싱글톤 인스턴스 접근 시도됨");
                return null;
            }
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                    
                }
            }
            return _instance;
        }
    }

    public static void SetConditionState(GameState conditionState) 
    { 
        _conditionState = conditionState;
    }
}
