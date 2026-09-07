using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//바인더를 이루는 색션 종류 - 증가 가능
public enum EBinderSectionType
{
    None = -1,
    Front,
    Inventory,
    ClueCollection,
    Setting,
    Back,
}
//한 페이지를 이루는 섹션의 추상 클래스
public abstract class UI_BinderSection
{
    public UI_Binder Owner {  get; protected set; }
    public abstract EBinderSectionType State { get; }

    //왼쪽, 오른쪽 구분 기반 초기화 
    public abstract void InitSection(RectTransform left, RectTransform right);
    // 해당 색션이 펼쳐질 때 호출(넘어가는 연출 도중에도 호출됨)
    public abstract void OnSectionOpen(); 
}

public class UI_Binder : UI_Popup, IInitializablePopup<EBinderSectionType>
{
    #region Enums and Classes
    enum Images
    {
        Section_1,
        Section_2,
        Section_3,
        Section_4,
    }
    enum Objects
    {
        Left,
        Right,
    }
    enum Direction
    {
        Left = -1,
        Right = 1,
    }

    //UI_Binder가 색션을 관리하는 단위 클래스 - 실제 객체 기반
    class Section
    {
        public Image Panel { get; private set; }
        public RectTransform Left { get; private set; }
        public EBinderSectionType LState { get; private set; }
        public RectTransform Right { get; private set; }
        public EBinderSectionType RState { get; private set; }

        public Section(Image panel, EBinderSectionType leftState, EBinderSectionType rightState)
        {
            if (leftState == EBinderSectionType.None || rightState == EBinderSectionType.None)
                throw new System.Exception($"Not Valid Section State. L:{leftState}, R:{rightState}");
            Panel = panel;
            Right = Util.FindChild(Panel.gameObject, "Area_R").transform as RectTransform;
            Right.gameObject.SetActive(false);
            LState = leftState;
            Left = Util.FindChild(Panel.gameObject, "Area_L").transform as RectTransform;
            Left.gameObject.SetActive(false);
            RState = rightState;
        }

        public bool SetActiveAny(EBinderSectionType targetState, bool active)
        {
            if (SetActiveLeft(targetState, active) == true)
                return true;
            else if (SetActiveRight(targetState, active) == true)
                return true;

            return false;
        }

        public bool SetActiveLeft(EBinderSectionType targetState, bool active)
        {
            if (LState == targetState)
            {
                Left.gameObject.SetActive(active);
                return true;
            }
            return false;
        }

        public bool SetActiveRight(EBinderSectionType targetState, bool active)
        {
            if (RState == targetState)
            {
                Right.gameObject.SetActive(active);
                return true;
            }
            return false;
        }
    }
    #endregion

    //색션 집합
    private Dictionary<EBinderSectionType, UI_BinderSection> BinderSections = new();

