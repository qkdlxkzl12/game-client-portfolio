using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Radishmouse;

//올바른 증거에 대한 정보
public struct CorrectInfo
{
    public int SlotIdx; // 사용자가 카드를 놓은 위치 (0, 1, 2...)
    public int DataIdx; // 데이터상 정답의 순서 (ID 추출용)
}
//정답 판정 시 결과 형태
public struct CheckResult
{
    public bool IsComplete;
    public List<CorrectInfo> CorrectInfos; // 단순 int 리스트 대신 상세 정보 리스트
    public List<int> WrongIndexes;         // 오답은 슬롯 위치만 알아도 충분
}
public enum EArrangementType
{
    Extended,   //확장형 - 증거가 병렬(수직) 형탤로 나열됨
    Linked,     //연계형 - 증거가 직렬(수평) 형태로 나열됨 
}
//증거 위치 상태
public enum EClueCardState
{
    None,
    Hand,   //배치하지 않은 상태
    Placed, //보드에 배치한 상태
}

//보드 조작(스크롤, 스케일업 등)에 활용되는 값
public static class IncidentBoardInfo
{
    public static readonly Vector2Int ScreensAvailableGridCount = new Vector2Int(4, 2); //기본 크기 기준 화면에 보이는 그리드 개수
    public static readonly Vector2Int TotalGridCount = new Vector2Int(11, 4);           //보드를 이루는 총 그리드 개수
    public static readonly Vector2Int GridOffset = new Vector2Int(480, -540);           //한 그리드의 크기
    public static readonly Color LineColor = new Color32(255, 85, 78, 255);             //연결선의 대한 색상값
    public static readonly Vector2Int FrameSize = new Vector2Int(100, 100);           //배치 이외의 영역인 액자 틀 영역 사이즈

    //그리드 위치에 대한 배치 피벗(왼쪽 상단) 기준 실제 위치값 반환
    public static Vector2 GetAnchoredPos(Vector2Int point)
    {
        return GridOffset * (point + Vector2.one * 0.5f);
    }

    //기본 크기 기준 최대 이동 거리 계산
    public static Vector2 GetMaxMoveRange()
    {
        float xRange = Mathf.Abs(GridOffset.x * (TotalGridCount.x - ScreensAvailableGridCount.x));
        float yRange = Mathf.Abs(GridOffset.y * (TotalGridCount.y - ScreensAvailableGridCount.y));

        return new Vector2(xRange, yRange);
    }

    //확대/축소가 적용된 값으로 변환
    public static Vector3 TransToAvailablePosition(Vector3 position, float currentScale)
    {
        Vector2 totalSize = new Vector2(
            Mathf.Abs(GridOffset.x) * TotalGridCount.x,
            Mathf.Abs(GridOffset.y) * TotalGridCount.y);

        Vector2 screenSize = new Vector2(
            Mathf.Abs(GridOffset.x) * ScreensAvailableGridCount.x,
            Mathf.Abs(GridOffset.y) * ScreensAvailableGridCount.y);

        // 전체 보드에만 스케일이 적용되고, 화면 영역은 고정
        Vector2 moveRange = Vector2.Max(
            totalSize * currentScale - screenSize,
            Vector2.zero);

        Vector2 frameOffset = (Vector2)FrameSize * currentScale;

        float minX = -(moveRange.x + frameOffset.x);
        float maxX = frameOffset.x;

        float minY = -frameOffset.y;
        float maxY = moveRange.y + frameOffset.y;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}

public class UI_IncidentBoard : UI_Popup
{
    #region Popup
    Canvas Canvas { get; set; }
    public override bool UseBlinder => false;
    public override bool AffectedCancel => _isAnyCompleted;
    #endregion
    #region Items
    public List<UI_AskingCard> AskingCards { get; private set; } = new();
    public List<UI_ClueCard> HandClues { get; private set; } = new();
    public List<UILineRenderer> Lines { get; private set; } = new();
    public Dictionary<Vector2Int, Image> Pins { get; private set; } = new();
    #endregion
    #region Roots
    CanvasGroup MainRoot { get; set; }
    RectTransform HandClueRoot { get; set; }
    RectTransform LineRoot { get; set; }
    RectTransform PinRoot { get; set; }
    public RectTransform TreeRoot { get; private set; }
    #endregion
 
