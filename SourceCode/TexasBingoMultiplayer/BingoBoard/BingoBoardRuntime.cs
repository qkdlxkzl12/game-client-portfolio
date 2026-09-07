using System;
using TB.Bingo;
using TB.Card;
using TB.Direction;
using TB.Extention.GM;
using UnityEngine;
using UnityEngine.Playables;
public enum CellChangeReason
{
    Preview,
    Ready,
    Commit,
    Effect,
    SelectionChanged,
    SelectionCleared,
}

public class BingoBoardRuntime
{
    //public event Action<BingoCell> OnCellChanged;
    public event Action<BingoCell, CellChangeReason> OnCellChanged;
    public event Action OnPreviewCell;
    private BingoCell _selectedCell;


    private readonly BingoBoard _board;

    public BingoBoardRuntime(BingoBoard board)
    {
        _board = board;
    }

    //카드 배치 시 호출되는 메서드
    public void TryPlaceCard(BingoCell cell, PlayingCardSO card)
    {
        if (card == null)
        {
            Debug.LogWarning("선택된 카드가 없습니다");
            return;
        }
        //빙고 판별 시 호출 되야함
        //_board.PlaceCardDataOnly(cell);
        //준비 완료 시 호출 되야함
        //OnCellChanged?.Invoke(cell);
        //GameManager.Instance.IsPlacementPhase = false;
        //테스트 코드 - 준비 완료 시 호출
        //OnCardPlaced?.Invoke(cell);
    }

    #region New
    //셀에 반투명 이미지 제공
    public void PreviewCell(BingoCell newCell, PlayingCardSO card)
    {
        //이전 브리뷰 설정 종료
        var prevCell = _selectedCell;
        if (prevCell != null && prevCell != newCell)
        {
            prevCell.SetPreview(null, false);
            Debug.Log($"{prevCell.X},{prevCell.Y} 선택 취소");
            Debug.Log($"테스트{card.Type}");
            OnCellChanged?.Invoke(prevCell, CellChangeReason.Preview);
        }

        _selectedCell = newCell;

        //새 브리뷰 설정
        _selectedCell.SetPreview(card, true);
        Debug.Log($"{_selectedCell.X},{_selectedCell.Y} 선택 완료");
        OnCellChanged?.Invoke(_selectedCell, CellChangeReason.Preview);
        OnPreviewCell?.Invoke();
    }
    public void Ready(bool force = false)
    {
        if(!force)
        {
            //현재 위치에 토큰 배치
            SetSelectedCell();
            //서버에게 레디 상태 알리기
            OnCellChanged?.Invoke(_selectedCell, CellChangeReason.Ready);
        }
        //누군지 보내거나 은닉 필요
        GameManager.Instance.PlayerPlaceDone();
    }

    //레디 시 호출되는 메서드
    //현재 선택한 곳에 배치를 완료함
    public void SetSelectedCell(BingoCell newCell = null, PlayingCardSO card = null)
    {
        //새 위치 지정 시 이전 위치 초기화
        if (newCell != null)
        {
            var prev = _selectedCell;
            if (prev != null && prev != newCell)
            {
                prev.SetPreview(null, false); // or ClearPreview()
                OnCellChanged?.Invoke(prev, CellChangeReason.Preview);
            }
            _selectedCell = newCell;
        }
        //이미지 제공
        _selectedCell.PlaceToken(); // 토큰 이미지만 배치
    }
    public void CheatOff()
    {
        var select = GameManager.Instance.SelectedCard.Info as PlayingCardSO;
        //만약 속임수 셀이라면
        Debug.Log($"<color=red>테스트{GameManager.Instance.ShouldRevealFakeCards}, {_selectedCell.TokenType}, {select?.Type}</color>");
        if (GameManager.Instance.ShouldRevealFakeCards && _selectedCell.TokenType != select?.Type)
        {
            Debug.Log($"<color=red>속임수 해제 애님 재생 {select.Type}</color>");
            GameManager.Instance.RegisterPhaseJob();
            _selectedCell.SetPreview(select, true);
            OnCellChanged?.Invoke(_selectedCell, CellChangeReason.Preview);
            ParticleManager.Instance.TrickyEffect(_selectedCell.X, _selectedCell.Y,
                () =>
                {
                    GameManager.Instance.CompletePhaseJob();
                    GameNetworkController.Instance.RequestPlaceToken(_selectedCell.X, _selectedCell.Y, _selectedCell.TokenType);
                });
        }
    }
    //흐름에 의해 호출됨
    public void CommitCell()
    {
        if (ExistPlaceableCell())
            GameNetworkController.Instance.RequestPlaceToken(_selectedCell.X, _selectedCell.Y, _selectedCell.TokenType);
    }
    //RPC를 통해 최종적으로 데이터 삽입되는 메서드
    public void CommitPlacement(int x, int y, PlayingCardType type)
    {
        if (_selectedCell.X != x || _selectedCell.Y != y)
        {
            Debug.LogWarning("[Error] 커밋된 셀과 선택된 셀이 다릅니다");
        }
        // 2. 데이터 변경 (예전 Commit/PlaceCardDataOnly 안에 있던 로직)
        _selectedCell.Committed();
        // 3. OnChangedCell 트리거
        OnCellChanged?.Invoke(_selectedCell, CellChangeReason.Commit);
    }

