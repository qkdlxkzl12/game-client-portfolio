using TB.Card;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayingCardOS", menuName = "Scriptable Objects/Card/PlayingCard")]
public class PlayingCardSO : CardSO
{
    [SerializeField]
    PlayingCardType _type;
    public PlayingCardType Type => _type;

    public override Sprite GetFrontSprite(CardSkinSO s)
    {
        return s.GetPlayingCardSprite(_type);
    }
}