    #region Event
    public event Action<int> OnRegistrationClue;        //새 증거를 등록했을 때(askingId)
    public event Action<int, int, bool> OnCompareClue;  //질문에 대한 증거 제출에 대한 판별 여부(askingId, clueId, isCorrect)
    public event Action<int, bool> OnCompleteAsking;    //질문에 대한 작용을 모두 완수 했을 때(askingId, isComplete)
    public event Action OnClickAnywhere;                //어디든 클릭했을 때
    #endregion

    private RectTransform[] _affectedScalerTranses;
    private bool _isAnyCompleted = false; // 하나라도 완성되었는지 체크하는 변수 - 임시 
    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        Canvas = GetComponent<Canvas>();
        MainRoot = Util.FindChild(gameObject, "Root", true).GetComponent<CanvasGroup>();
        HandClueRoot = Util.FindChild(gameObject, "HandClueList", true).transform as RectTransform;
        LineRoot = Util.FindChild(gameObject, "@Lines", true).transform as RectTransform;
        PinRoot = Util.FindChild(gameObject, "@Pins", true).transform as RectTransform;
        TreeRoot = Util.FindChild<RectTransform>(gameObject, "@Trees", true);
        HandClues = new List<UI_ClueCard>();
        _affectedScalerTranses = new RectTransform[3];
        _affectedScalerTranses[0] = Util.FindChild<RectTransform>(gameObject, "GridGround", true);
        _affectedScalerTranses[1] = Util.FindChild<RectTransform>(gameObject, "Placed", true);
        _affectedScalerTranses[2] = Util.FindChild<RectTransform>(gameObject, "Dynamic", true);
        //화면 기반 요소틀을 왼쪽 상단을 포함한 위치로 조정
        foreach(var _affectedScalerTrans in _affectedScalerTranses)
        {
            float scale = _affectedScalerTranses[0].localScale.x;
            Vector2 position = IncidentBoardInfo.FrameSize * new Vector2(scale, -scale); 
            _affectedScalerTrans.anchoredPosition = position;
        }
        var scaleSlider = Util.FindChild<Slider>(gameObject, "ScaleSlider", true);
        scaleSlider.onValueChanged.AddListener(value =>
        {
            float targetScale = (2f + value) / 5f; // 0.4 ~ 0.6 (슬라이더 범위에 따라 조절)
            Debug.Log($"value={value}, targetScale={targetScale}");
            foreach (var trans in _affectedScalerTranses)
            {
                trans.DOKill();

                // 1. 현재 위치를 새로운 스케일 기준 유효 범위로 미리 계산
                Vector3 validatedPos = IncidentBoardInfo.TransToAvailablePosition(trans.anchoredPosition, targetScale);

                // 2. 스케일과 위치를 동시에 트윈 (부드러운 연출 유지)
                trans.DOScale(targetScale, 0.2f).SetEase(Ease.OutSine);
                trans.DOAnchorPos(validatedPos, 0.2f).SetEase(Ease.OutSine);
            }
        });
        var scroll = Util.GetOrAddComponent<UI_EventHandler>(_affectedScalerTranses[0].gameObject);
        scroll.OnDragHandler += ev =>
        {
            Vector2 delta = ev.delta / Canvas.scaleFactor;
            float scale = _affectedScalerTranses[0].localScale.x;

            foreach (var trans in _affectedScalerTranses)
            {
                // 1. 단순 이동 후
                Vector2 nextPos = trans.anchoredPosition + delta;
                // 2. 가용 범위 검열 및 적용
                trans.anchoredPosition = IncidentBoardInfo.TransToAvailablePosition(nextPos, scale);
            }
        };
        return true;
    }
    private void Start()
    {
        SpawnItem<UI_AskingCard>(50101001, new Vector2Int(0, 0));
        InitGuide();
    }
    private void Update()
    {
        //클릭 감지(가이드 사용)
        if (Input.GetMouseButtonDown(0) && Util.IsPointerOverClickableUI(true) == false)
        {
            OnClickAnywhere?.Invoke();
        }
    }

