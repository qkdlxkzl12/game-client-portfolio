using System.Collections.Generic;
using System.Linq;
using TB.Card;
public class HandAnalysis
{
    #region 파생 데이터
    public List<PlayingCardType> Cards { get; private set; }
    public int[] RankCounts { get; private set; }
    public int[] SuitCounts { get; private set; }
    public SortedSet<int> SortedRanks { get; private set; }
    #endregion
    #region 상태 플래그
    public bool CanBeStraight { get; private set; } //연속 가능 여부
    public bool CanBeFlush { get; private set; }    //동일 무늬 여부
    public int CardCount => Cards.Count;            //현재 패에 포함된 카드 수
    public bool OnlyBroadway { get; private set; }  //로얄 플러시 가능 여부
    public bool OnlyAceLow { get; private set; }    //백 스트레이트 여부
    #endregion
    public HandAnalysis()
    {
        //기본 초기화
        Cards = new List<PlayingCardType>();
        RankCounts = new int[15]; // 2~14 (Ace=14)
        SuitCounts = new int[5];  // 1~4 (Spade=1, Club=2, Diamond=3, Heart=4)
        SortedRanks = new SortedSet<int>();
        CanBeStraight = true;
        CanBeFlush = true;
        OnlyBroadway = true;
        OnlyAceLow = true;
    }
    public void Update(PlayingCardType newCard)
    {
        //새로운 카드 추가
        Cards.Add(newCard);
        //카드의 숫자와 무늬에 따라 카운트 업데이트
        int rankIndex = newCard.Rank.ToInt(); 
        int suitIndex = newCard.Suit.ToInt();
        RankCounts[rankIndex] += 1;
        SuitCounts[suitIndex] += 1;
        SortedRanks.Add(rankIndex);
        // 상태 플래그 업데이트
        if (OnlyAceLow)
            OnlyAceLow = rankIndex <= CardRank.Five.ToInt() || 
                rankIndex == CardRank.Ace.ToInt();
        if (OnlyBroadway)
            OnlyBroadway = rankIndex >= CardRank.Ten.ToInt();
        if (CanBeStraight)
            CanBeStraight = CheckStraightPossibility();
        if(CanBeFlush)
            CanBeFlush = CheckFlushPossibility();
    }

    //동일 무늬 가능성이 있는지 확인
    private bool CheckFlushPossibility()
    {
        //첫번째 카드 혹은 동일 무늬 카드만 있다면 true
        if (GetSuitKindCount() <= 1) 
            return true;
        return false;
    }
    //연속 가능성이 있는 숫자인지 확인
    private bool CheckStraightPossibility()
    {
        //카드가 한 장 이하라면 true
        if (SortedRanks.Count <= 1)
            return true;
        //같은 숫자가 2장 이상 있으면 false
        if (GetKindCount(1,true) > 0) 
            return false;

        bool hasAce = SortedRanks.Contains(CardRank.Ace.ToInt());
        int min = SortedRanks.Min;
        int max = SortedRanks.Max;
        // Ace가 있을 경우 1과 14중 어떤 역할을 할지 확인
        if (hasAce)
        {
            max = SortedRanks.Reverse().Skip(1).First(); //Ace(14) 제외 후 최대값 갱신

            if (min <= 5 && max >= 9)  //스트레이트 불가 (A2345 or 10JQKA 둘 중 하나만 가능)
                return false;
            else if (min <= 5) // Ace=1로 계산 (low straight 쪽만 가능)
            {
                min = 1;
            }
            else // 이외 상황에는 Ace=14로 계산
            {
                max = 14;
            }
        }
        //연속 가능성이 있는 숫자인지 확인
        if (max - min < 5)
            return true;
        return false;
    }
    /// 패에 같은 숫자가 ofKind개인 그룹의 개수 반환
    public int GetKindCount(int ofKind, bool greaterThen = false)
    {
        int count = 0;
        foreach (var rankCount in RankCounts)
        {
            if(greaterThen)
            {
                if (rankCount > ofKind)
                    count++;
            }
            else  //정확히 ofKind개인 경우만 카운트
            {
                if (rankCount == ofKind)
                    count++;
            }
        }
        return count;
    }
    /// 패에 포한된 무늬 종류 개수 반환
    public int GetSuitKindCount()
    {
        int count = 0;
        foreach (var rankCount in SuitCounts)
        {
            if (rankCount > 0)
                count++;
        }
        return count;
    }
}

