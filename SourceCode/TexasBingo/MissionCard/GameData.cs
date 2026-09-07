using System;
using System.Collections.Generic;
using TB.Bingo;
using TB.PokerHand;
using TB.Result;
using UnityEngine;

namespace TB.Direction 
{
    public enum Direction
    {
        RightTop,
        LeftTop,
        RightDown,
        LeftDown
    }
    public struct IntRange2D
    {
        public Vector2Int X;
        public Vector2Int Y;

        public IntRange2D(Vector2Int x, Vector2Int y)
        {
            X = x;
            Y = y;
        }
        public IntRange2D(int xMin, int xMax, int yMin, int yMax)
        {
            X = new Vector2Int(xMin, xMax);
            Y = new Vector2Int(yMin, yMax);
        }
        public int XMin => X.x;
        public int XMax => X.y;
        public int YMin => Y.x;
        public int YMax => Y.y;
    }
}

namespace TB.Result
{
    public readonly struct HandResult : IComparable<HandResult>
    {
        public readonly HandRank Rank;
        public readonly int[] Kickers;
        public HandResult(HandRank handRank, int[] kickers)
        {
            Rank = handRank;
            Kickers = kickers;
        }

        public static bool operator >(HandResult a, HandResult b)
        {
            if (a.Rank != b.Rank)
                return a.Rank > b.Rank;
            for (int i = 0; i < a.Kickers.Length; i++)
            {
                if (a.Kickers[i] != b.Kickers[i])
                    return a.Kickers[i] > b.Kickers[i];
            }
            return false; // 동률
        }

        public static bool operator <(HandResult a, HandResult b)
        {
            if (a.Rank != b.Rank)
                return a.Rank < b.Rank;
            for (int i = 0; i < a.Kickers.Length; i++)
            {
                if (a.Kickers[i] != b.Kickers[i])
                    return a.Kickers[i] < b.Kickers[i];
            }
            return false; // 동률
        }

        public int CompareTo(HandResult other)
        {
            if (this > other) return 1;
            if (this < other) return -1;
            return 0;
        }
    }

    public struct LineHandResult
    {
        public readonly HandResult Result;
        public readonly bool IsWin;
        public readonly bool SameHandCategory;
        public bool IsStrongestRank { get; private set; }

        public LineHandResult(HandResult result, bool isWin, bool hasSame)
        {
            Result = result;
            IsWin = isWin;
            SameHandCategory = hasSame;
            IsStrongestRank = false;
        }

        public void CheckStrongest(HandRank strongest) => IsStrongestRank = Result.Rank == strongest;
    }
}

namespace TB.Card
{
    [Serializable]
    public enum CardSuit
    {
        Spade = 1,
        Club,
        Diamond,
        Heart,
    }
    [Serializable]
    public enum CardRank
    {
        Two = 2,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace,
    }
    [Serializable]
    public struct PlayingCardType
    {
        public CardSuit Suit;
        public CardRank Rank;
        public PlayingCardType(CardSuit suit, CardRank rank)
        {
            Suit = suit;
            Rank = rank;
        }
        public override string ToString()
        {
            return $"{Suit} {Rank}";
        }

        public static bool operator ==(PlayingCardType a, PlayingCardType b)
        {
            return a.Rank == b.Rank && a.Suit == b.Suit;
        }

        public static bool operator !=(PlayingCardType a, PlayingCardType b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is PlayingCardType other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Suit, Rank);
        }
    }
    [Serializable]
    public enum EventCardType
    {
        Crossroads,
        Tunnel,
        SandstormRT,
        SandstormLT,
        SandstormRD,
        SandstormLD,
    }
}

namespace TB.Card.MissionCard
{
    [Serializable, Flags]
    public enum BingoLocationType
    {
        None = 0,

        // 행 (0~4)
        Row1 = 1 << 0,
        Row2 = 1 << 1,
        Row3 = 1 << 2,
        Row4 = 1 << 3,
        Row5 = 1 << 4,

        // 열 (5~9)
        Column1 = 1 << 5,
        Column2 = 1 << 6,
        Column3 = 1 << 7,
        Column4 = 1 << 8,
        Column5 = 1 << 9,

        // 대각선 (10, 11)
        Diagonal1 = 1 << 10,
        Diagonal2 = 1 << 11,