    #region override
    public override bool OpenPopupUI()
    {
        if (base.OpenPopupUI() == false)
            return false;
        return true;
    }

    protected override Sequence CreateOpenSequence()
    {
        var openSequence = base.CreateOpenSequence();
        openSequence.Append(MainRoot.transform.DOScale(1f, 0.25f).SetEase(Ease.OutQuad))
            .Join(MainRoot.DOFade(1f, 0.15f).SetEase(Ease.OutQuad));
        openSequence.onPlay += () =>
        {
            MainRoot.transform.localScale = Vector3.zero;
            MainRoot.alpha = 0;
        };
        openSequence.onComplete += () =>
        {
            var remainClues = Managers.Game.Clues.GetRemainClues();
            foreach (var clueId in remainClues)
            {
                SpawnItem<UI_ClueCard>(clueId, Vector2Int.zero);
            }
            RefreshHandCard();
        };
        return openSequence;
    }

    protected override Sequence CreateCloseSequence()
    {
        var closeSequence = base.CreateCloseSequence();
        closeSequence.Append(MainRoot.transform.DOScale(0f, 0.25f).SetEase(Ease.OutQuad));
        closeSequence.onPlay += () =>
        {
            MainRoot.transform.localScale = Vector3.one;
            MainRoot.alpha = 1;
        };
        closeSequence.onComplete += () =>
        {
            foreach (var clue in HandClues.ToList())
            {
                HandClues.Remove(clue);
                Destroy(clue.gameObject);
            }
            Managers.InteractEffect.EndInteract();
        };
        return closeSequence;
    }
    #endregion
    //판정 관련 로직
    #region Result
    public void CheckBoard()
    {
        //새로운 메모가 추가될 수 있어 현재 값의 복사본으로 비교
        var currentAskingCards = new List<UI_AskingCard>(AskingCards);
        foreach (var askingCard in currentAskingCards)
        {
            var result = askingCard.ValidateClues();

            // 1. 오답 처리 (배열 형태)
            var wrongClues = askingCard.CollectedClueCards
                .Select((clue, index) => new { clue, index }) // 인덱스와 클루를 쌍으로 묶음
                .Where(x => x.clue != null && result.WrongIndexes.Contains(x.index)) // WrongIndexes(인덱스)에 포함되는지 확인
                .OrderByDescending(x => x.index) // 뒤에서부터 처리하기 위해 인덱스 기준 내림차순 정렬
                .Select(x => x.clue) // 최종적으로 UI_ClueCard 객체만 추출
                .ToArray();

            // 2. 타입별 필수 액션 수행
            if (askingCard.Type == EArrangementType.Extended)
            {
                foreach (var info in result.CorrectInfos)
                {
                    if (askingCard.CompleteExtendeds[info.SlotIdx])
                        continue;

                    //판별 처리 - true
                    OnCompareClue?.Invoke(askingCard.TemplatedId, info.DataIdx, true);

                    askingCard.CompleteExtendeds[info.SlotIdx] = true;
                    GenerateNextStep(askingCard, info.SlotIdx, info.DataIdx);
                }
                //판별 처리 - false
                foreach (var wrongIndex in result.WrongIndexes)
                    OnCompareClue?.Invoke(askingCard.TemplatedId, wrongIndex, false);
            }
            else if (askingCard.Type == EArrangementType.Linked)
            {
                // 판별 처리
                foreach (var info in result.CorrectInfos)
                {
                    OnCompareClue?.Invoke(askingCard.TemplatedId, info.DataIdx, result.IsComplete);
                }
                // 해설지 제공 및 추가 메모 발생 (필수/선택)
                if (result.IsComplete)
                    ProvideSummaryAndMemo(askingCard);
            }

            //완료 처리된 증거에 대해 사용 처리 
            foreach (var correctInfo in result.CorrectInfos)
            {
                // 임시하나라도 완성되면 ESC 취소가 가능해지도록 true로 변경!
                _isAnyCompleted = true;
                var clue = askingCard.RequiredClues[correctInfo.DataIdx];
                Managers.Game.Clues.UseClue(clue.TemplatedId);
            }
            // 한꺼번에 핸드로 복귀 (ReturnToHand 내부에서 리스트 제거 로직 등이 포함되어 있어야 함)
            askingCard.ClearCollected(wrongClues);
            ReturnToHand(wrongClues);
            //완료된 질문지는 관리에서 제외
            if (result.IsComplete)
            {
                AskingCards.Remove(askingCard);
                OnCompleteAsking?.Invoke(askingCard.TemplatedId, result.IsComplete);
            }
        }
        PlayButtonSound();
        RefreshHandCard();
    }
    private void GenerateNextStep(UI_AskingCard askingCard, int slotIdx, int dataIdx)
    {
        var newAskingCardId = askingCard.Data.NextAskingCardIds[dataIdx];

        var beforeItem = askingCard.GetEndItem(slotIdx);

        // 3. 생성
        SpawnItem<UI_AskingCard>(newAskingCardId, beforeItem.RightPoint, beforeItem);
    }
    private void ProvideSummaryAndMemo(UI_AskingCard askingCard)
    {
        var beforeItem = askingCard.GetEndItem();
        var solutionCardId = askingCard.Data.SolutionCardId;
        askingCard.SolutionCard = SpawnItem<UI_SolutionCard>(solutionCardId, beforeItem.RightPoint, beforeItem);
        //이어서 질문지있다면 생성
        if (askingCard.Data.NextAskingCardIds.Length != 0)
        {
            GenerateNextStep(askingCard, 0, 0);
        }
    }
    #endregion
    //증거 카드 조작 로직
    #region HandCard
    public void RefreshHandCard(float duration = 0.5f)
    {
        DOTween.Kill(HandClueRoot);

        Sequence refreshSequence = DOTween.Sequence()
            .SetTarget(HandClueRoot) // 타겟 지정
            .OnStart(() => {
                foreach (var c in HandClues) c.IsProcessing = true;
            })
            .OnComplete(() => {
                foreach (var c in HandClues) c.IsProcessing = false;
            })
            .OnKill(() => {
                foreach (var c in HandClues) c.IsProcessing = false;
            });

        for (int i = 0; i < HandClues.Count; i++)
        {
            var clue = HandClues[i];
            float targetX = -75 * (HandClues.Count - 1) / 2f + i * 75;
            Vector2 targetPosition = new Vector2(targetX, 37);

            // Join으로 병렬 실행
            refreshSequence.Join(clue.Rect.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic));
            refreshSequence.Join(clue.Rect.DORotate(Vector3.zero, duration).SetEase(Ease.OutCubic));
            refreshSequence.Join(clue.Rect.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        }
        refreshSequence.Play();
    }
    public void ReturnToHand(UI_ClueCard[] clues)
    {
        foreach (var clue in clues)
        {
            if (clue == null || HandClues.Contains(clue) == true)
                return;
            clue.transform.SetParent(HandClueRoot);
            clue.IsPaced = false;
            HandClues.Add(clue);
        }
        ReleasePin(clues.Select(c => c.Point).ToArray());
    }
    public void SetPlace(UI_BoardItem item, Vector2Int point, Action onComplete = null)
    {
        item.transform.SetParent(TreeRoot);
        item.MoveToPosition(point, 0.3f, onComplete);
    }
    public void BegineFocusClue(PointerEventData evData, UI_ClueCard target)
    {
        if (target.CanMove == false) return;

        int targetIndex = HandClues.IndexOf(target);
        int totalCount = HandClues.Count;
        int maxDiff = Mathf.Max(targetIndex, (totalCount - 1) - targetIndex);
        float originX = -75f * (totalCount - 1) / 2f + targetIndex * 75f;

        for (int i = 0; i < totalCount; i++)
        {
            var clue = HandClues[i];
            int diff = i - targetIndex;

            if (i == targetIndex)
            {
                clue.Rect.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DOAnchorPos(new Vector2(originX, 230), 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
            }
            else
            {
                float rotationUnit = maxDiff > 0 ? 30f / maxDiff : 0f;
                float finalRotation = -diff * rotationUnit;
                float margin = (targetIndex == 0 || targetIndex == totalCount - 1) ? 50f : 140f;
                float targetX = originX + (diff * 75f) + (Mathf.Sign(diff) * margin);
                float targetY = 37f + 30f;

                clue.Rect.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DOAnchorPos(new Vector2(targetX, targetY), 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DORotate(new Vector3(0, 0, finalRotation), 0.2f).SetEase(Ease.OutBack);
            }
        }
    }
    public void EndFocusClue(PointerEventData evData, UI_ClueCard target)
    {
        if (target.CanMove == false)
            return;
        int targetIndex = HandClues.IndexOf(target);
        for (int i = 0; i < HandClues.Count; i++)
        {
            var clue = HandClues[i];
            Vector2 defaultPosition = new Vector2(-75 * (HandClues.Count - 1) / 2 + i * 75, 37);
            if (i == targetIndex)
            {
                clue.Rect.DOAnchorPos(defaultPosition, 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            }
            else
            {
                clue.Rect.DOAnchorPos(defaultPosition, 0.2f).SetEase(Ease.OutBack);
                clue.Rect.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
            }
        }
    }
    public void HandleDropClue(PointerEventData evData, UI_ClueCard target)
    {
        var askingCard = GetOverlapItem<UI_AskingCard>(evData);
        if (askingCard != null)
        {
            var lastItem = askingCard.GetEndItem();
            if (askingCard.TryAddClue(target, out Vector2Int point))
            {

                SetPlace(target, point, () => {
                    CreateConnection(lastItem, target);
                    OnRegistrationClue?.Invoke(askingCard.TemplatedId);
                });

                HandClues.Remove(target);
                RefreshHandCard();
                return;
            }
            else {
                Debug.Log($"쓰레기양");
            }
        }

        EndFocusClue(evData, target);
    }
    private void CreateConnection(UI_BoardItem startItem, UI_BoardItem targetItem)
    {
        Vector3 startScale = startItem.transform.localScale;
        Vector3 targetScale = targetItem.transform.localScale;
        startItem.transform.localScale = Vector3.one;
        targetItem.transform.localScale = Vector3.one;

        Canvas.ForceUpdateCanvases();

        Vector2 lineLinkPoint = PinRoot.GetAnchoredPosFromOther(startItem.GetPinPoint(), Canvas);
        Vector2 position = PinRoot.GetAnchoredPosFromOther(targetItem.GetPinPoint(), Canvas);

        startItem.transform.localScale = startScale;
        targetItem.transform.localScale = targetScale;

        var pin = AddPin(position, lineLinkPoint);
        Pins.Add(targetItem.Point, pin);
    }
    #endregion
    //새 아이템 등록 시 핀 및 줄 생성 로직
    #region Pin
    private Image AddPin(Vector2 position, Vector2 lineLinkPoint)
    {
        // 1. 핀 생성 및 애니메이션
        var pin = Managers.Resource.Instantiate("UI_Pin", PinRoot).GetComponent<Image>();
        pin.rectTransform.anchoredPosition = position;
        pin.rectTransform.localScale = Vector2.one * 1.5f;
        pin.rectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce)
            .onPlay += () =>
            {
                Managers.Sound.Play(Define.ESound.Effect, "Effect_AddPin");
            };

        // 2. 단일 핀셋(시작점)일 경우 선 드로잉 없이 종료
        if (position == lineLinkPoint)
            return pin;

        Managers.Sound.Play(Define.ESound.Effect, "Effect_DrawLine");
        // 3. 기존 선들 중 끝점이 lineLinkPoint와 일치하는 선이 있는지 확인 (선 확장)
        foreach (var line in Lines)
        {
            // 선의 마지막 점이 현재 연결하려는 점(lineLinkPoint)과 근접한지 체크
            if (Vector2.Distance(line.points[^1], lineLinkPoint) < 0.1f)
            {
                int oldLength = line.points.Length;
                Vector2 lastPoint = line.points[^1]; // 현재 선의 끝점

                // 배열 크기를 1 늘림
                Vector2[] targetPoints = new Vector2[oldLength + 1];
                Array.Copy(line.points, targetPoints, oldLength);

                // 새 점의 초기 위치는 현재 끝점과 동일하게 설정 (애니메이션 시작점)
                targetPoints[oldLength] = lastPoint;
                line.points = targetPoints;

                // 끝점만 목표 위치(position)로 이동하는 애니메이션
                DOVirtual.Float(0f, 1f, 0.5f, value =>
                {
                    var animPoints = line.points;
                    // 현재 끝점을 시작점에서 목표점(position)으로 Lerp
                    animPoints[^1] = Vector2.Lerp(lastPoint, position, value);
                    line.points = animPoints;
                    line.SetVerticesDirty();
                }).SetEase(Ease.OutCubic);

                return pin;
            }
        }

        // 4. 연결할 기존 선이 없다면 새로운 선 생성
        var newLine = new GameObject("Line", typeof(UILineRenderer)).GetComponent<UILineRenderer>();
        newLine.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        newLine.transform.SetParent(LineRoot, false);

        // UI 좌표계 설정
        newLine.rectTransform.anchorMin = Vector2.up;
        newLine.rectTransform.anchorMax = Vector2.up;
        newLine.rectTransform.anchoredPosition = Vector2.zero;
        newLine.center = false;
        newLine.color = IncidentBoardInfo.LineColor;

        // 시작점(lineLinkPoint)과 목표점(position) 두 개로 구성된 선 생성
        Vector2[] newLinePoints = new Vector2[2];
        newLinePoints[0] = lineLinkPoint;
        newLinePoints[1] = lineLinkPoint; // 처음엔 길이가 0인 상태로 시작

        newLine.points = newLinePoints;
        Lines.Add(newLine);

        // 선이 길어지는 애니메이션
        DOVirtual.Float(0f, 1f, 0.3f, value =>
        {
            var animPoints = newLine.points;
            animPoints[1] = Vector2.Lerp(lineLinkPoint, position, value);
            newLine.points = animPoints;
            newLine.SetVerticesDirty();
        }).SetEase(Ease.OutCubic);

        return pin;
    }
    public void ReleasePin(Vector2Int[] points)
    {
        if (points == null || points.Length == 0) return;

        // 1. 제거될 핀들의 월드(Canvas) 좌표 수집 및 핀 제거 연출
        List<Vector2> releasePositions = CollectReleasePositions(points);
        if (releasePositions.Count == 0) return;

        // 2. 라인별 제거될 마디 수 계산
        int[] lineReleaseCounts = CalculateLineReleaseCounts(releasePositions);

        // 3. 줄 수축 애니메이션 실행
        PlayLineShrinkAnimations(lineReleaseCounts);
    }
    private List<Vector2> CollectReleasePositions(Vector2Int[] points)
    {
        List<Vector2> positions = new();
        foreach (var point in points)
        {
            if (Pins.TryGetValue(point, out var pin))
            {
                // 좌표 저장
                positions.Add(PinRoot.GetAnchoredPosFromOther(pin.rectTransform, Canvas));

                // 핀 제거 연출 (Tween)
                pin.DOFade(0, 0.3f);
                pin.rectTransform.DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => { if (pin != null) Destroy(pin.gameObject); });

                Pins.Remove(point);
            }
        }
        return positions;
    }

    private int[] CalculateLineReleaseCounts(List<Vector2> releasePositions)
    {
        int[] counts = new int[Lines.Count];
        const float matchThreshold = 1.0f;

        foreach (var relPos in releasePositions)
        {
            for (int i = Lines.Count - 1; i >= 0; i--)
            {
                var line = Lines[i];
                if (line == null || line.points == null) continue;

                int targetIdx = line.points.Length - 1 - counts[i];

                if (targetIdx > 0 && Vector2.Distance(line.points[targetIdx], relPos) < matchThreshold)
                {
                    counts[i]++;
                    break;
                }
            }
        }
        return counts;
    }
    private void PlayLineShrinkAnimations(int[] lineReleaseCounts)
    {
        Sequence mainSeq = DOTween.Sequence();
        const float shrinkDuration = 0.25f;

        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            int releaseCount = lineReleaseCounts[i];

            if (line == null || releaseCount <= 0 || line.points.Length <= 1) continue;

            // 애니메이션 데이터 스냅샷
            Vector2[] originalPoints = line.points.ToArray();
            int originalLength = originalPoints.Length;
            int newLength = Mathf.Max(1, originalLength - releaseCount);

            mainSeq.Join(
                DOVirtual.Float(0f, 1f, shrinkDuration, t =>
                {
                    if (line == null) return;

                    var currentPoints = line.points;
                    for (int idx = newLength; idx < originalLength; idx++)
                    {
                        if (idx >= currentPoints.Length) break;
                        currentPoints[idx] = Vector2.Lerp(originalPoints[idx], originalPoints[idx - 1], t);
                    }

                    line.points = currentPoints;
                    line.SetVerticesDirty();
                })
                .OnComplete(() => CleanupLine(line, originalPoints, newLength))
            );
        }
        mainSeq.Play();
    }

