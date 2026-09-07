using UnityEngine;
using TB.Card;

[CreateAssetMenu(fileName = "EventCardOS", menuName = "Scriptable Objects/Card/EventCard")]
public class EventCardSO : CardSO
{
    [SerializeField]
    private EventCardType _type;
    public EventCardType Type => _type;

    public override Sprite GetFrontSprite(CardSkinSO s)
    {   
        return s.GetEventCardSprite(_type);
    }
}
