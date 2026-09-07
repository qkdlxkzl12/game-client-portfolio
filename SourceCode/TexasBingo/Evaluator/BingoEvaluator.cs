using System;
using System.Collections.Generic;
using System.Linq;
using TB.Bingo;
using TB.PokerHand;
using TB.Result;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.Rigidbody2D;

public static class BingoEvaluator
{
    public static void EvalutorScoreMode(PlayerContext context)
    {
        int score = 0;
        foreach (var lineRef in BingoLineRegistry.GetAllLineRefs())
        {
            var rank = context.Board.GetLineResult(lineRef);
            score += GetScore(rank.Rank);
        }
        var info = ProfileDataManager.Instance.PlayerInfo;
        //최고기록 갱신 여부 판별
        info.UpdateHighscore(score);
        context.Score = score;
    }

    public static void EvalutorDefaultMode(PlayerContext context)
    {
        int score = 0;
        //빙고 달성 점수 계산
        foreach (var lineRef in BingoLineRegistry.GetAllLineRefs())
        {
            if (context.LineResults[lineRef].IsWin)
            {
                score ++;
                Debug.Log($"<color=cyan>{lineRef.Type}-{lineRef.Index}에서 {context.Nickname}가 1점 득점!</color>");
            }
        }
        //미션 달성 여부 판별
        var missionCard = CardManager.Instance.GetMissionCard(context.MissionId);
        bool success = MissionEvaluator.Evaluate(context.LineResults, missionCard.MissionConditionData);
        if (success)
        {
            score += missionCard.Point;
        }
        context.AchieveMission = success;
        context.Score = score;
    }

    public static int GetScore(HandRank rank)
    {
        return rank switch
        {
            HandRank.RoyalFlush => 15,
            HandRank.StraightFlush => 12,
            HandRank.FourOfAKind => 10,
            HandRank.FullHouse => 8,
            HandRank.Flush => 6,
            HandRank.Straight => 5,
            HandRank.Triple => 3,
            HandRank.TwoPair => 2,
            HandRank.OnePair => 1,
            HandRank.HighCard => 0,
            _ => 0,
        };
    }

    public static void CheckBingo(IEnumerable<PlayerContext> playerContexts)
    {
        //사용 변수 초기화
        var ctxs = playerContexts.ToArray();
        var allLineRefs = BingoLineRegistry.GetAllLineRefs().ToArray();

        Dictionary<PlayerContext, Dictionary<BingoLineRef, HandResult>> bingoResultTable = new();
        foreach (var ctx in ctxs)
        {
            bingoResultTable[ctx] = new Dictionary<BingoLineRef, HandResult>();
        }
        //context에서 사용하는 값 추출 및 저장
        foreach (var lineRef in allLineRefs)
        {
            foreach (var ctx in ctxs)
            {
                var result = ctx.Board.GetLineResult(lineRef);
                bingoResultTable[ctx][lineRef] = result;
            }
        }

        HandRank bestRank = HandRank.None;
        Dictionary<PlayerContext,Dictionary < BingoLineRef, LineHandResult>> playerLineResults = new();
        foreach (var ctx in ctxs)
            playerLineResults[ctx] = new();
        // 각 라인별 최고 랭크 보드 찾기 및 점수 부여
        foreach (var lineRef in allLineRefs)
        {
            HandRank highestRank = HandRank.None;
            List<PlayerContext> bestRankPlayers = new();

            // 각 보드의 해당 라인 랭크 비교
            foreach (var kvp in bingoResultTable)
            {
                var player = kvp.Key;
                var resultList = kvp.Value;

                var currentRank = resultList[lineRef].Rank;


                // 더 높은 랭크 발견 시 갱신
                if (currentRank > highestRank)
                {
                    highestRank = currentRank;
                    bestRankPlayers.Clear();
                    bestRankPlayers.Add(player);
                }
                // 동일 랭크라면 공동 1위로 추가
                else if (currentRank == highestRank)
                {
                    bestRankPlayers.Add(player);
                }
            }

            //최강 족보 갱신
            if(bestRank < highestRank)
                bestRank = highestRank;

            //동일한 랭크일 때 키커 비교
            var bestPlayer = EvaluateBest(bestRankPlayers, bingoResultTable, lineRef);
            var rankCountsByLine = bingoResultTable
                .Select(t => t.Value[lineRef].Rank)
                .GroupBy(r => r).
                ToDictionary(g => g.Key, g => g.Count());

            //각 줄에 대한 판별 결과 할당
            foreach (var player in bingoResultTable.Keys)
            {
                var result = bingoResultTable[player][lineRef];
                bool isWin = bestPlayer.Contains(player);
                bool hasSameRank = rankCountsByLine.TryGetValue(result.Rank,out int c) ? c > 1 : false;

                playerLineResults[player][lineRef] = new LineHandResult(result, isWin, hasSameRank);
            }
        }
        UnityEngine.Debug.Log($"최강 족보: {bestRank}");
        //최강 점수에 대해 전달
        foreach (var player in playerLineResults.Keys)
        {
            var dict = playerLineResults[player];
            foreach (var lineRef in dict.Keys.ToArray())
            {
                var lr = dict[lineRef];
                lr.CheckStrongest(bestRank);
                //구조체 값 할당
                dict[lineRef] = lr;
            }
        }

        //값 할당
        foreach (var player in playerLineResults.Keys)
        {
            player.SetLineResults(playerLineResults[player]);
        }
    }

    //동일한 족보일 때 키커 비교 후 최종 우승자 반환
    private static List<PlayerContext> EvaluateBest(List<PlayerContext> contexts, Dictionary<PlayerContext, Dictionary<BingoLineRef, HandResult>> bingoResults, BingoLineRef lineRef)
    {
        List<PlayerContext> bests = new List<PlayerContext>();
        foreach (var context in contexts)
        {
            if (bests.Count == 0)
            {
                bests.Add(context);
                continue;
            }
            //1:비교값이 더 큼, 0:키커 일치, -1:비교값이 더 작음
            int cmp = CompareKickers(
                bingoResults[context][lineRef].Kickers,
                bingoResults[bests.First()][lineRef].Kickers
            );

            if (cmp > 0)
            {
                bests.Clear();
                bests.Add(context);
            }
            else if (cmp == 0)
            {
                bests.Add(context);
            }
        }
        return bests;
    }

    //두 큐의 키커를 비교 값 반환 -1, 0, 1
    private static int CompareKickers(int[] a, int[] b)
    {
        if (a == null || b == null)
            throw new InvalidOperationException("Kickers array is null.");

        if (a.Length != b.Length)
            throw new InvalidOperationException($"Kickers length mismatch: {a.Length} vs {b.Length}");

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] > b[i]) return 1;
            if (a[i] < b[i]) return -1;
        }
        return 0;
    }
}
