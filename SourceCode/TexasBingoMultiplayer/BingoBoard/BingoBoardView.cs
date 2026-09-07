using DG.Tweening;
using System;
using TB.Card;
using TB.Characters.Ability;
using TB.Extention.GM;
using UnityEngine;

public class BingoBoardView : MonoBehaviour
{
    [SerializeField] private BingoCellView[] _cellViewsFlat;
    private BingoCellView[,] _cellViews;
    private BingoBoard _board;
    public BingoBoardRuntime Runtime { get; private set; }

    //스킬 사용을 위한 임시 사용
    private PlayerContext _playerContext;

    public void Connect(PlayerContext context)
    {
        _board = context.Board;
        _playerContext = context;
        Runtime = new BingoBoardRuntime(_board);
        Runtime.OnCellChanged += HandleCellChanged;
        BindCells();
        GameManager.Instance.AddDurationPhase(TurnPhase.TurnStart, (ctx) => Runtime.InitSelectCell(), -1);
        GameManager.Instance.AddDurationPhase(TurnPhase.TurnEnd, (ctx) => Runtime.CommitCell(), -1);
    }

    //바인딩을 Board가 아닌 다른 곳에서 해줘야함 - Ex. PlayerContext
    private void BindCharacterAbility(TB.Characters.Character character)
    {
        Debug.Log($"<color=blue>{character.Type}</color>");
        //빙고 시 관련 능력 바인드
        if (character is IBingoLineAbility)
        {
            switch (character.AbilityTrigger)
            {
                //뱀
                case AbilityTriggerType.OnBingoLineCompleted:
                    _board.AddOnBingoAchievedListener(lineRef => character.OnBingoLineCompleted(_playerContext, lineRef));
                    break;
                default:
                    throw new Exception($"[Error]-잘못된 구조의 능력이 바인드를 요청합니다. {character.Type}, {character.AbilityTrigger}");
            }
        }
        //무조건(패시브) 관련 능력 바인드
        if (character is INoTargetAbility)
        {
            switch (character.AbilityTrigger)
            {
                case AbilityTriggerType.OnCardRevealed:
                    //독수리
                    if (character is IAbilityInstaller installer)
                    {
                        GameManager.Instance.AddDelayedPhase(TurnPhase.RevealCards, _ => installer.Install(_playerContext));
                    }
                    GameManager.Instance.Dealer.OnCompleteReveal += _ => character.OnTrigger(_playerContext);
                    break;
                default:
                    throw new Exception($"[Error]-잘못된 구조의 능력이 바인드를 요청합니다. {character.Type}, {character.AbilityTrigger}");
            }
        }
        var defaultUI = UIManager.Instance.GetUI<DefaultUI>();
        if (character is IUseActiveAbility ability)
        {
            switch (character.AbilityTrigger)
            {
                case AbilityTriggerType.OnUseActive:
                    //여우
                    Debug.Log("여우 능력 적용");
                    GameNetworkController.Instance.OnAllPlayerPhaseReady += phase =>
                    {
                        if (phase != TurnPhase.Placement)
                            return;
                        if (!ability.UseAbility)
                            return;
                        Debug.Log("<color=red>능력 사용</color>");
                        defaultUI.SetSkillButtonInteractable(false);
                        character.OnTrigger(_playerContext);
                        GameManager.Instance.AddDelayedPhase(TurnPhase.TurnEnd, _ => ability.UseAbility = false);
                    };
                    break;
                default:
                    throw new Exception($"[Error]-잘못된 구조의 능력이 바인드를 요청합니다. {character.Type}, {character.AbilityTrigger}");
            }
        }
        Runtime.OnPreviewCell += () =>
        {
            defaultUI.SetReadyButtonInteractable(true);
        };
    }
    private void BindCells()
    {
        int n = 5;
        _cellViews = new BingoCellView[n, n];

        for (int i = 0; i < _cellViewsFlat.Length; i++)
        {
            int x = i % n;
            int y = i / n;
            //보드를 통해 셀 연결
            var cell = _board.GetCell(x, y);
            var view = _cellViewsFlat[i];

            view.Bind(cell, this);
            view.OnClicked += HandleCellClicked;
            _cellViews[x, y] = view;
        }
    }

