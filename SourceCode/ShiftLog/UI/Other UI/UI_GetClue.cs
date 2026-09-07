using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;

public class UI_GetClue : UI_Popup, IInitializablePopup<Define.AcquisitionInfo>
{
    #region Enum
    enum Images
    {
        Panel,
    }
    enum TMP_Texts
    {
        MessageText,
    }
    #endregion

    public override bool IsTracked => false;
    public override bool UseBlinder => false;
    private Define.AcquisitionInfo _info;
    public override bool Init()
    {
        if(base.Init()==false)
            return false;
        BindImages(typeof(Images));
        BindTexts(typeof(TMP_Texts));
        _info = new Define.AcquisitionInfo();
        return true;
    }
    public void Initialize(Define.AcquisitionInfo info)
    {
        _info = info;
        var clueData = Managers.Data.ClueDic[info.TargetId];
        switch (info.Source)
        {
            case Define.EAcquireSource.Dialogue:
                GetText((int)TMP_Texts.MessageText).text = $"[ªı∑ŒøÓ ¡§∫∏∏¶ »πµÊ«ﬂ¥Ÿ.]";
                break;
            //case EAcquireSource.InteractObject:
            //    GetText((int)TMP_Texts.MassageText).text = $"[ {clueData.Name.AddParticle()} »πµÊ«ﬂ¥Ÿ.]";
            //    break;
            //case EAcquireSource.Sequence:
            //    GetText((int)TMP_Texts.MassageText).text = $"[ {clueData.Name.AddParticle()} »πµÊ«ﬂ¥Ÿ.]";
            //    break;
            default:
                GetText((int)TMP_Texts.MessageText).text = $"[ {clueData.Name.AddParticle()} »πµÊ«ﬂ¥Ÿ.]";
                break;
        }
    }
    protected override Sequence CreateOpenSequence()
    {
        Image panel = GetImage((int)Images.Panel);
        var openSequence = base.CreateOpenSequence();
        openSequence.Append(panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBounce));
        openSequence.onPlay += () =>
        {
            panel.transform.localScale = Vector3.zero;
            Managers.Sound.Play(Define.ESound.Effect, "Effect_GetClue");
        };
        openSequence.onComplete += () =>
        {
            var closeSeq = CreateCloseSequence().SetDelay(2.0f);
            closeSeq.Play();
        };
        return openSequence;
    }

    protected override Sequence CreateCloseSequence()
    {
        var closeSequence =  base.CreateCloseSequence();
        closeSequence.onComplete += () =>
        {
            Managers.UI.ClosePopupUI(this);
            //ªÛ»£¿€øÎ¿∏∑Œ »πµÊ«— ∞ÊøÏ √¢ ¥›¿ª ∂ß ªÛ»£¿€øÎ ¡æ∑· æÀ∏≤
            if(_info.Source == Define.EAcquireSource.InteractObject)
                Managers.InteractEffect.EndInteract();
            _info = new Define.AcquisitionInfo();
        };
        return closeSequence;
    }
}
