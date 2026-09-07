using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_GetItem : UI_Popup, IInitializablePopup<Define.AcquisitionInfo>
{
    public enum Images
    {
        Ray,
        ItemIcon,
        ShiningStar1,
        ShiningStar2,
        ShiningStar3,
    }
    public enum Objects 
    { 
        Effect, 
    }

    public override bool IsTracked => false;
    public override bool IsInterruptible => false;
    public override bool UseBlinder => false;

    Define.AcquisitionInfo _info; 
    private Sequence _shiningSeq;
    private Tween _rayTween;

    public override bool Init()
    {
        if (base.Init()== false)
            return false;
        BindImages(typeof(Images));
        BindObjects(typeof(Objects));
        return true;
    }

    protected override Sequence CreateOpenSequence()
    {
        var effect = GetObject((int)Objects.Effect);
        var openSequence =  base.CreateOpenSequence();
        openSequence.Append(GetObject((int)Objects.Effect).transform.DOScale(1f, 0.75f).SetEase(Ease.OutBounce));
        openSequence.AppendInterval(0.25f);
        openSequence.onPlay += () =>
        {
            effect.transform.localScale = Vector3.zero;
             GetImage((int)Images.ItemIcon).sprite = Managers.Data.ItemDic[_info.TargetId].Sprite.Load(); 
            
            Managers.Sound.Play(Define.ESound.Effect, "Effect_GetItem");

            var shiningSeq = DOTween.Sequence().SetLoops(-1);
            _shiningSeq = shiningSeq;
            Image[] ShiningStars = new Image[] { GetImage((int)Images.ShiningStar1), GetImage((int)Images.ShiningStar2), GetImage((int)Images.ShiningStar3) };
            for (int i = 0; i < ShiningStars.Length; i++)
            {
                var star = ShiningStars[i]; 
                float duration = 1.6f - i * 0.2f;
                star.color = new Color(1, 1, 1, 0);
                star.transform.localScale = Vector3.zero;
                var starSeq = DOTween.Sequence()
                    .Join(star.DOFade(1f, duration).SetEase(Ease.OutSine))
                    .Join(star.transform.DOScale(1f, duration).SetEase(Ease.OutSine))
                    .Append(star.DOFade(0f, duration / 2).SetEase(Ease.InSine))
                    .Join(star.transform.DOScale(0f, duration / 2 - 0.2f).SetEase(Ease.InSine).SetDelay(0.2f))
                    .AppendInterval(0.5f);
                float startDelay = (ShiningStars.Length - 1 - i) * 0.15f;
                shiningSeq.Insert(startDelay, starSeq);
            }


            _rayTween = GetImage((int)Images.Ray).transform.DORotate(Vector3.back*360,30,RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1);
        };
        return openSequence;
    }
    protected override Sequence CreateCloseSequence()
    {
        var closeSequence = base.CreateCloseSequence();
        closeSequence.Append(GetObject((int)Objects.Effect).transform.DOScale(0f, 0.2f).SetEase(Ease.OutQuad));
        closeSequence.onComplete += () =>
        {
            _shiningSeq?.Kill();
            _shiningSeq = null;
            _rayTween?.Kill();
            _rayTween = null;
            //상호작용으로 획득한 경우 창 닫을 때 상호작용 종료 알림
            if (_info.Source == Define.EAcquireSource.InteractObject)
                Managers.InteractEffect.EndInteract();
            _info = new Define.AcquisitionInfo();
        };
        return closeSequence;
    }

    public void Initialize(Define.AcquisitionInfo info)
    {
        _info = info;
    }
}