    private void CleanupLine(UILineRenderer line, Vector2[] originalPoints, int newLength)
    {
        if (line == null) return;

        if (newLength <= 1)
        {
            Lines.Remove(line);
            Destroy(line.gameObject);
        }
        else
        {
            Vector2[] finalPoints = new Vector2[newLength];
            Array.Copy(originalPoints, finalPoints, newLength);
            line.points = finalPoints;
            line.SetVerticesDirty();
        }
    }
    #endregion
    #region Item
    public T SpawnItem<T>(int id, Vector2Int point, UI_BoardItem owner = null) where T : UI_BoardItem
    {
        bool spawnOnTree = (typeof(T) != typeof(UI_ClueCard));
        RectTransform parent = spawnOnTree ? TreeRoot : HandClueRoot;
        var item = Managers.Resource.Instantiate(typeof(T).Name, parent).GetComponent<T>();
        if (spawnOnTree)
        {
            //owner없이 생성 시 Pin만 생성
            UI_BoardItem beforeItem = owner ?? item;
            item.Spawn(id, point, () => {
                CreateConnection(beforeItem, item);
                CanvasGroup canvasGroup = Util.GetOrAddComponent<CanvasGroup>(item.gameObject);
                item.transform.localScale = Vector3.zero;
                canvasGroup.alpha = 0;
                item.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
                canvasGroup.DOFade(1, 0.5f).SetEase(Ease.OutBack);
            });
        }
        //분기 처리
        if (item is UI_AskingCard asking)
        {
            AskingCards.Add(asking);
        }
        else if (item is UI_ClueCard clue)
        {
            clue.Spawn(id, Vector2Int.zero, null);
            BindEvent(clue.gameObject, clue.BeginDragFollow, Define.EUIEvent.PointerDown);
            BindEvent(clue.gameObject, clue.UpdateDragFollow, Define.EUIEvent.Drag);
            BindEvent(clue.gameObject, clue.EndDragFollow, Define.EUIEvent.PointerUp);

            BindEvent(clue.gameObject, ev => HandleDropClue(ev, clue), Define.EUIEvent.PointerUp);
            BindEvent(clue.gameObject, ev => BegineFocusClue(ev, clue), Define.EUIEvent.PointerEnter);
            BindEvent(clue.gameObject, ev => EndFocusClue(ev, clue), Define.EUIEvent.PointerExit);
            HandClues.Add(clue);
        }
        return item;
    }
    private T GetOverlapItem<T>(PointerEventData evData) where T : UI_BoardItem
    {
        List<RaycastResult> casts = new List<RaycastResult>();
        EventSystem.current.RaycastAll(evData, casts);

        foreach (RaycastResult cast in casts)
        {
            T boardItem = cast.gameObject.GetComponentInParent<UI_BoardItem>() as T;
            if (boardItem != null)
            {
                return boardItem;
            }
        }

        return null;
    }
    #endregion
    //첫 실행 시 사용 설명용
    #region Guide
    enum EGuideEventType
    {
        RegisterClue,
        CompareClue,
        CompleteAsking,
        ClickAnyWhere,
    }

