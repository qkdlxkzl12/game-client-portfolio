using System;
using System.Collections.Generic;
using System.Linq;
using TB.Bingo;
using TB.Card;
using TB.PokerHand;
using UnityEngine;

public class BingoLine
{
    private HandAnalysis _handAnalysis;

    public event Action<BingoLineRef> OnBingoAchieved;
    public HandRank PossibleHands { get; private set; }
    private Queue<int> _kickers = new();
    public Queue<int> Kickers => new Queue<int>(_kickers);
    BingoLineRef _lineRef;

    public BingoLine(int lineIndex)
    {
        _lineRef = BingoLineRegistry.GetLineRef(lineIndex);
        _handAnalysis = new HandAnalysis();
        PossibleHands = ~HandRank.None;
    }

    public void AddHandCard(PlayingCardType type)
    {
        _handAnalysis.Update(type);
        _kickers.Enqueue(type.Rank.ToInt());
        UpdatePossibleHands();
        //토큰 추가 처리 필요
        if (_handAnalysis.CardCount == 5)
        {
            SetKickers();
            OnBingoAchieved?.Invoke(_lineRef);
            Debug.Log($"<color=#FF4F4F>빙고! {GetResult().Rank}</color>");
        }
    }

    private void UpdatePossibleHands()
    {
        foreach (HandRank rank in HandRankHelper.AllRanks)
        {
            if ((PossibleHands & rank) == 0)
                continue;
            if (!PokerHandEvaluator.EvaluateHand(_handAnalysis, rank))
            {
                PossibleHands &= ~rank;
                //Debug.Log($"{rank} 가능성 제거");
            }
            else
            {
                //Debug.Log($"{rank} 가능성 유지");
            }
        }
    }

    public HandResult GetResult()
    {
        if (_handAnalysis.CardCount != 5)
        {
            throw new InvalidOperationException("5장의 카드가 모두 채워지지 않았습니다.");
        }
        foreach (HandRank rank in HandRankHelper.AllRanks.Reverse())
        {
            if ((PossibleHands & rank) != 0)
            {
                return new HandResult(rank, (CardRank)Kickers.Peek());
            }
        }
        return HandResult.Empty;
    }

    private void SetKickers()
    {
        _kickers = new Queue<int>(_kickers
            .Select(r => (_handAnalysis.OnlyAceLow && r == 14) ? 1 : r)
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .Select(g => g.Key).ToList() );
        var test = _kickers.ToArray();
    }
}
