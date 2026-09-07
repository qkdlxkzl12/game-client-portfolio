using DG.Tweening;
using System;
using UnityEngine;

public abstract class UI_BoardItem : UI_Base
{
    //배치한 위치 정보
    public Vector2Int Point { get; private set; }
    //바로 오른쪽 위치
    public Vector2Int RightPoint => Point + Vector2Int.right;
    //생성 시 연출
    public virtual void Spawn(int templateId, Vector2Int point, Action onComplete)
    {
        MoveToPosition(point, 0f, onComplete);
    }
    //이동 관련 로직
    public virtual Sequence MoveToPosition(Vector2Int point, float duration = 0.5f, Action onComplete = null)
    {
        Point = point;
        var seq = DOTween.Sequence()
            .Append(Rect.DOAnchorPos(IncidentBoardInfo.GetAnchoredPos(point), duration).SetEase(Ease.OutBack))
            .Join(Rect.DOScale(1f, duration).SetEase(Ease.OutBack))
            .OnComplete(() => onComplete?.Invoke());
            return seq;
    }
    //핀 연출을 위한 아이템의 핀 위치 제공
    public RectTransform GetPinPoint()
    {
        var pointRoot = Util.FindChild<RectTransform>(gameObject, "PinPoint", true);
        RectTransform point= pointRoot.GetComponentInChildren<RectTransform>();
        return point;
    }
}

