using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UI_AskingCard : UI_BoardItem
{
    enum TMP_Texts
    {
        TitleText,
        RequiredCountText,
    }
    //질문지 데이터 식별 인덱스
    public int TemplatedId { get; private set; }
    //할당 받지 않은 질문지는 없어 프토타입 단계에서 바로 참조
    public Data.AskingCardData Data => Managers.Data.AskingCardDic[TemplatedId];
    public EArrangementType Type => Data.Type;
    //프로퍼티 힙 할당 오염 대비 1회성 캐싱
    private Data.ClueData[] _requiredClues;

    public Data.ClueData[] RequiredClues
    {
        get
        {
            if (_requiredClues == null)
            {
                _requiredClues = new Data.ClueData[Data.RequiredClueIds.Length];
                for (int i = 0; i < Data.RequiredClueIds.Length; i++)
                {
                    _requiredClues[i] = Managers.Data.ClueDic[Data.RequiredClueIds[i]];
                }
            }
            return _requiredClues;
        }
    }
    public UI_ClueCard[] CollectedClueCards { get; private set; }
    public int CollectedCount { get; private set; }

    public UI_SolutionCard SolutionCard { get; set; }
    public bool[] CompleteExtendeds { get; private set; }
    public int Height => Type == EArrangementType.Extended ? RequiredClues.Length : 1;

    public override void Spawn(int templatedId, Vector2Int point, Action onComplete)
    {
        TemplatedId = templatedId;
        CollectedClueCards = new UI_ClueCard[RequiredClues.Length];
        CompleteExtendeds = new bool[RequiredClues.Length];
        CollectedCount = 0;
        Get<TMP_Text>((int)TMP_Texts.TitleText).text = Data.Title;
        Get<TMP_Text>((int)TMP_Texts.RequiredCountText).text = RequiredClues.Length.ToString();
        base.Spawn(templatedId, point, onComplete);
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        BindTexts(typeof(TMP_Texts));
        return true;
    }

    public bool TryAddClue(UI_ClueCard clue, out Vector2Int point)
    {
        point = Vector2Int.zero;
        for (int i = 0; i < CollectedClueCards.Length; i++)
        {
            if (CollectedClueCards[i] == null && CompleteExtendeds[i] == false)
            {
                CollectedClueCards[i] = clue;
                point = RightPoint + (Type == EArrangementType.Extended ? Vector2Int.up : Vector2Int.right) * i;
                CollectedCount++;
                return true;
            }
        }

        return false;
    }

    //지정한 증거에 대해 참 거짓 구분
    [ContextMenu("비교")]
    private bool CompareClue(int slotIdx, out int dataIdx)
    {
        dataIdx = -1;
        var placedCard = CollectedClueCards[slotIdx];

        for (int j = 0; j < Data.RequiredClueIds.Length; j++)
        {
            if (placedCard.TemplateId == Data.RequiredClueIds[j])
            {
                dataIdx = j; // 데이터상의 위치를 찾음
                return true;
            }
        }
        return false;
    }

    public void ClearCollected(UI_ClueCard[] clue)
    {
        for (int i = 0; i < CollectedClueCards.Length; i++)
        {
            if (clue.Contains(CollectedClueCards[i]) == true)
            {
                CollectedClueCards[i] = null;
                CollectedCount--;
            }
        }
    }
    public UI_BoardItem GetEndItem(int treeIndex = -1)
    {
        //아무런 아이템이 없다면 본인 제공
        if (CollectedClueCards[0] == null)
            return this;
        //해설지에 대한 제공 추가 필요
        if (Type == EArrangementType.Extended)
        {
            //treeIndex가 -1이면 자신 제공
            if (treeIndex == -1 || treeIndex >= CollectedCount)
                return this;
            return CollectedClueCards[treeIndex];
        }
        else if (Type == EArrangementType.Linked)
        {
            if(SolutionCard != null)
                return SolutionCard;
            return CollectedClueCards[CollectedCount - 1];
        }
        throw new Exception("Invalid Arrangement Type");
    }
    public CheckResult ValidateClues()
    {
        CheckResult result = new CheckResult
        {
            CorrectInfos = new List<CorrectInfo>(),
            WrongIndexes = new List<int>()
        };

        // 1. 공통 로직: 모든 슬롯을 순회하며 정답 여부 판별
        for (int i = 0; i < CollectedClueCards.Length; i++)
        {
            if (CollectedClueCards[i] == null) continue;

            if (CompareClue(i, out int dataIdx))
            {
                // [핵심] 배치된 위치(i)와 데이터 순서(dataIdx)를 함께 저장
                result.CorrectInfos.Add(new CorrectInfo { SlotIdx = i, DataIdx = dataIdx });
            }
            else
            {
                // 오답은 해당 슬롯 위치만 기록
                result.WrongIndexes.Add(i);
            }
        }

        // 2. 타입별 최종 결과 처리
        if (Type == EArrangementType.Extended)
        {
            // 확장형: 모든 정답 슬롯이 채워졌는지 체크
            result.IsComplete = result.CorrectInfos.Count == RequiredClues.Length;
        }
        else if (Type == EArrangementType.Linked)
        {
            // 링크형: All or Nothing 로직
            bool hasError = result.WrongIndexes.Count > 0;
            bool countMismatch = (CollectedCount != Data.RequiredClueIds.Length);

            if (hasError || countMismatch)
            {
                // 하나라도 틀리면 맞았던 정보들도 전부 Wrong 슬롯 인덱스로 통합
                foreach (var info in result.CorrectInfos)
                {
                    result.WrongIndexes.Add(info.SlotIdx);
                }
                result.CorrectInfos.Clear();
                result.WrongIndexes.Sort();
                result.IsComplete = false;
            }
            else
            {
                result.IsComplete = true;
            }
        }

        return result;
    }
}