    //셀에 특수 효과 적용
    public void ApplyEffectToCell(BingoCell cell, SlotState effect)
    {
        switch (effect)
        {
            case SlotState.Petrified:
                //cell.SetPetrified();
                break;
            case SlotState.SandStorm:
                //cell.SetSandStorm();
                break;
                // 기타 효과들...
        }

        OnCellChanged?.Invoke(cell, CellChangeReason.Effect);
    }
    #endregion


    //외부 효과(모래폭풍, 석화 등) 발생 시 호출되는 메서드
    public void AddSandStorm(Direction direction)
    {
        ApplyEffect(SlotState.SandStorm, direction);
    }
    public void AddPetrification(BingoLineRef lineRef)
    {
        ApplyEffect(SlotState.Petrified, lineRef);
    }

    //모래바람 전용
    private void ApplyEffect(SlotState effect, Direction direction)
    {
        var positions = direction.GetAffectedPositions();
        foreach (var pos in positions)
        {
            var cell = _board.GetCell(pos.x, pos.y);
            cell.SetSandStorm(true);
            OnCellChanged?.Invoke(cell, CellChangeReason.Effect);
            var capturedCell = cell;
            GameManager.Instance.AddDelayedPhase(TurnPhase.TurnEnd, () =>
            {
                capturedCell.SetSandStorm(false);
                OnCellChanged?.Invoke(capturedCell, CellChangeReason.Effect);
            });
        }
    }
    //석화 전용
    private void ApplyEffect(SlotState effect, BingoLineRef lineRef)
    {
        var positions = lineRef.GetAffectedPositions(); // IEnumerable<(int x, int y)> 라고 가정
        foreach (var pos in positions)
        {
            var cell = _board.GetCell(pos.x, pos.y);
            cell.SetPetrified(true);
            OnCellChanged?.Invoke(cell, CellChangeReason.Effect);
            var capturedCell = cell;
            GameManager.Instance.AddDelayedPhase(TurnPhase.TurnEnd, () =>
            {
                capturedCell.SetPetrified(false);
                OnCellChanged?.Invoke(capturedCell, CellChangeReason.Effect);
            });
        }
        //ApplyEffect(effect, positions);
    }

    public void InitSelectCell()
    {
        _selectedCell = null;
    }

    public void ForcePlace()
    {
        GameManager.Instance.CanPlace = false;
        if (_selectedCell == null)
        {
            _selectedCell = _board.GetEmpthyCell();
        }
        var card = GameManager.Instance.SelectedCard.VisualInfo as PlayingCardSO;
        _selectedCell.SetPreview(card, true);
        Ready();
    }

    public bool ExistPlaceableCell()
    {
        Debug.Log($"{_board.PlaceableCellCount}");
        return _board.PlaceableCellCount != 0;
    }

    public bool BoardResolved() => _board.IsFull;
}
