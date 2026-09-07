using Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//대화 도중 등장하는 선택창
public class UI_SelectChoice : UI_Popup, IInitializablePopup<Data.Choice[]>
{
    enum Images
    {
        BlackPanel,
        Choice1,
        Choice2, 
        Choice3,
    }
    private Image[] _choises;

    #region Override
    public override bool IsTracked => false;
    public override bool UseBlinder => false;
    public override bool AffectedCancel => false;

    public override bool Init()
    {
        if(base.Init() == false) 
            return false;
        BindImages(typeof(Images));
        _choises = new Image[3]
        {
            GetImage((int)Images.Choice1),
            GetImage((int)Images.Choice2),
            GetImage((int)Images.Choice3),
        };
        for(int i = 0; i < _choises.Length; i++)
        {
            int index = i;
            var image = _choises[i];
            BindEvent(image.gameObject, _ => image.transform.DOScale(1.1f, 0.1f),Define.EUIEvent.PointerEnter);
            BindEvent(image.gameObject, _ => image.transform.DOScale(1f, 0.1f), Define.EUIEvent.PointerExit);
            BindEvent(image.gameObject, _ => Managers.Game.Dialogue.SelectChoice(index), Define.EUIEvent.Click);
            BindEvent(image.gameObject, _ => Managers.UI.ClosePopupUI(this), Define.EUIEvent.Click);
        }
        return true;
    }

    protected override Sequence CreateOpenSequence()
    {
        var openSequence = base.CreateOpenSequence();
        var target = GetImage((int)Images.BlackPanel).rectTransform;
        openSequence.Append(target.DOAnchorPosY(0f, 0.4f).SetEase(Ease.OutQuad));
        openSequence.onPlay += () =>
        {
            target.anchoredPosition = new Vector2(0f, -250f);
        };
        return openSequence;
    }

    protected override Sequence CreateCloseSequence()
    {
        var closeSequence = base.CreateCloseSequence();
        closeSequence.Append(transform.DOMoveY(-250f, 0.75f).SetEase(Ease.InQuad));
        return closeSequence;
    }
    #endregion
    public void Initialize(Choice[] choiceDatas)
    {
        for(int i = 0; i < _choises.Length; i++)
        {
            if (i < choiceDatas.Length)
            {
                _choises[i].gameObject.SetActive(true);
                _choises[i].GetComponentInChildren<TMP_Text>().text = choiceDatas[i].ChoiceText;
            }
            else
            {
                _choises[i].gameObject.SetActive(false);
            }
        }
    }
}
