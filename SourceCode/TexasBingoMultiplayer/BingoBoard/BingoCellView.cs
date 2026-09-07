using DG.Tweening;
using System;
using TB.Card;
using UnityEngine;
public class BingoCellView : MonoBehaviour, IClickable
{
    private BingoCell _cell;
    private BingoBoardView _boardView;

    public event Action<BingoCell> OnClicked;

    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _token;
    private SpriteRenderer[] _petrifiedImages;

    //플레이어에 대한 스킨으로 수정 필요
    private CardSkinSO CardSkin => GameManager.Instance.TEST_cardSkin;

    public bool IsClickable
    {
        get => GameManager.Instance.CanPlace && _cell.CanPlace;
    }

    [ContextMenu("상태")]
    public void TEST() => Debug.Log($"{_cell.State}");

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _token = transform.GetChild(1).GetComponent<SpriteRenderer>();
        _petrifiedImages = new SpriteRenderer[2];
        _petrifiedImages[0] = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _petrifiedImages[0].material = new Material(_petrifiedImages[0].material);
        _petrifiedImages[0].enabled = false;
        _petrifiedImages[1] = _token.transform.GetChild(0).GetComponent<SpriteRenderer>();
        _petrifiedImages[1].material = new Material(_petrifiedImages[1].material);
        _petrifiedImages[1].enabled = false;
    }

    public void Bind(BingoCell cell, BingoBoardView boardView)
    {
        _cell = cell;
        _boardView = boardView;
    }

    public void OnClick()
    {
        OnClicked?.Invoke(_cell);
    }
    //BingoBoardRuntime의 TryPlaceCard에서 호출
    public void UpdateTokenVisual(BingoCell cell)
    {
        //빈 셀로 바꼈을 경우
        if (!cell.HasToken)
        {
            _token.enabled = false;
            return;
        }

        _token.sprite = CardSkin.GetTokenSprite(cell.TokenType);
        _token.enabled = true;
        switch (cell.State)
        {
            case CellTokenState.Empty:
                break;
            case CellTokenState.Preview:
                _token.color = new Color(1f, 1f, 1f, .15f);
                break;
            case CellTokenState.Ready:
                _token.color = new Color(1f, 1f, 1f, .5f);
                _token.transform.localScale = Vector3.zero;
                break;
            case CellTokenState.Committed:
                _token.color = new Color(1f, 1f, 1f, .5f);
                break;
            default:
                break;
        }
    }

    public Sequence PlayPreviewAnimation()
    {
        return DOTween.Sequence()
            .Append(_token.DOFade(.5f, .5f))
            .AppendInterval(.5f)
            .Append(_token.DOFade(.25f, .5f))
            .AppendInterval(0.1f)
            .SetLoops(-1)
            .SetTarget(_token);
    }

    public Sequence PlayReadyAnimation()
    {
        return DOTween.Sequence()
            .Append(_token.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutElastic))
            .SetTarget(_token);
    }

    public Sequence PlayCommitAnimation()
    {
        return DOTween.Sequence()
            .Append(_token.transform.DOScaleX(1.5f, 0.2f))
            .Join(_token.transform.DOScaleY(0.6f, 0.2f))
            .Append(_token.transform.DOScaleX(0.6f, 0.2f))
            .Join(_token.transform.DOScaleY(1.5f, 0.2f))
            .Append(_token.transform.DOScaleX(1f, 0.2f))
            .Join(_token.transform.DOScaleY(1f, 0.2f))
            .Insert(0f, _token.DOFade(1f, 0.6f))
            .SetTarget(_token);
    }


    public void PlayEffectAnimation()
    {
    }

    public Sequence SetPetrified(bool active)
    {
        if (active)
        {
            _petrifiedImages[0].enabled = true;
            if (_cell.TokenType != PlayingCardType.None)
            {
                _petrifiedImages[1].enabled = true;
                _petrifiedImages[1].sprite = CardSkin.GetTokenSprite(_cell.TokenType, true);
            }
            var seq = DOTween.Sequence()
                .AppendInterval(1f)
                .Append(_petrifiedImages[0].material.DOFloat(0f, "_Dissolve", 1f));
            if (_cell.TokenType != PlayingCardType.None)
                seq.Join(_petrifiedImages[1].material.DOFloat(0f, "_Dissolve", 1f));

            return seq;
        }
        else
        {
            //불필요 호출 제거
            if (_petrifiedImages[0].enabled == false)
            {
                return null;
            }
            var seq = DOTween.Sequence()
                .Append(_petrifiedImages[0].material.DOFloat(1f, "_Dissolve", 1f));

            if (_cell.TokenType != PlayingCardType.None)
            {
                seq.Join(_petrifiedImages[1].material.DOFloat(1f, "_Dissolve", 1f))
                .OnComplete(() =>
                {
                    _petrifiedImages[0].enabled = false;
                    _petrifiedImages[1].enabled = false;
                    _petrifiedImages[1].sprite = null;
                });
            }
            else
            {
                seq.OnComplete(() => _petrifiedImages[0].enabled = false);
            }
            return seq;
        }
    }
    public void InitAllEffect()
    {
        _token.DOKill();
        _token.color = Color.white;
    }

    public void UpdateEffectVisual(BingoCell cell)
    {
        //석화 관련 판단
        if (cell.IsPetrified)
        {
            var seq = SetPetrified(true);
            seq?.Play();
        }
        else
        {
            var seq = SetPetrified(false);
            seq?.Play();
        }
        //모래 바람 관련 판단
        if (cell.IsSandstorm)
        {
            _spriteRenderer.DOColor(new Color(1f, 0.3843f, 0.3843f, 1f), 0.25f);
        }
        else
        {
            //불필요 호출 제거
            if (_spriteRenderer.color == Color.white)
                return;
            _spriteRenderer.DOColor(Color.white, 0.25f);
        }
    }
}
