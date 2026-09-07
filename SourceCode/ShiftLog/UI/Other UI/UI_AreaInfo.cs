using DG.Tweening;
using UnityEngine;
using static Define;

public class UI_AreaInfo : UI_Popup, IInitializablePopup<EAreaType>
{
    public override bool UseBlinder => false;
    public override bool IsTracked => false;
    public override bool IsInterruptible => true;

    enum TMP_Text
    {
        AreaName,
    }

    public override bool Init()
    {
        if(base.Init()==false)
            return false;
        BindTexts(typeof(TMP_Text));
        return true;
    }

    protected override Sequence CreateOpenSequence()
    {
        var openSequence =  base.CreateOpenSequence();
        GetText((int)TMP_Text.AreaName).color = new Color(1, 1, 1, 0);

        openSequence.AppendInterval(0.2f)
            .Append(GetText((int)TMP_Text.AreaName).DOFade(1, 0.5f)).SetEase(Ease.OutQuad)
            .AppendInterval(.75f)
            .Append(GetText((int)TMP_Text.AreaName).DOFade(0, 0.25f)).SetEase(Ease.OutQuad);

        openSequence.onComplete += () =>
        {
            ClosePopupUI();
        };

        return openSequence;
    }

    public void Initialize(EAreaType areaType)
    {
        GetText((int)TMP_Text.AreaName).text = $"{Managers.Object.FindArea(areaType).AreaDisplayName}";
    }
}
