using UnityEngine;
using TB.Card.MissionCard;
[CreateAssetMenu(fileName = "MissionCard", menuName = "Scriptable Objects/Card/MissionCard")]
public class MissionCardSO : CardSO
{
    [Header("미션 카드 기본 정보")]
    [SerializeField] private int _id;
    [SerializeField] private int _point;
    public int Point => _point;
    public int Id => _id;
    [Header("미션 달성 정보")]
    [SerializeField] private MissionConditionData _missionConditionData;
    public MissionConditionData MissionConditionData => _missionConditionData;
    public int CurrentCount { get; set; }

    public override Sprite GetFrontSprite(CardSkinSO cardSkin)
    {
        return cardSkin.GetMissionCardSprite(_id);
    }
}
