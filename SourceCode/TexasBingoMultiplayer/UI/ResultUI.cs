using DG.Tweening;
using NUnit.Framework;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TB.Card;
using TB.PokerHand;
using TB.Result;
using UnityEngine;
using UnityEngine.UIElements;
public class ResultUI : UIObjectBase
{
    [SerializeField] private PlayerResultInfoUI[] _playerResultInfoUIs;
    [SerializeField] protected Sprite[] _characterLabelSprites;
    private int _currentLineIndex;
    private PlayerContext[] _players;

    protected override void Awake()
    {
        UseBlackPanel = true;
        BPFadeDuration = 0.5f;
        base.Awake();
        foreach (var item in _playerResultInfoUIs)
        {
            item.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void Aprear()
    {

    }
    public void OnEnable()
    {
        _rectTransform.anchoredPosition = new Vector2(0, 1200);
        Debug.Log("활성화");
        //테스트용
        //Test();
        var seq = DOTween.Sequence()
            .Append(_rectTransform.DOAnchorPosY(0, .5f).SetEase(Ease.OutQuad))
            .OnComplete(() => StartCoroutine(CompareLine()));

    }
    [ContextMenu("테스트")]
    void Test()
    {
        List<PlayerContext> list = new();
        int cardIndex = 1;
        for (int i = 0; i < 3; i++)
        {
            var c = new PlayerContext(i, "테스트" + i, i+1);
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    BingoCell cell = new BingoCell(x, y);
                    var cardType = cardIndex++.ToPlayingCardType();
                    cell.PlaceToken(CardManager.Instance.GetPlayingCard(cardType));
                    c.Board.PlaceCardDataOnly(cell);
                }
            }
            Debug.Log($"{c.Board.GetLineResult(1).ToText()}");
            list.Add(c);
            cardIndex -= 20;
        }
        BingoEvaluator.CheckBingo(list);
        _players = list.ToArray();
        Open(list.ToArray());
    }

    IEnumerator CompareLine()
    {
        yield return new WaitForSeconds(.5f);
        for (int i = 1;i <= 12;i++)
        {
            _currentLineIndex = i;
            ShowResult(_currentLineIndex);
            yield return new WaitForSeconds(5f);
        }
    }

    public void Open(PlayerContext[] contexts)
    {
        Debug.Log("오픈");
        for (int i = 0; i < contexts.Length; i++)
        {
            var context = contexts[i];
            var labelSprite = _characterLabelSprites[(int)context.SelectCharacter.Type - 1];
            _playerResultInfoUIs[i].Init(context.Board, labelSprite);
            _playerResultInfoUIs[i].gameObject.SetActive(true);
        }
        _players = contexts;
        gameObject.SetActive(true);
    }
    
    private void ShowResult(int lineIndex)
    {
        var lineRef = BingoLineRegistry.GetLineRef(lineIndex);
        for (int i = 0; i < _playerResultInfoUIs.Length; i++)
        {
            var infoUI = _playerResultInfoUIs[i];
            if (infoUI.gameObject.activeSelf == false)
                continue;
            //이전 라인 초기화
            if (_currentLineIndex != 1)
            {
                var prevLineRef = BingoLineRegistry.GetLineRef(_currentLineIndex-1);
                infoUI.HighlightLine(prevLineRef, false);
                infoUI.InitResult();
            }
            infoUI.HighlightLine(lineRef);
            var result = _players[i].Board.GetLineResult(lineIndex);
            bool isWinner = _players[i].WinResult.ContainsKey(lineRef);
            infoUI.ShowHandResult(result, isWinner);
        }
    }
}
