using System;
using TMPro;
using UnityEngine;

public class UI_SolutionCard : UI_BoardItem
{
    public int TemplatedId { get; private set; }
    public Data.SolutionCardData Data => Managers.Data.SolutionCardDic[TemplatedId];
    enum TMP_Texts
    {
        LoreText,
    }

    public override bool Init()
    {
        if(base.Init() == false) 
            return false;
        BindTexts(typeof(TMP_Texts));
        return true;
    }

    public override void Spawn(int templateId, Vector2Int point, Action onComplete)
    {
        TemplatedId = templateId;
        Get<TMP_Text>((int)TMP_Texts.LoreText).text = Data.Lore;
        base.Spawn(templateId, point, onComplete);
    }
}
