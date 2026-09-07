using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//DialogueSystem에서 사용되는 캐릭터 별 고유 말풍선
public class UI_SpeechBubble : UI_Base
{
    public static readonly float CharTypingDuration = 0.075f;
    enum Images
    {
        BubbleArea,
        BubbleTail
    }
    enum TMP_Texts
    {
        MessageText,
        SkipText,
    }

    private Image _bubbleArea;
    private Image _bubbleTail;
    private TMP_Text _messageText;
    private TMP_Text _skipText;
    //========================================
    private Sequence _showSeq;
    private Sequence _hideSeq;
    private Sequence _blinkEnterSeq;
    private Tween _blinkDelayTween;
    private Tween _typingTween;
    //========================================

    private bool _isTyping;
    public bool IsTyping
    {
        get { return _isTyping; }
        set
        {
            if (_isTyping == value)
                return;
            _isTyping = value;
            BlinkEnterWithDelay(!value);
        }
    }
    public Character Owner { get; set; }
    public event Action OnTypingCompleted;
    private string _fullMessage;
    private readonly float _defaultAjustWidth = 40f;
    private readonly float _minWidth = 150f;
    private readonly float _maxWidth = 450f;
    //한 글자의 사이즈 오차에 대한 추가 값
    private readonly float _charWidthBias = 3.5f;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        BindImages(typeof(Images));
        BindTexts(typeof(TMP_Texts));
        _bubbleArea = GetImage((int)Images.BubbleArea);
        _bubbleTail = GetImage((int)Images.BubbleTail);
        _messageText = GetText((int)TMP_Texts.MessageText);
        _skipText = GetText((int)TMP_Texts.SkipText);
        _skipText.gameObject.SetActive(false);
        _bubbleArea.transform.localScale = Vector3.zero;
        _bubbleTail.transform.localScale = Vector3.zero;
        _messageText.text = "";
        gameObject.SetActive(false);
        return true;
    }
    #region Show Hide
    //말풍선 등장 및 타이핑 연출까지 진행
    public void Show(string message, EDialogEventType eventType = EDialogEventType.Interactive)
    {
        bool isMonologue = (eventType == EDialogEventType.Monologue);

        //스프라이트 전용 더미 Json 데이터 파일 필요
        string spritesName = isMonologue ? "UI_SpeechBubble_Mono" : "UI_SpeechBubble";
        //말풍선 이미지 초기화
        var sprites = Managers.Resource.LoadAll<Sprite>(spritesName);
        _bubbleArea.sprite = sprites[0];
        _bubbleTail.sprite = sprites[1];

        _fullMessage = message;

        //꺼져있다면 활성화 애니메이션 재생
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
            PlayShowAnimation(ShowTyping);
            return;
        }

        bool needRecover =
            _bubbleArea.transform.localScale.sqrMagnitude < 0.01f ||
            _bubbleTail.transform.localScale.sqrMagnitude < 0.01f;

        if (needRecover)
        {
            _bubbleArea.transform.localScale = Vector3.one;
            _bubbleTail.transform.localScale = Vector3.one;
        }

        SkipTyping();
        ShowTyping();
    }
    //말풍선 등장 연출
    private void PlayShowAnimation(Action onComplete = null)
    {
        CencelAll();
        _skipText.gameObject.SetActive(false);
        _bubbleArea.rectTransform.sizeDelta = new Vector2(_minWidth, _bubbleArea.rectTransform.sizeDelta.y);
        _messageText.rectTransform.sizeDelta = new Vector2(_minWidth, _messageText.rectTransform.sizeDelta.y);
        _messageText.text = "";
        _bubbleArea.transform.localScale = Vector3.zero;
        _bubbleTail.transform.localScale = Vector3.zero;
        _showSeq = DOTween.Sequence()
        .Append(_bubbleArea.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack))
        .Join(_bubbleTail.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetDelay(0.15f));
        _showSeq.OnComplete(() => onComplete?.Invoke());
    }
    //말풍선 사라짐 연출
    public void Hide()
    {
        CencelAll();
        _hideSeq = DOTween.Sequence()
            .Append(_bubbleTail.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack))
            .Join(_bubbleArea.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetDelay(0.1f))
            .OnComplete(() =>
            {
                _messageText.text = "";
                //회수되는 로직 추가
            });
    }
    #endregion
    #region Typing
    //텍스트 타이핑 연출
    public void ShowTyping()
    {
        if (string.IsNullOrEmpty(_fullMessage))
            return;

        CencelAll();
        _typingTween = null;
        IsTyping = true;

        int totalCount = _fullMessage.Length;
        if (totalCount <= 0)
            return;

        int lastCount = -1;
        float textWidth = _defaultAjustWidth;
        float logestWidth = _defaultAjustWidth;
        float totalTypingDuration = totalCount * CharTypingDuration;
        ResizeWidth();
        _typingTween = DOVirtual.Int(0, totalCount, totalTypingDuration, count =>
        {
            if (count == lastCount)
                return;
            lastCount = count;
            _messageText.text = _fullMessage.Substring(0, count);
            _messageText.ForceMeshUpdate();

            if (count > 0)
            {
                char currentChar = _fullMessage[count - 1];


                if (currentChar != ' ' && currentChar != '\n')
                {

                    Managers.Sound.Play(Define.ESound.Effect, "Effect_Typing", 0.8f);
                }
            }

            if (count <= 0)
            {
                ResizeWidth(logestWidth);
                return;
            }

            float newCharWidth = GetCharWidth(_messageText.text[count - 1]);
            //줄넘김
            if (newCharWidth == -1)
            {
                logestWidth = Mathf.Max(logestWidth, textWidth);
                textWidth = 0;
            }
            else
            {
                //보정치와 함께
                textWidth += newCharWidth + _charWidthBias;
                logestWidth = Mathf.Max(logestWidth, textWidth);
            }
            ResizeWidth(logestWidth);
        })
        .SetEase(Ease.Linear)
        .OnKill(() => _typingTween = null)
        .OnComplete(() =>
        {
            _typingTween = null;
            CompleteTyping();
        });
    }
    //텍스트 타이핑 강제 스킵
    public void SkipTyping()
    {
        if (IsTyping == false)
            return;
        CencelAll();
        CompleteTyping();
        //최종 텍스트에 대한 사이즈 계산
        float textWidth = GetCharWidth(_fullMessage);
        ResizeWidth(textWidth);
        _messageText.ForceMeshUpdate();
    }
    //타이핑 완료 후 호출
    private void CompleteTyping()
    {
        _messageText.text = _fullMessage;
        IsTyping = false;
        OnTypingCompleted?.Invoke();
    }
    #endregion
    #region Width
    //말풍선 가로 사이즈 조정
    private void ResizeWidth(float width = 0, bool force = false)
    {
        float newWidth = force ? width : Mathf.Clamp(width, _minWidth, _maxWidth);
        //텍스트 영역 범위 수정
        _messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        //말풍선 영역 범위 수정
        Vector2 size = _bubbleArea.rectTransform.sizeDelta;
        size.x = newWidth;
        _bubbleArea.rectTransform.sizeDelta = size;
    }

    //단어에 대한 추정 사이즈 반환
    float GetCharWidth(string s)
    {
        float width = _defaultAjustWidth;
        float maxWidth = _defaultAjustWidth;
        foreach (var c in s)
        {
            if (GetCharWidth(c) == -1)
            {
                maxWidth = Mathf.Max(maxWidth, width);
                width = 0;
                continue;
            }
            width += GetCharWidth(c) + _charWidthBias;
        }
        maxWidth = Mathf.Max(maxWidth, width);
        return maxWidth;
    }

    //단어에 대한 추정 사이즈 반환
    float GetCharWidth(char c)
    {
        TMP_FontAsset font = _messageText.font;
        //줄넘김 예외 처리를 위한 값
        if (c == '\n')
        {
            return -1;
        }

        if (font.characterLookupTable.TryGetValue(c, out TMP_Character character))
        {
            float advance = character.glyph.metrics.horizontalAdvance;
            float scale = _messageText.fontSize / font.faceInfo.pointSize;

            return advance * scale;
        }
        return 0f;
    }
    #endregion
    //트윈 및 시퀀스 종료
    private void CencelAll()
    {
        _showSeq?.Kill();
        _hideSeq?.Kill();
        _typingTween?.Kill();
        _blinkEnterSeq?.Kill();
    }
    #region BlinkEnter
    private void BlinkEnterWithDelay(bool active)
    {
        // 기존 예약 취소
        _blinkDelayTween?.Kill();
        _blinkDelayTween = null;

        if (active)
        {
            // 혹시 이전 시퀀스가 남아있다면 정리
            _blinkEnterSeq?.Kill();
            _blinkEnterSeq = null;

            _blinkDelayTween = DOVirtual.DelayedCall(1.5f, StartBlinkEnter);
        }
        else
        {
            StopBlinkEnter();
        }
    }

    private void StartBlinkEnter()
    {
        _blinkDelayTween = null;

        _blinkEnterSeq?.Kill();
        _blinkEnterSeq = DOTween.Sequence()
            .OnStart(() =>
            {
                _skipText.gameObject.SetActive(true);
                _skipText.alpha = 0.1f;
            })
            .Append(_skipText.DOFade(0.8f, 1f))
            .AppendInterval(0.5f)
            .Append(_skipText.DOFade(0.1f, 1f))
            .AppendInterval(0.1f)
            .SetLoops(-1, LoopType.Restart);
    }
    private void StopBlinkEnter()
    {
        _blinkDelayTween = null;

        _blinkEnterSeq?.Kill();
        _blinkEnterSeq = null;

        _skipText.gameObject.SetActive(false);
    }
    #endregion
}
