using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TB.Card;
using UnityEngine;

public class RevealCardUI : UIObjectBase
{
    private Queue<CardSO> Deck => GameManager.Instance.Deck;
    [SerializeField] private RevealCard[] _revealCards;

    [SerializeField] private RectTransform[] _spawnStartPoints;
    [SerializeField] private RectTransform[] _revealPoints;
    [SerializeField] private RectTransform[] _spawnEndPoints;
    public RevealCard SelectedCard { get; private set; }

    public event Action<bool> OnCompleteReveal;
    //속임수 효과에 사용될 필드
    private IRevealVisualResolver _revealVisualResolver;
    private PlayingCardSO[] _trickyCardInfos = new PlayingCardSO[2];

    private void Start()
    {
        foreach (var revealCard in _revealCards)
        {
            revealCard.OnClicked += HandleCardClicked;
        }
        //카드 사라질 떄 애니메이션 될 떄 해줌으로 일단 주석처리
        //GameManager.Instance.AddDurationPhase(TurnPhase.TurnEnd, InitRevealCards,-1);
        GameManager.Instance.OnReadySelf += FadeRevealCard;
        OnCompleteReveal += CompleteReveal;
        _revealVisualResolver = DefaultRevealVisualResolver.Resolver;
    }

    public void ApplyTricky(bool isTricky, PlayingCardSO[] trickyCardType = null)
    {
        _revealVisualResolver = isTricky ? TrickeryRevealVisualResolver.Resolver : DefaultRevealVisualResolver.Resolver;
        _trickyCardInfos = isTricky ?  trickyCardType : null;
    }

    private void InitRevealCards()
    {
        foreach (var revealCard in _revealCards)
        {
            revealCard.Init();
        }
    }

    public void FadeRevealCard()
    {
        GameManager.Instance.RegisterPhaseJob();
        int usingCardIndex = GameManager.Instance.ShouldRevealTwoCards ? 2 : 1;
        for (int i = 0; i < usingCardIndex; i++)
        {
            var seq = _revealCards[i].PlayDissolveInOut()
                .OnComplete(() =>
                {
                    Debug.Log($"카드 초기화 완료: {usingCardIndex}");
                    GameManager.Instance.CompletePhaseJob();
                    _revealCards[i].gameObject.SetActive(false);
                });
            seq.Play();
        }
        AudioManager.I.PlaySFX(SoundId.BurnCard);
    }

    [ContextMenu("RevealNextCards")]
    public void RevealNextCards(int totalCount = 1, int index = 0)
    {
        if (Deck.Count == 0)
        {
            Debug.Log("더 이상 카드가 없습니다");
            return;
        }
        InitRevealCards();
        ClearSelectedCard();
        //두 장일 때
        if (GameManager.Instance.ShouldRevealTwoCards)
        {
            RevealCardResult firstResult;
            RevealCardResult secondResult;
            var firstCard = Deck.Dequeue();
            var secondCard = Deck.Dequeue();
            if (GameManager.Instance.ShouldRevealFakeCards)
            {
                firstResult = _revealVisualResolver.Resolve(firstCard, _trickyCardInfos[0]);
                secondResult = _revealVisualResolver.Resolve(secondCard, _trickyCardInfos[1]);
            }
            else
            {
                firstResult = _revealVisualResolver.Resolve(firstCard);
                secondResult = _revealVisualResolver.Resolve(secondCard);
            }
            RevealDoubleCard(firstResult, secondResult);
        }
        //한 장일 때
        else
        {
            CardSO nextCard;
            if (totalCount == 1)
            {
                nextCard = Deck.Dequeue();
            }
            //터널이 발동된 상태일 때
            else
            {
                nextCard = Deck.TakeCardsWithRule(totalCount, index);
                Debug.Log($"<color=cyan> {totalCount}중 {index}, {nextCard.name}</color>");
            }
            var result = GameManager.Instance.ShouldRevealFakeCards ? _revealVisualResolver.Resolve(nextCard, _trickyCardInfos[index])
                                                                    : _revealVisualResolver.Resolve(nextCard);
            //속임수 턴에 이벤트 카드 적용 시 이벤트 카드 등장에는 일반 등장 사용
            if (GameManager.Instance.ShouldRevealFakeCards && nextCard is EventCardSO)
                result = DefaultRevealVisualResolver.Resolver.Resolve(nextCard);
            RevealSingleCard(result);
        }
    }

