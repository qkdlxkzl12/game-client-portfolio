using System.Linq;
using TB.Card;
using TB.PokerHand;

public static class PokerHandEvaluator
{
    public static readonly int HAND_SIZE = 5;

    public static bool EvaluateHand(HandAnalysis handAnalysis, HandRank handRank)
    {
        return handRank switch
        {
            HandRank.HighCard => true,
            HandRank.OnePair => CanBeOnePair(handAnalysis),
            HandRank.TwoPair => CanBeTwoPair(handAnalysis),
            HandRank.Triple => CanBeTriple(handAnalysis),
            HandRank.Straight => CanBeStraight(handAnalysis),
            HandRank.Flush => CanBeFlush(handAnalysis),
            HandRank.FullHouse => CanBeFullHouse(handAnalysis),
            HandRank.FourOfAKind => CanBeFourCard(handAnalysis),
            HandRank.StraightFlush => CanBeStraightFlush(handAnalysis),
            HandRank.RoyalFlush => CanBeRoyalFlush(handAnalysis),
            _ => false,
        };
    }
    public static bool CanBeOnePair(HandAnalysis handAnalysis)
    {
        // 패가 3장 미만이라면 항상 원페어 가능
        if (handAnalysis.CardCount < HAND_SIZE - 2) // < 3
            return true;

        // 3장 이상 같은 카드가 있으면 트리플 이상이므로 원페어 불가
        if (handAnalysis.GetKindCount(2, true) > 0)
            return false;

        // 패가 5장 미만이면 아직 쌍이 없어도 원페어 가능성 열려 있음
        if (handAnalysis.CardCount < HAND_SIZE) // < 5
            return true;

        // 패가 5장일 때는 딱 한 쌍일 때만 원페어 확정
        return handAnalysis.GetKindCount(2) == 1;
    }
    public static bool CanBeTwoPair(HandAnalysis handAnalysis)
    {
        //패가 4장 미만이라면 항상 투페어 가능
        if (handAnalysis.CardCount < HAND_SIZE - 1) 
            return true;

        // 3장 이상 같은 카드가 있으면 트리플 이상이므로 투페어 불가
        if (handAnalysis.GetKindCount(2, true) > 0)
            return false;

        return handAnalysis.GetKindCount(0, true) < 4; //무늬 종류가 3개 이상이면 false
    }
    public static bool CanBeTriple(HandAnalysis handAnalysis)
    {
        //카드가 4장 미만이라면 항상 트리플 가능 4/5
        if (handAnalysis.CardCount < HAND_SIZE - 1) 
            return true;

        // 4장 이상 같은 카드가 있으면 포카드 임으로 트리플 불가
        if (handAnalysis.GetKindCount(0, true) > 3)
            return false;

        //4장일 때 투페어 존재 or 원페어(+트리플) 없을 시 트리플 불가
        if (handAnalysis.GetKindCount(2) == 2 || handAnalysis.GetKindCount(1,true) < 1) 
            return false;

        return true;
    }
    public static bool CanBeStraight(HandAnalysis handAnalysis)
    {
        return handAnalysis.CanBeStraight;
    }
    public static bool CanBeFlush(HandAnalysis handAnalysis)
    {
        return handAnalysis.CanBeFlush;
    }
    public static bool CanBeFullHouse(HandAnalysis handAnalysis)
    {
        //카드가 3장 미만이라면 항상 풀하우스 가능
        if (handAnalysis.CardCount < HAND_SIZE - 2)
            return true;
        if(handAnalysis.GetKindCount(0,true) > 2 || handAnalysis.GetKindCount(3,true) > 0)
            return false;
        return true;
    }

    public static bool CanBeFourCard(HandAnalysis handAnalysis)
    {
        //카드가 3장 미만이라면 항상 포카드 가능
        if (handAnalysis.CardCount < HAND_SIZE - 2)
            return true;

        //숫자 종류가 3개 이상이거나 투페어(+풀하우스) 상황일 경우 false
        if (handAnalysis.GetKindCount(0,true) >= 3 || handAnalysis.GetKindCount(1, true) > 1)
            return false;
        return true;
    }

    public static bool CanBeStraightFlush(HandAnalysis handAnalysis)
    {
        return handAnalysis.CanBeStraight && handAnalysis.CanBeFlush;
    }

    public static bool CanBeRoyalFlush(HandAnalysis handAnalysis)
    {
        if(!handAnalysis.OnlyBroadway)
            return false;
        return CanBeStraightFlush(handAnalysis);
    }
}
