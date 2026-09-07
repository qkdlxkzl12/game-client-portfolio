using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
public class UI_ClueCard : UI_BoardItem
{
    enum TMP_Texts
        {
        DisplayText,
    }
    enum Images
    {
        IconImage,
    }
    public int TemplateId { get; private set; }
    public Data.ClueData Data => Managers.Data.ClueDic[TemplateId];

    private RectTransform _parentRect;
    private Canvas _canvas;

    private Vector2 _dragStartOffset;
    public bool IsFollowing {get; set;}
    public bool IsPaced {get; set;}
    public bool IsProcessing {get; set;}
    public bool CanMove => IsPaced == false && IsFollowing == false && IsProcessing == false;
    #region Override
    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        BindImages(typeof(Images));
        BindTexts(typeof(TMP_Texts));
        _parentRect = Rect.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        return true;
    }

    public override void Spawn(int templateId, Vector2Int point, Action onComplete)
    {
        TemplateId = templateId;
        Get<Image>((int)Images.IconImage).sprite = Data.Sprite.Load();
        Get<TMP_Text>((int)TMP_Texts.DisplayText).text = Data.Name;
    }


    public override Sequence MoveToPosition(Vector2Int point, float duration = 0.5f, Action onComplete = null)
    {
        Sequence seq = base.MoveToPosition(point, duration, onComplete);
        seq.OnPlay(() => IsProcessing = true)
           .OnComplete(() =>
           {
               onComplete?.Invoke();
               IsProcessing = false;
               IsPaced = true;
           });
        return seq;
    }
    #endregion
    private bool TryGetLocalPointerPosition(PointerEventData evData, out Vector2 localPoint)
    {
        Camera eventCamera = null;

        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = evData.pressEventCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            evData.position,
            eventCamera,
            out localPoint);
    }

    #region Bind Event
    public void BeginDragFollow(PointerEventData evData)
    {
        if (IsPaced == true)
            return;
        if (TryGetLocalPointerPosition(evData, out Vector2 localPointerPos) == false)
            return;

        _dragStartOffset = Rect.anchoredPosition - localPointerPos;
        IsFollowing = true;
    }
    public void UpdateDragFollow(PointerEventData evData)
    {
        if (IsPaced == true)
            return;
        if (IsFollowing == false)
            return;

        if (TryGetLocalPointerPosition(evData, out Vector2 localPointerPos) == false)
            return;

        Rect.anchoredPosition = localPointerPos + _dragStartOffset;
    }
    public void EndDragFollow(PointerEventData evData)
    {
        if (IsPaced == true)
            return;
        IsFollowing = false;
        _dragStartOffset = Vector2.zero;
    }
    #endregion
}