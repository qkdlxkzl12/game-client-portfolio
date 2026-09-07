using System;
using TB.Card;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSkinSO", menuName = "Scriptable Objects/CardSkin")]
public class CardSkinSO : ScriptableObject
{
    [Header("Back Image")]
    [SerializeField] private Sprite _faceDownSprite;
    [Header("PlayingCard Image")]
    [SerializeField] private Sprite[] _spadeFrontSprites;
    [SerializeField] private Sprite[] _clubFrontSprites;
    [SerializeField] private Sprite[] _diamondFrontSprites;
    [SerializeField] private Sprite[] _heartFrontSprites;
    [Header("EventCard")]
    [SerializeField] private Sprite[] _eventCardSprites;
    [Header("MissionCard")]
    [SerializeField] private Sprite[] _missionCardSprites;
    [Header("Token")]
    [SerializeField] private Sprite[] _spadeTokenSprites;
    [SerializeField] private Sprite[] _clubTokenSprites;
    [SerializeField] private Sprite[] _diamondTokenSprites;
    [SerializeField] private Sprite[] _heartTokenSprites;
    [Header("Petrified-Token")]
    [SerializeField] private Sprite[] _spadePetrifiedTokenSprites;
    [SerializeField] private Sprite[] _clubPetrifiedTokenSprites;
    [SerializeField] private Sprite[] _diamondPetrifiedTokenSprites;
    [SerializeField] private Sprite[] _heartPetrifiedTokenSprites;


    public Sprite GetBackSprite() => _faceDownSprite;
    public Sprite GetEventCardSprite(EventCardType type) => _eventCardSprites[(int)type - 1];
    public Sprite GetMissionCardSprite(int id) => _missionCardSprites[id];

    public Sprite GetPlayingCardSprite(PlayingCardType type)
    {
        var frontSprites = type.Suit switch
        {
            CardSuit.Spade => _spadeFrontSprites,
            CardSuit.Club => _clubFrontSprites,
            CardSuit.Diamond => _diamondFrontSprites,
            CardSuit.Heart => _heartFrontSprites,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type.Suit), type.Suit, "지원하지 않는 카드 문양입니다")
        };
        int index = (int)type.Rank - 2;
        if (index < 0 || index >= frontSprites.Length)
            throw new ArgumentOutOfRangeException(nameof(type.Rank), type.Rank, "지원하지 않는 카드 랭크입니다");
        return frontSprites[index];
    }

    public Sprite GetTokenSprite(PlayingCardType type, bool isPetrified = false)
    {
        Sprite[] tokenSprites;
        if(isPetrified)
        {
            tokenSprites = type.Suit switch
            {
                CardSuit.Spade => _spadePetrifiedTokenSprites,
                CardSuit.Club => _clubPetrifiedTokenSprites,
                CardSuit.Diamond => _diamondPetrifiedTokenSprites,
                CardSuit.Heart => _heartPetrifiedTokenSprites,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type.Suit), type.Suit, "지원하지 않는 카드 문양입니다")
            };
        }
        else
        {
            tokenSprites = type.Suit switch
            {
                CardSuit.Spade => _spadeTokenSprites,
                CardSuit.Club => _clubTokenSprites,
                CardSuit.Diamond => _diamondTokenSprites,
                CardSuit.Heart => _heartTokenSprites,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type.Suit), type.Suit, "지원하지 않는 카드 문양입니다")
            };
        }
        int index = (int)type.Rank - 2;
        if (index < 0 || index >= tokenSprites.Length)
            throw new ArgumentOutOfRangeException(nameof(type.Rank), type.Rank, "지원하지 않는 카드 랭크입니다");
        return tokenSprites[index];
    }
}