        // 그룹
        AnyRow = (1 << 5) - 1,
        AnyColumn = ((1 << 10) - 1) ^ AnyRow,
        AnyDiagonal = (1 << 10) | (1 << 11),
    }
    [Serializable]
    public enum ResultRequirement
    {
        Any,    // 승/패 무관
        Win,    // 반드시 승리
        Lose    // 반드시 패배
    }
    [Serializable]
    public enum HandComparator
    {
        None,       // 족보 요구 없음
        EqualTo,    // 특정 족보와 동일
        AtLeast,    // 특정 족보 이상
        AtMost,     // 특정 족보 이하
    }
    [Serializable]
    public enum RankRequirementMode
    {
        None,           // 집합 요구 없음
        AnyRankTotal,   // 집합 중 아무거나 N회 이상 (TotalMin)
        AllRanksEach,   // 집합의 각 타입을 최소 K회 이상 (PerTypeMin)
        SameRankTotal   // 같은 족보가 존재하는 집합 중 N회 이상  
    }
    [Serializable]
    public enum DistinctnessRule
    {
        None,
        DistinctStraights // 서로 다른 형태의 스트레이트여야 함
    }
    [Serializable]
    public struct MissionConditionData
    {
        public bool RequireBothConditions => Result != ResultRequirement.Any && 
                                            ReferenceHand != HandRank.None;
        [Header("위치")]
        public BingoLocationType LocationType;
        // 승/패 요구
        [Header("결과")]
        public ResultRequirement Result;                 // Any/Win/Lose
        public int ResultAchievementMin;

        [Header("족보")]
        // 족보 기반 요구
        public HandRank ReferenceHand;              // EqualTo/AtLeast에서 사용
        public RankRequirementMode RankRepuireMode;      // Any/All/Same
        public HandComparator HandCompare;               // None/EqualTo/AtLeast
        public int RankAchievementMin;


        [Header("기타")]
        // 보드 상대 강도 요구
        public bool RequireStrongest;                    // true: 전체 조합 중 최강(동률 포함)
    }
}

namespace TB.PokerHand
{
    [Serializable, Flags]
    public enum HandRank
    {
        None = 0,
        HighCard = 1 << 0,
        OnePair = 1 << 1,
        TwoPair = 1 << 2,
        Triple = 1 << 3,
        Straight = 1 << 4,
        Flush = 1 << 5,
        FullHouse = 1 << 6,
        FourOfAKind = 1 << 7,
        StraightFlush = 1 << 8,
        RoyalFlush = 1 << 9,
    }
}

namespace TB.GameState
{
    [Serializable, Flags]
    public enum GameState
    {
        Loading = 1 << 0,
        MainMenu = 1 << 1,
        Ingame = 1 << 2,
        All = Loading | MainMenu | Ingame,
    }
}

namespace TB.Bingo
{
    public enum BingoLineType
    {
        Horizontal,
        Vertical,
        Diagonal
    }
    public class BingoLineRef
    {
        public BingoLineType Type { get; }
        public int Index { get; }
        public BingoLineRef(BingoLineType type, int index)
        {
            Type = type;
            Index = index;
        }
    }
}

namespace TB.Data.Result
{
    [Serializable]
    public struct LineResultRecord
    {
        public int LineTypeIndex;
        public int LineIndex;
        public int HandRankIndex;
        public int[] Kickers;
        public bool IsWin; 

        public LineResultRecord(LineHandResult lineHandResult, BingoLineRef lineRef)
        {
            LineTypeIndex = (int)lineRef.Type;
            LineIndex = lineRef.Index;
            HandRankIndex = (int)lineHandResult.Result.Rank;
            Kickers = lineHandResult.Result.Kickers;
            IsWin = lineHandResult.IsWin;
        }
        public BingoLineRef GetLineInfo()
        {
            return BingoLineRegistry.GetLineRef(LineTypeIndex*5+LineIndex);
        }

        public LineHandResult GetHandResult()
        {
            return new LineHandResult(new HandResult((HandRank)HandRankIndex, Kickers), IsWin, false);
        }
    }

    [Serializable]
    public struct PlayerInfoData
    {
        public int ProfileId;
        public string DisplayName;
        public int TotalScoore;
        public LineResultRecord[] ResultRecords;
        public PlaceData[] Places;
        //ScoreMode에서 미사용
        public int ChosenMissionID;
        public bool AchievedMission;
        public bool IsPlayer;