    private void InitGuide()
    {
        for (int i = 1; i <= 7; i++)
        {
            var root = Util.FindChild(gameObject, "Guide", true);
            var ui = Util.FindChild(root, $"Step{i}", true);
            //첫번째 제외하고 전부 비활성화
            ui.SetActive(i == 1);
        }
        BindGuide(2, EGuideEventType.ClickAnyWhere, () =>
        {
            BindGuide(3, EGuideEventType.ClickAnyWhere, () =>
            {
                BindGuide(4, EGuideEventType.RegisterClue, () =>
                {
                    BindGuide(5, EGuideEventType.CompareClue, () =>
                    {
                        BindGuide(6, EGuideEventType.ClickAnyWhere, () =>
                        {
                            BindGuide(7, EGuideEventType.ClickAnyWhere, () =>
                            {
                                //마지막 7 끄는 역할
                                BindGuide(8, EGuideEventType.ClickAnyWhere);
                            });
                        });
                    });
                });
            });
        });
    }
    private void BindGuide(int index, EGuideEventType eventType, Action onComplete = null)
    {
        var root = Util.FindChild(gameObject, "Guide", true);
        var target = Util.FindChild(root, $"Step{index}", true);
        var before = Util.FindChild(root, $"Step{index - 1}", true);
        switch (eventType)
        {
            case EGuideEventType.RegisterClue:
                Util.RegisterOnce<int>(h => OnRegistrationClue += h, h => OnRegistrationClue -= h, _ => true, () =>
                {
                    before?.SetActive(false);
                    target?.SetActive(true);
                    onComplete?.Invoke();
                });
                break;
            case EGuideEventType.CompareClue:
                Util.RegisterOnce<int, int, bool>(h => OnCompareClue += h, h => OnCompareClue -= h, (_, _, _) => true, () =>
                {
                    before?.SetActive(false);
                    target?.SetActive(true);
                    onComplete?.Invoke();
                });
                break;
            case EGuideEventType.CompleteAsking:
                Util.RegisterOnce<int, int, bool>(h => OnCompareClue += h, h => OnCompareClue -= h, (_, _, _) => true, () =>
                {
                    before?.SetActive(false);
                    target?.SetActive(true);
                    onComplete?.Invoke();
                });
                break;
            case EGuideEventType.ClickAnyWhere:
                CanvasGroup.blocksRaycasts = false;
                Util.RegisterOnce(h => OnClickAnywhere += h, h => OnClickAnywhere -= h, () =>
                {
                    before?.SetActive(false);
                    target?.SetActive(true);
                    if (OnClickAnywhere == null)
                        CanvasGroup.blocksRaycasts = true;
                    onComplete?.Invoke();
                });
                break;
            default:
                break;
        }
    }
    #endregion
}


