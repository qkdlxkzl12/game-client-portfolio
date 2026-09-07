using System;
using UnityEngine;
using TB.PokerHand;
using TB.Bingo;
using System.Collections.Generic;
using System.Linq;

public class BingoBoard
{
    public static int LineLength => BingoLineRegistry.LineLength;

    private BingoCell[,] _cells = new BingoCell[LineLength, LineLength];

    private BingoLine[] _horizontalLines = new BingoLine[LineLength];
    private BingoLine[] _verticalLines = new BingoLine[LineLength];
    private BingoLine[] _diagonalLines = new BingoLine[2];

    public int PlaceableCellCount => _cells.Cast<BingoCell>().Count(c => c.CanPlace || c.State == CellTokenState.Ready);
    public bool IsFull {  get; private set; }

    public BingoBoard()
    {
        int lineIndex = 1;
        for (int i = 0; i < LineLength; i++)
        {
            _horizontalLines[i] = new BingoLine(lineIndex);
            lineIndex++;
        }
        for (int i = 0; i < LineLength; i++)
        {
            _verticalLines[i] = new BingoLine(lineIndex);
            lineIndex++;
        }
        for (int i = 0; i < _diagonalLines.Length; i++)
        {
            _diagonalLines[i] = new BingoLine(lineIndex);
            lineIndex++;
        }
        for (int x = 0; x < LineLength; x++)
        {
            for (int y = 0; y < LineLength; y++)
            {
                _cells[x, y] = new BingoCell(x, y);
            }
        }
    }

    //빙고 시 호출될 리스너 일괄 추가 메서드   
    public void AddOnBingoAchievedListener(Action<BingoLineRef> listener)
    {
        foreach (var line in _horizontalLines)
            line.OnBingoAchieved += listener;
        foreach (var line in _verticalLines)
            line.OnBingoAchieved += listener;
        foreach (var line in _diagonalLines)
            line.OnBingoAchieved += listener;
    }

    public void PlaceCardDataOnly(BingoCell cell)
    {
        Debug.Log($"{cell.X},{cell.Y} 입력");
        var cardType = cell.TokenType;
        cell.PlaceToken(cardType);
        //각 라인에 추가
        _horizontalLines[cell.Y].AddHandCard(cardType);
        _verticalLines[cell.X].AddHandCard(cardType);
        if (cell.X == cell.Y)
            _diagonalLines[0].AddHandCard(cardType);
        if (cell.X + cell.Y == LineLength - 1)
            _diagonalLines[1].AddHandCard(cardType);
        //모든 칸이 채워졌는지 확인
        IsFull = _cells.Cast<BingoCell>().All(c => c.State == CellTokenState.Done);
    }
    #region Getter 
    public BingoCell GetEmpthyCell()
    {
        var empthyCells = _cells.Cast<BingoCell>().Where(c => c.CanPlace).ToArray();

        if(empthyCells.Length == 0)
            return null;

        return empthyCells.GetRandom();
    }
    public BingoCell GetCell(int x, int y)
    {
        if (x < 0 || x >= LineLength || y < 0 || y >= LineLength)
            throw new ArgumentOutOfRangeException();
        return _cells[x, y];
    }
    public BingoCell[] GetCells(BingoLineRef line)
    {
        if (line == null)
            throw new ArgumentNullException("BingoLineRef is null");
        BingoCell[] slots = new BingoCell[LineLength];
        switch (line.Type)
        {
            case BingoLineType.Horizontal:
                for (int i = 0; i < LineLength; i++)
                    slots[i] = _cells[i, line.Index - 1];
                return slots;
            case BingoLineType.Vertical:
                for (int i = 0; i < LineLength; i++)
                    slots[i] = _cells[line.Index - 1, i];
                return slots;
            case BingoLineType.Diagonal:
                if (line.Index == 1)
                {
                    for (int i = 0; i < LineLength; i++)
                        slots[i] = _cells[i, i];
                    return slots;
                }
                else if (line.Index == 2)
                {
                    for (int i = 0; i < LineLength; i++)
                        slots[i] = _cells[i, LineLength - 1 - i];
                    return slots;
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public BingoLine GetLine(int line)
    {
        if (!BingoLineRegistry.IsValidLineId(line))
            throw new ArgumentOutOfRangeException();
        var lineRef = BingoLineRegistry.GetLineRef(line);
        return GetLine(lineRef);
    }

    public BingoLine GetLine(BingoLineRef lineRef)
    {
        if (lineRef == null)
            throw new ArgumentNullException();
        switch (lineRef.Type)
        {
            case BingoLineType.Horizontal:
                return _horizontalLines[lineRef.Index - 1];
            case BingoLineType.Vertical:
                return _verticalLines[lineRef.Index - 1];
            case BingoLineType.Diagonal:
                return _diagonalLines[lineRef.Index - 1];
            default:
                Debug.Log($"{lineRef.Type}");
                throw new ArgumentOutOfRangeException("");
        }
    }
    #endregion
    #region GetLineRank
    public HandResult GetLineResult(int line)
    {
        if (!BingoLineRegistry.IsValidLineId(line))
            throw new ArgumentOutOfRangeException();
        var lineRef = BingoLineRegistry.GetLineRef(line);
        Debug.Log($"{lineRef.Type}-{lineRef.Index}");
        return GetLineResult(lineRef);
    }

    public HandResult GetLineResult(BingoLineRef lineRef)
    {
        if (lineRef == null)
            throw new ArgumentNullException();
        switch (lineRef.Type)
        {
            case BingoLineType.Horizontal:
                return _horizontalLines[lineRef.Index - 1].GetResult();
            case BingoLineType.Vertical:
                return _verticalLines[lineRef.Index - 1].GetResult();
            case BingoLineType.Diagonal:
                return _diagonalLines[lineRef.Index - 1].GetResult();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IEnumerable<HandResult> GetLineResultAll ()
    {
        for (int i = 1; i <= 12; i++)
        {
            yield return GetLineResult(i);
        }
    }
    #endregion
}