    private void RevealSingleCard(RevealCardResult result)
    {
        var revealCard = _revealCards[0];
        revealCard.Init(result.Real, result.Visual);
        var revealRectTrans = revealCard.transform as RectTransform;
        revealRectTrans.anchoredPosition = _revealPoints[0].anchoredPosition;
        revealRectTrans.rotation = _revealPoints[0].rotation;
        var startPos = _spawnStartPoints.GetRandom();
        Sequence seq = DOTween.Sequence()
            .Append(revealCard.PlayDrop(startPos.anchoredPosition))
            .AppendInterval(.15f)
            .Append(revealCard.PlayReveal())
            .AppendInterval(1f);
        if (revealCard.IsEventCard)
        {
        }
        else
        {
            //등장 카드일 경우 위치로 이동
            seq.Append(revealCard.PlayDoTransAnchored(_spawnEndPoints[1]));
            seq.OnComplete(() =>
            {
                var placeable = GameManager.Instance.MyBingoBoardRuntime.ExistPlaceableCell();
                OnCompleteReveal?.Invoke(placeable);
                SelectCard(revealCard);
            });
        }
        seq.Play();
    }
    private void RevealDoubleCard(RevealCardResult fistResult, RevealCardResult secondResult)
    {
        for (int i = 0; i <= 1; i++)
        {
            var revealCard = _revealCards[i];
            var revealRectTrans = revealCard.transform as RectTransform;
            revealRectTrans.anchoredPosition = _revealPoints[1 + i].anchoredPosition;
            revealRectTrans.rotation = _revealPoints[0].rotation;
            var startPos = _spawnStartPoints[1 - i];
            var result = i == 0 ? fistResult : secondResult;
            revealCard.Init(result.Real, result.Visual);
            Sequence seq = DOTween.Sequence()
                .AppendInterval(i * 0.25f)
                .Append(revealCard.PlayDrop(startPos.anchoredPosition))
            .AppendInterval(.15f)
            .Append(revealCard.PlayReveal())
            .AppendInterval(1f)
                .Append(revealCard.PlayDoTransAnchored(_spawnEndPoints[i]));
            //처음에 등장한 카드가 선택된 상태로 지정
            if (i == 0)
            {
                seq.OnComplete(() =>
                {
                    SelectCard(revealCard);
                });
            }
            else
            {
                seq.OnComplete(() =>
                {
                    //강제로 비선택 상태로 변경
                    revealCard.DeselectCard();
                    var placeable = GameManager.Instance.MyBingoBoardRuntime.ExistPlaceableCell();
                    OnCompleteReveal?.Invoke(placeable);
                });
            }
            seq.Play();
        }
    }

    public void SelectCard(RevealCard newRevealCard)
    {
        var prevCard = SelectedCard;
        prevCard?.SetSelect(false);
        SelectedCard = newRevealCard;
        newRevealCard.SetSelect(true);
    }

    //등장 카드 선택 시
    private void HandleCardClicked(RevealCard clickedCard)
    {
        if (clickedCard != SelectedCard)
        {
            SelectCard(clickedCard);
            SwapRevealCard(clickedCard);
        }
    }
    private void ClearSelectedCard()
    {
        SelectedCard = null;
    }

    //카드 공개에 대한 모든 행위(1턴)이 완료 시
    public void CompleteReveal(bool placeable)
    {
        //카드 공개 연출 종료
        Debug.Log($"<color=red>카드공개 종료</color>");
        GameManager.Instance.CompletePhaseJob();
        if (placeable)
        {
            //공개 카드 상호작용 잠금 해제
            _revealCards[0].LockClick(false, true);
            if (GameManager.Instance.ShouldRevealTwoCards)
                _revealCards[1].LockClick(false, true);
            //배치 가능 상태로 변경
            GameManager.Instance.CanPlace = true;
        }
        else
        {
            //배치할게 없다면 바로 준비 완료 
            GameManager.Instance.MyBingoBoardRuntime.Ready(true);
        }
    }

    [ContextMenu("Swap")]
    public void SwapRevealCard(RevealCard clickedCard)
    {
        for (int i = 0; i < _revealCards.Length; i++)
        {
            var revealCard = _revealCards[i];
            revealCard.LockClick();
            var targetRectTransfrom = _revealCards[1 - i].transform as RectTransform;
            var seq = revealCard.PlayDoTransAnchored(targetRectTransfrom, .6f).SetEase(Ease.OutSine);
            seq.OnComplete(() =>
            {
                revealCard.LockClick(false);
                if (revealCard != clickedCard)
                    revealCard.DeselectCard();
            });
            seq.Play();
        }
        //선택된 카드 위치 바꾸기]
        clickedCard.transform.SetSiblingIndex(1);
    }
}
