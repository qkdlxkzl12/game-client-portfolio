using System;
using TB.Card;
using TB.GameState;
using UnityEngine;

public class CardManager : StateSingleton<CardManager>
{
    [Header("PlayingCard")]
    [SerializeField] private PlayingCardSO[] _spadeCards;
    [SerializeField] private PlayingCardSO[] _clubCards;
    [SerializeField] private PlayingCardSO[] _diamondCards;
    [SerializeField] private PlayingCardSO[] _heartCards;
    [Header("EventCard")]
    [SerializeField] private EventCardSO[] _eventCards;
    [Header("MissionCard")]
    [SerializeField] private Sprite[] _missionCards;
    public void Awake()
    {
        Initialize(GameState.Ingame);
    }

    public EventCardSO GetEventCard(EventCardType type) => _eventCards[(int)type - 1];
    //public Sprite GetMissionCardSprite() => _missionCardSprites;

    public PlayingCardSO GetPlayingCard(PlayingCardType type)
    {
        var frontSprites = type.Suit switch
        {
            CardSuit.Spade => _spadeCards,
            CardSuit.Club => _clubCards,
            CardSuit.Diamond => _diamondCards,
            CardSuit.Heart => _heartCards,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type.Suit), type.Suit, "지원하지 않는 카드 문양입니다")
        };
        int index = (int)type.Rank - 2;
        if (index < 0 || index >= frontSprites.Length)
            throw new ArgumentOutOfRangeException(nameof(type.Rank), type.Rank, "지원하지 않는 카드 랭크입니다");
        return frontSprites[index];
    }

    public PlayingCardSO[] GetAllPlayingCard()
    {
        int totalCards = _spadeCards.Length + _clubCards.Length + _diamondCards.Length + _heartCards.Length;
        PlayingCardSO[] allCards = new PlayingCardSO[totalCards];
        int index = 0;
        foreach (var card in _spadeCards)
            allCards[index++] = card;
        foreach (var card in _clubCards)
            allCards[index++] = card;
        foreach (var card in _diamondCards)
            allCards[index++] = card;
        foreach (var card in _heartCards)
            allCards[index++] = card;
        return allCards;
    }
}