        public PlayerInfoData(PlayerContext context)
        {
            IsPlayer = context.IsPlayer;
            ProfileId = context.ProfileId;
            DisplayName = context.Nickname;
            TotalScoore = context.Score;
            //판 데이터
            PlaceData[] placeDatas = new PlaceData[25];
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var slotData = context.Board.GetSlot(x, y);
                    if(slotData == null)
                    {
                        Debug.Log($"결과 데이터 변환 중 빈 빙고 슬롯 접근 X:{x},Y:{y}");
                    }
                    placeDatas[y * 5 + x] = new PlaceData(x,y,slotData.Info.Type);
                }
            }
            Places = placeDatas;
            //라인 결과
            ResultRecords = new LineResultRecord[context.LineResults.Count];
            foreach(var pair in context.LineResults)
            {
                var lineRef = pair.Key;
                var lineResult = pair.Value;
                int lineTypeIndex = lineRef.Type switch
                {
                    BingoLineType.Horizontal => 0,
                    BingoLineType.Vertical => 1,
                    BingoLineType.Diagonal => 2,
                    _ => throw new ArgumentOutOfRangeException()
                };
                ResultRecords[lineTypeIndex * 5 + lineRef.Index-1] = new LineResultRecord(lineResult, lineRef);
            }
            ChosenMissionID = context.MissionId;
            AchievedMission = context.AchieveMission;
        }

        public Dictionary<BingoLineRef, LineHandResult> GetLineResults()
        {
            Dictionary<BingoLineRef, LineHandResult> lineResults = new Dictionary<BingoLineRef, LineHandResult>();
            foreach (var record in ResultRecords)
            {
                BingoLineType lineType = record.LineTypeIndex switch
                {
                    0 => BingoLineType.Horizontal,
                    1 => BingoLineType.Vertical,
                    2 => BingoLineType.Diagonal,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var lineRef = record.GetLineInfo();
                var lineResult = record.GetHandResult();
                lineResults[lineRef] = lineResult;
            }
            return lineResults;
        }

        public void Log()
        {
            Debug.Log($"Player: {DisplayName} (ProfileId: {ProfileId}, Score: {TotalScoore}, Mission: {ChosenMissionID}, Achieved: {AchievedMission})");
        }

        public void LogToPlace()
        {
            foreach (var place in Places)
            {
                Debug.Log($"Place X:{place.X}, Y:{place.Y}, Type:{place.CardType}");
            }
        }
    }
    public abstract class BasePlayResultData
    {
        public PlayerInfoData UserInfo;
        public abstract GameMode Mode { get; }

        public BasePlayResultData(PlayerContext userContext)
        {
            UserInfo = new PlayerInfoData(userContext);
        }

        public abstract PlayerContext[] GetAllPlayers();
    }

    public class DefaultModeResultData : BasePlayResultData
    {
        public PlayerInfoData[] otherInfoDatas = new PlayerInfoData[3];
        public override GameMode Mode => GameMode.Default;

        public DefaultModeResultData(PlayerContext userContext, PlayerContext[] otherContexts) : base(userContext)
        {
            for (int i = 0; i < otherContexts.Length; i++)
            {
                otherInfoDatas[i] = new PlayerInfoData(otherContexts[i]);
            }
        }

        public override PlayerContext[] GetAllPlayers()
        {
            PlayerContext[] allPlayers = new PlayerContext[4];
            allPlayers[0] = new PlayerContext(UserInfo);
            for (int i = 0; i < otherInfoDatas.Length; i++)
            {
                var info = otherInfoDatas[i];
                allPlayers[i + 1] = new PlayerContext(info);
            }
            return allPlayers;
        }
    }

    public class ScoreModeResultData : BasePlayResultData
    {
        public override GameMode Mode => GameMode.Score;

        public ScoreModeResultData(PlayerContext userContext) : base(userContext)
        {
        }

        public override PlayerContext[] GetAllPlayers()
        {
            return new PlayerContext[] { new PlayerContext(UserInfo) };
        }
    }

    [Serializable]
    public struct Entry
    { 
        public string Nickname;
        public int Score;
        public Entry(string nickname, int score)
        {
            Nickname = nickname;
            Score = score;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Nickname);
    }

    [Serializable]
    public class LeaderBoard
    {
        public Entry[] TopEntries = new Entry[4];

        public bool CompareNewEntry(Entry newEntry)
        {
            //최저 보다 낮을 경우
            if(newEntry.Score <= TopEntries[TopEntries.Length - 1].Score)
                return false;

            //가장 윗 점수부터 비교
            for (int i = 0; i < TopEntries.Length; i++)
            {
                if (newEntry.Score > TopEntries[i].Score)
                {
                    // 새로운 엔트리가 현재 엔트리보다 높은 점수를 가지고 있다면, 순위를 업데이트
                    for (int j = TopEntries.Length - 1; j > i; j--)
                    {
                        TopEntries[j] = TopEntries[j - 1]; // 아래로 한 칸씩 이동
                    }
                    Debug.Log($"갱신! {i + 1}등 {newEntry.Score}");
                    TopEntries[i] = newEntry; // 새로운 엔트리를 현재 위치에 삽입
                    return true;
                }
            }
            return false;
        }
    }
}