    private Section[] Sections { get; set; }
    private Transform Left { get; set; }
    private Transform Right { get; set; }
    private EBinderSectionType _currentState { get; set; }
    private EBinderSectionType CurrentState
    {
        get
        {
            return _currentState;
        }
        set
        {
            //새 색션이 들어오면 현재 색션에서 다른 색션으로 변경되는 로직
            if (_currentState == value)
                return;
            int prevIndex = (int)_currentState;
            _currentState = value;
            int newIndex = (int)_currentState;
            OnStateChaged?.Invoke(prevIndex, newIndex);
        }
    }
    private EBinderSectionType _targetState;
    public event Action<int, int> OnStateChaged;    //색션이 변경됐을 때(beforeState, targetState)
    private int _turnTaskCount;
    public int TurnTaskCount
    {
        get
        {
            return _turnTaskCount;
        }
        set
        {
            _turnTaskCount = Mathf.Max(0, value);
            if (_turnTaskCount == 0)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _turnTaskDir = 0;
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
    private int _turnTaskDir;
    CanvasGroup _canvasGroup;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        {
            _canvasGroup = Util.GetOrAddComponent<CanvasGroup>(gameObject);

            BindImages(typeof(Images));
            BindObjects(typeof(Objects));

            Left = GetObject((int)Objects.Left).transform;
            Right = GetObject((int)Objects.Right).transform;
            _targetState = EBinderSectionType.None;

            Sections = new Section[4];
            Sections[0] = new Section(GetImage((int)Images.Section_1), EBinderSectionType.Inventory, EBinderSectionType.Front);
            Sections[1] = new Section(GetImage((int)Images.Section_2), EBinderSectionType.ClueCollection, EBinderSectionType.Inventory);
            Sections[2] = new Section(GetImage((int)Images.Section_3), EBinderSectionType.Setting, EBinderSectionType.ClueCollection);
            Sections[3] = new Section(GetImage((int)Images.Section_4), EBinderSectionType.Back, EBinderSectionType.Setting);
        }
        {
            Transform indexsRoot = Util.FindChild(gameObject, "SectionIndexs", true).transform;
            int childCount = indexsRoot.childCount;

            _indexImages = new Image[childCount];
            _indexTexts = new TMP_Text[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = indexsRoot.GetChild(i);
                _indexImages[i] = child.GetComponent<Image>();
                // 텍스트가 자식의 자식에 있다면 GetComponentInChildren 사용
                _indexTexts[i] = child.GetComponentInChildren<TMP_Text>();
            }
            OnStateChaged += UpdateTurnAnimation;
            OnStateChaged += UpdateIndexAnimation;
        }

        InitBinderSection();
        return true;
    }

    //UIManager로 열람 시 필수로 초기화해야하는 UI의 인터페이스 메서드
    public void Initialize(EBinderSectionType state)
    {
        _targetState = state;
    }
    #region Open/Close
    Vector2 apearStartPoint = new Vector2(1200, -200);
    Vector2 apearEndPoint = new Vector2(-379, -50);
    public override bool OpenPopupUI()
    {
        _currentState = EBinderSectionType.Front;
        for(int i = 0; i < _indexImages.Length; i++)
        {
            _indexImages[i].color = Color.gray;
            _indexImages[i].transform.localScale = Vector3.one;
            _indexTexts[i].color = Color.gray;
        }
        foreach (var section in Sections)
        {
            section.Panel.transform.SetParent(Right);
            section.Panel.transform.rotation = Quaternion.identity;
            section.Panel.transform.SetSiblingIndex(0);
            section.Left.gameObject.SetActive(false);
            section.Right.gameObject.SetActive(false);
        }
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        if (base.OpenPopupUI() == false)
            return false;
        return true;
    }
    protected override Sequence CreateOpenSequence()
    {
        var openSequence = base.CreateOpenSequence();
        var file = Util.FindChild(gameObject, "File").transform as RectTransform;
        openSequence.onPlay += () =>
        {
            file.localPosition = apearStartPoint;
            Sections.FirstOrDefault(s => s.RState == EBinderSectionType.Front).SetActiveRight(EBinderSectionType.Front, true);
            file.rotation = Quaternion.Euler(Vector3.forward * 270);
            Managers.Sound.Play(Define.ESound.Effect, "Effect_CaseFile");
        };
        openSequence.Append(file.DOAnchorPos(apearEndPoint, 0.25f).SetEase(Ease.OutSine))
        .Join(file.DORotate(Vector3.forward * 360, 0.25f, RotateMode.FastBeyond360))
        .AppendCallback(() =>
        {
            if (ChackTurnStep(Direction.Right, true) == true)
            {
                CurrentState = _targetState == EBinderSectionType.None ? EBinderSectionType.Inventory : _targetState;
                _targetState = EBinderSectionType.None;
            }
        })
        .Insert(0.3f, file.DOAnchorPosX(0, 0.4f).SetEase(Ease.OutSine).Play());


        return openSequence;
    }
    protected override Sequence CreateCloseSequence()
    {
        transform.DOKill();
        var closeSequence = base.CreateCloseSequence();
        var file = Util.FindChild(gameObject, "File").transform as RectTransform;
        int tempState = (int)CurrentState;
        int targetState = (int)EBinderSectionType.Front;
        int stepCount = 0;
        Managers.Sound.Play(Define.ESound.Effect, "Effect_CaseFile");

        //인덱스 초기화 - 고려
        UpdateIndexAnimation(tempState, 0);
        while (tempState > targetState)
        {
            int currentIndex = tempState;
            closeSequence.Insert(stepCount * 0.15f, TurnToPrev(currentIndex - 1));

            tempState--;
            stepCount++;
        }

        float fileMoveDelay = (stepCount > 0) ? (stepCount * 0.15f + 0.5f) : 0f;
        closeSequence.Insert(fileMoveDelay, file.DOAnchorPos(apearStartPoint, 0.2f).SetEase(Ease.OutSine));

        closeSequence.OnComplete(() =>
        {
            _currentState = EBinderSectionType.Front;
            TurnTaskCount = 0;
            _turnTaskDir = 0;
            gameObject.SetActive(false);
        });

        return closeSequence;
    }
    #endregion
    #region Support Methods
    private void InitBinderSection()
    {
        UI_Binder_Inventory inventory = new UI_Binder_Inventory(this);
        BinderSections.Add(inventory.State, inventory);
        UI_Binder_ClueCollection clueCollection = new UI_Binder_ClueCollection(this);
        BinderSections.Add(clueCollection.State, clueCollection);
        UI_Binder_Setting setting = new UI_Binder_Setting(this);
        BinderSections.Add(setting.State, setting);


        foreach (var section in BinderSections.Values)
        {
            var leftSection = Sections.FirstOrDefault(s => s.LState == section.State);
            var rightSection = Sections.FirstOrDefault(s => s.RState == section.State);

            section.InitSection(leftSection.Left, rightSection.Right);
        }
    }

    private bool IsValidSectionIndex(int index)
    {
        return Sections != null && index >= 0 && index <= Sections.Length;
    }
    private bool ChackTurnStep(Direction moveDirction, bool enableCover = false)
    {
        int newDir = moveDirction == Direction.Right ? 1 : -1;
        int currentIndex = (int)CurrentState;
        int prevIndex = currentIndex + newDir;
        if (IsValidSectionIndex(prevIndex) == false)
            return false;
        if (enableCover == false && (prevIndex == 0 || prevIndex == Sections.Length))
            return false;

        //변동이 없다면
        if (newDir == 0)
            return false;
        //진행과 다른 방향이라면
        if (_turnTaskDir != 0 && _turnTaskDir != newDir)
            return false;
        _turnTaskDir = newDir;
        return true;
    }
    private EBinderSectionType TryParseEBinderSectionType(int sectionIndex)
    {
        if (IsValidSectionIndex(sectionIndex) == false)
            return EBinderSectionType.None;
        return (EBinderSectionType)sectionIndex;
    }
    #endregion
    #region OnChagedState
    private Image[] _indexImages;
    private TMP_Text[] _indexTexts;
    public void UpdateIndexAnimation(int prevIndex, int currIndex)
    {
        if (prevIndex > 0 && prevIndex < _indexImages.Length)
        {
            Image prevIndexImage = _indexImages[prevIndex];
            TMP_Text prevIndexText = _indexTexts[prevIndex];
            prevIndexText.DOColor(Color.gray, 0.3f);
            prevIndexImage.DOColor(Color.gray, 0.3f);
            prevIndexImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutSine);
        }

        if (currIndex > 0 && currIndex < _indexImages.Length)
        {
            Image currIndexImage = _indexImages[currIndex];
            TMP_Text currIndexText = _indexTexts[currIndex];
            currIndexText.DOColor(Color.white, 0.3f);
            currIndexImage.DOColor(Color.white, 0.3f);
            currIndexImage.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutSine);
        }
    }

    private void UpdateTurnAnimation(int prevIndex, int currentIndex)
    {
        Sequence seq = DOTween.Sequence();
        seq.OnPlay(() => {
            Managers.Sound.Play(Define.ESound.Effect, "Effect_CaseFileFlap");
            TurnTaskCount++;
        });
        seq.OnComplete(() => TurnTaskCount--);
        if (_turnTaskDir > 0)
        {
            int count = 0;
            for (int i = prevIndex; i < currentIndex; i++)
            {
                seq.Insert(count * 0.2f, TurnToNext(i));
                count++;
            }
        }
        else
        {
            int count = 0;
            for (int i = prevIndex; i > currentIndex; i--)
            {
                seq.Insert(count * 0.2f, TurnToPrev(i - 1));
                count++;
            }
        }
        //Front만 담김. 이외는 안 담김. 수정 필요
        seq.SetTarget(transform);
        seq.Play();
    }

    public Sequence TurnToNext(int index)
    {
        if (IsValidSectionIndex(index) == false)
            return null;

        EBinderSectionType beforestate = TryParseEBinderSectionType(index);
        EBinderSectionType newState = TryParseEBinderSectionType(index + 1);

        Section prevSection = null;
        Section nextSection = null;
        if (IsValidSectionIndex(index - 1))
            prevSection = Sections[index - 1];
        if (IsValidSectionIndex(index + 1))
            nextSection = Sections[index + 1];

        Section targetSection = Sections[index];

        return DOTween.Sequence()
            .OnPlay(() =>
            {
                nextSection?.SetActiveRight(newState, true);
                if(BinderSections.ContainsKey(newState) == true)
                    BinderSections[newState]?.OnSectionOpen();
            })
            .Append(targetSection.Panel.transform.DORotate(Vector3.up * -90, 0.4f).SetEase(Ease.Linear))
            .AppendCallback(() =>
            {
                targetSection.Panel.transform.SetParent(Left);
                targetSection.Panel.transform.SetSiblingIndex(Left.childCount - 1);
                targetSection.SetActiveRight(beforestate, false);
                targetSection.SetActiveLeft(newState, true);
            })
            .Append(targetSection.Panel.transform.DORotate(Vector3.up * -180, 0.3f).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                prevSection?.SetActiveLeft(beforestate, false);
            });
    }
    public Sequence TurnToPrev(int index)
    {
        if (IsValidSectionIndex(index) == false)
            return null;

        EBinderSectionType beforestate = TryParseEBinderSectionType(index + 1);
        EBinderSectionType newState = TryParseEBinderSectionType(index);

        Section prevSection = null;
        Section nextSection = null;

        if (IsValidSectionIndex(index - 1))
            prevSection = Sections[index - 1];
        if (IsValidSectionIndex(index + 1))
            nextSection = Sections[index + 1];

        Section targetSection = Sections[index];

        return DOTween.Sequence()
            .OnPlay(() =>
            {
                prevSection?.SetActiveLeft(newState, true); 
                if (BinderSections.ContainsKey(newState) == true)
                    BinderSections[newState]?.OnSectionOpen();
            })
            .Append(targetSection.Panel.transform.DORotate(Vector3.up * -90, 0.4f).SetEase(Ease.Linear))
            .AppendCallback(() =>
            {
                targetSection.Panel.transform.SetParent(Right);
                targetSection.Panel.transform.SetSiblingIndex(Right.childCount - 1);

                targetSection.SetActiveLeft(beforestate, false);
                targetSection.SetActiveRight(newState, true);
            })
            .Append(targetSection.Panel.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                nextSection?.SetActiveRight(beforestate, false);
            });
    }
    #endregion
    #region TestCode
    //바인더UI가 열려있을 때 Q,E로 옆 색션으로 넘어가는 기능
    public bool TryNextSection()
    {
        if (ChackTurnStep(Direction.Right) == true)
        {
            CurrentState++;
            return true;
        }
        return false;
    }
    public bool TryPrevSection()
    {
        if (ChackTurnStep(Direction.Left) == true)
        {
            CurrentState--;
            return true;
        }
        return false;
    }
    private void Update()
    {
        if(IsClosing == true)
            return;
        if (Input.GetKeyDown(KeyCode.E))
            TryNextSection();
        if (Input.GetKeyDown(KeyCode.Q))
            TryPrevSection();
    }
    #endregion
}
