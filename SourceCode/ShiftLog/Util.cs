using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public static class Util
{
    //1회성 콜백 등록
    #region Once
        public static void RegisterOnce(Action<Action> addHandler, Action<Action> removeHandler, Action callback)
    {
        Action wrapper = null;

        wrapper = () =>
        {
            removeHandler?.Invoke(wrapper);
            callback?.Invoke();
        };
        addHandler?.Invoke(wrapper);
    }
    public static void RegisterOnce<TArgs>(Action<Action<TArgs>> addHandler,
    Action<Action<TArgs>> removeHandler, Func<TArgs, bool> condition, Action callback)
    {
        Action<TArgs> wrapper = null;

        wrapper = (args) =>
        {
            if (condition != null && !condition(args))
                return;

            removeHandler?.Invoke(wrapper);
            callback?.Invoke();
        };
        addHandler?.Invoke(wrapper);
    }
    public static void RegisterOnce<T1, T2>(Action<Action<T1, T2>> addHandler,
    Action<Action<T1, T2>> removeHandler, Func<T1, T2, bool> condition, Action callback)
    {
        Action<T1, T2> wrapper = null;

        wrapper = (arg1, arg2) =>
        {
            if (condition != null && !condition(arg1, arg2))
                return;

            removeHandler?.Invoke(wrapper);
            callback?.Invoke();
        };

        addHandler?.Invoke(wrapper);
    }
    public static void RegisterOnce<T1, T2, T3>(Action<Action<T1, T2, T3>> addHandler,
Action<Action<T1, T2, T3>> removeHandler, Func<T1, T2, T3, bool> condition, Action callback)
    {
        Action<T1, T2, T3> wrapper = null;

        wrapper = (arg1, arg2, arg3) =>
        {
            if (condition != null && !condition(arg1, arg2, arg3))
                return;

            removeHandler?.Invoke(wrapper);
            callback?.Invoke();
        };

        addHandler?.Invoke(wrapper);
    }
    #endregion
    //스프라이트 매핑 데이터 로드
    #region SpriteRef
    public static Sprite LoadMappingSprite(string key, int index)
    {
        if (Managers.Data.SpriteMappingDataDic.TryGetValue(key, out var data) == false)
            return null;

        var spriteRef = data.GetSprite(index);
        return spriteRef.Load();
    }
    #endregion
    //클릭 지점 감지
    #region UI
    public static bool IsPointerOverClickableUI(bool onlyButton = false)
    {
        PointerEventData eventData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
            return false;

        GameObject topObject = results[0].gameObject;

        if (onlyButton == true)
            return topObject.GetComponentInParent<UnityEngine.UI.Button>() != null;

        return topObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
               topObject.GetComponentInParent<IPointerClickHandler>() != null;
    }
    public static Vector2 GetAnchoredPosFromOther(RectTransform target, RectTransform reference, Canvas canvas)
    {
        Vector3 worldPos = reference.position;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos);

        RectTransform parentRect = target.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, canvas.worldCamera, out Vector2 localPos);

        return localPos;
    }
    #endregion
}