    //셀 뷰 클릭 시 호출
    private void HandleCellClicked(BingoCell cell)
    {
        var card = GameManager.Instance.SelectedCard.VisualInfo as PlayingCardSO;
        if (card == null)
        {
            Debug.LogWarning("선택된 카드가 없습니다");
            return;
        }
        Debug.Log($"X:{cell.X}, Y:{cell.Y} 클릭");
        //셀을 선택했을 때 상황에 따라 호출하는 메서드를 변경해야함
        //혹은 셀의 클릭이 아닌 별도의 방법으로 트리거 발동

        //셀 클릭 시 선택된 셀 갱신 및 업데이트
        //현재는 클릭=변경. 조건 필요
        Runtime.PreviewCell(cell, card);
    }

    private void HandleCellChanged(BingoCell cell, CellChangeReason reason)
    {
        //Debug.Log($"{cell.X},{cell.Y}에서 {reason}에 의한 변화");
        //실질적인 값 변동은 Runtime에서 
        var view = GetCellView(cell);
        view.InitAllEffect();

        // 비주얼은 이펙트만 처리하는게 아니라면 처리
        if (reason != CellChangeReason.Effect)
            view.UpdateTokenVisual(cell);

        Sequence seq;
        // 이유에 따라 추가 연출이 필요하면 여기서 분기
        switch (reason)
        {
            case CellChangeReason.Preview:
                seq = view.PlayPreviewAnimation();
                break;
            case CellChangeReason.Ready:
                GameManager.Instance.RegisterPhaseJob();
                seq = view.PlayReadyAnimation()
                    .AppendInterval(0.5f)
                    .OnComplete(GameManager.Instance.CompletePhaseJob);
                break;
            case CellChangeReason.Commit:
                GameManager.Instance.RegisterPhaseJob();
                seq = view.PlayCommitAnimation()
                    .OnComplete(() =>
                    {
                        //실제 데이터 기입 - RPC로 변경 필요
                        _board.PlaceCardDataOnly(cell);
                        GameManager.Instance.CompletePhaseJob();
                    });
                break;
            case CellChangeReason.Effect:
                //어떤 이펙트인지
                view.UpdateEffectVisual(cell);
                break;
        }
    }

    public void Ready()
    {
        Runtime.Ready();
    }

    //카드 위치 스왑 시 사용될 예정
    public void RequestPlaceCard(BingoCell cell)
    {
        //스왑 애니메이션은 별도의 토큰을 이용해 재생
        var card = GameManager.Instance.SelectedCard.VisualInfo as PlayingCardSO;
        Runtime.TryPlaceCard(cell, card);
    }

    public void Test_Add(BingoCell cell, PlayingCardSO card)
    {
        cell?.PlaceToken(card);
        cell?.Committed();
    }
    #region Getter
    private BingoCell GetCell(int x, int y)
    {
        if (x < 0 || x >= BingoBoard.LineLength || y < 0 || y >= BingoBoard.LineLength)
            return null;
        return _board.GetCell(x, y);
    }
    public BingoCellView GetCellView(BingoCell cell)
    {
        return _cellViews[cell.X, cell.Y];
    }

    public BingoCellView GetCellView(int x, int y)
    {
        return _cellViews[x, y];
    }
    #endregion

    public int lineIndex;
    [ContextMenu("석화")]
    public void Test()
    {
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(lineIndex));
    }

    [ContextMenu("봉쇄")]
    public void Test2()
    {
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(1));
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(2));
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(3));
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(4));
        Runtime.AddPetrification(BingoLineRegistry.GetLineRef(5));
    }


    [ContextMenu("랜덤 배치")]
    public void TEST() => Runtime.ForcePlace();

    [ContextMenu("확인")]
    public void TEST2() => Debug.Log($"배치 가능 칸 수 : {_board.PlaceableCellCount}");

}
