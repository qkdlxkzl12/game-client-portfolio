using TB.Card;
using UnityEngine;

public enum CellTokenState
{
    Empty,
    Preview,    // 1단계
    Ready,      // 2단계
    Committed,  // 3단계
    Done,       // 최종
}
public class BingoCell
{
    public PlayingCardType TokenType { get; private set; }

    public CellTokenState State { get; private set; }

    public bool HasToken => TokenType != PlayingCardType.None;
    public bool IsSandstorm { get; private set; }
    public bool IsPetrified { get; private set; }
    public bool IsTricky { get; private set; }

    public bool CanPlace => (State == CellTokenState.Empty && !(IsSandstorm || IsPetrified));

    private int _x, _y;
    public int X => _x;
    public int Y => _y;

    public BingoCell()
    {
        TokenType = PlayingCardType.None;
        State = CellTokenState.Empty;
        IsSandstorm = false;
        IsPetrified = false;
    }

    public BingoCell(int x, int y)
    {
        _x = x;
        _y = y;
        TokenType = PlayingCardType.None;
        State = CellTokenState.Empty;
        IsSandstorm = false;
        IsPetrified = false;
    }

    //셀이 선택되어 예시 이미지가 제공된 상태 데이터 설정
    public void SetPreview(PlayingCardSO card, bool visible)
    {
        State = visible ? CellTokenState.Preview : CellTokenState.Empty;
        TokenType = visible ? card.Type : PlayingCardType.None;
    }

    //셀에 카드가 배치된 상태
    public void PlaceToken(PlayingCardSO newCard = null)
    {
        //새 토큰에 대해 배치한다면
        if (newCard != null)
        {
            TokenType = newCard.Type;
        }
        State = CellTokenState.Ready;
    }
    public void PlaceToken(PlayingCardType newType)
    {
        if (newType != PlayingCardType.None)
        {
            TokenType = newType;
            State = CellTokenState.Done;
            return;
        }
        State = CellTokenState.Ready;
    }

    public void Committed()
    {
        if (TokenType == PlayingCardType.None)
            throw new System.Exception($"토큰이 할당되지 않은 곳에 확정을 시도했습니다. 셀정보: {X}, {Y}");
        State = CellTokenState.Committed;
    }

    public void Clear()
    {
        TokenType = PlayingCardType.None;
        State = CellTokenState.Empty;
        IsSandstorm = false;
        IsPetrified = false;
    }

    //만일 모래바람 외 조건이 추가된다면 flag로 관리
    public void SetSandStorm(bool actvie)
    {
        IsSandstorm = actvie;
    }

    public void SetPetrified(bool actvie)
    {
        IsPetrified = actvie;
    }

    public void ClearSandStorm()
    {
        IsSandstorm = false;
    }

    public void EffectEx()
    {
        //효과 부여 시 해당하는 값 적용 및 셀 비주얼 변경을 위한 호출
    }
}
