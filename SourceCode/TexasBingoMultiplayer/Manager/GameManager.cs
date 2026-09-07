using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;


public class GameManager
{
    public EAreaType CurrentAreaType { get; private set; }
    public Area CurrentArea => Managers.Object.FindArea(CurrentAreaType);
    public bool IsTeleporting { get; private set; }

    private ETimeState _currentTime;
    public ETimeState CurrentTime
    {
        get => _currentTime;
        set
        {
            if (_currentTime != value)
                ApplyTimeState(value);
            _currentTime = value;
        }
    }
    #region System
    private DialogSystem _dialogue;
    public DialogSystem Dialogue
    {
        get
        {
            if (_dialogue == null)
                _dialogue = new DialogSystem();
            return _dialogue;
        }
    }

    private KeyInputSystem _keyInput;
    public KeyInputSystem KeyInput
    {
        get
        {
            if (_keyInput == null)
                _keyInput = new KeyInputSystem();
            return _keyInput;
        }
    }

    private InventorySystem _inventory;
    public InventorySystem Inventory
    {
        get
        {
            if (_inventory == null)
                _inventory = new InventorySystem();
            return _inventory;
        }
    }

    private TaskSystem _taskSystem;
    public TaskSystem TaskSystem
    {
        get
        {
            if (_taskSystem == null)
                _taskSystem = new TaskSystem();
            return _taskSystem;
        }
    }

    private DaySystem _daySystem;
    public DaySystem DaySystem
    {
        get
        {
            if (_daySystem == null)
                _daySystem = new DaySystem();
            return _daySystem;
        }
    }
    private ClueSystem _clues;
    public ClueSystem Clues
    {
        get
        {
            if (_clues == null)
                _clues = new ClueSystem();
            return _clues;
        }
    }
    private SequenceSystem _sequenceSystem;
    public SequenceSystem SequenceSystem
    {
        get
        {
            if (_sequenceSystem == null)
                _sequenceSystem = new SequenceSystem();
            return _sequenceSystem;
        }
    }
    #endregion
    #region Event
    public event Action<EAreaType> OnChagedArea;
    public void Test_TriggerAreaChange(EAreaType areaType)
    {
        OnChagedArea?.Invoke(areaType);
    }
    public void BindOnChangedDay(int targetDay, Action action)
    {
        Func<int, bool> condition = day => targetDay == day;
        Util.RegisterOnce(
            h => Managers.Game.DaySystem.OnDayChanged += h,
            h => Managers.Game.DaySystem.OnDayChanged -= h,
            condition, action);
    }

    public void BindOnChagedArea(EAreaType targetAreaType, Action action)
    {
        Func<EAreaType, bool> condition = type => targetAreaType == type;
        Util.RegisterOnce(h => OnChagedArea += h,
            h => OnChagedArea -= h,
            condition, action);
    }
    public void BindOnBegineInteract(EInteractType targetInteractType, int targetObjectId, Action action)
    {
        Func<EInteractType, int, bool> condition = (type, index) =>
        (type == targetInteractType) && index == targetObjectId;
        Util.RegisterOnce(h => Managers.InteractEffect.OnBegineInteract += h,
            h => Managers.InteractEffect.OnBegineInteract -= h,
            condition, action);
    }
    public void BindOnEndInteract(EInteractType targetInteractType, int targetObjectId, Action action)
    {
        Func<EInteractType, int, bool> condition = (type, index) =>
        (type == targetInteractType) && index == targetObjectId;
        Util.RegisterOnce(h => Managers.InteractEffect.OnEndInteract += h,
            h => Managers.InteractEffect.OnEndInteract -= h,
            condition, action);
    }
    public void BindOnEnterCamera(int targetId, Action action)
    {
        BaseObject targetObj = Managers.Object.FindCharacter<Npc>(targetId);

        if (targetObj == null)
        {
            targetObj = Managers.Object.FindWorldObject(targetId);
        }
        CameraFollowController camController = Camera.main.GetComponent<CameraFollowController>();
        camController.TryAddFindTarget(targetObj, action);
    }
    public void BindOnMissionFinish(int targetTaskId, Action action)
    {
        Func<int, bool> condition = id => targetTaskId == id;
        Util.RegisterOnce(
            h => Managers.Game.TaskSystem.OnFinishTask += h,
            h => Managers.Game.TaskSystem.OnFinishTask -= h,
            condition, action);
    }
    public void BindOnAllTaskFinish(Action action)
    {
        void TryExecuteAction()
        {
            if (Managers.Game.Dialogue.IsPlaying || Managers.UI.AnyOpen())
            {
                DG.Tweening.DOVirtual.DelayedCall(0.5f, TryExecuteAction);
                return;
            }
            action?.Invoke();
        }

        Util.RegisterOnce(
            h => Managers.Game.TaskSystem.OnFinshAllTake += h,
            h => Managers.Game.TaskSystem.OnFinshAllTake -= h,
            TryExecuteAction
        );
    }
    #endregion

    #region Function
    public void HandleInput()
    {
        var player = Managers.Object.Player;
        var actions = KeyInput.GetOnPressActionAll();
        var moveDir = KeyInput.GetInputMoveDir();
        //화면 클릭
        if(Input.GetKeyDown(KeyCode.Mouse0) && IsPointerOverClickableUI() == false)
        {
            if (Managers.UI.IsOpen<UI_TestUI>() == true)
            {
                Managers.UI.ClosePopupUI<UI_TestUI>();
            }
            if (Managers.UI.IsOpen<UI_GetItem>() == true)
            {
                Managers.UI.ClosePopupUI<UI_GetItem>();
            }

        }
        //창 닫기
        if (Managers.UI.AnyOpen() == true)
        {
            if (KeyInput.IsPress(EUserAction.Cancle) && Managers.UI.CurPopup.AffectedCancel == true)
            {
                Managers.UI.ClosePopupUI();
            }
        }
        //아이템 획득 창일 경우 일부 예외 처리
        if (Managers.UI.IsOpen<UI_GetItem>() == true)
        {
            if (KeyInput.IsPress(EUserAction.Interaction) || KeyInput.IsPress(EUserAction.NextDialog))
            {
                Managers.UI.ClosePopupUI<UI_GetItem>();
            }
        }
        //창 열기
        if (KeyInput.IsPress(EUserAction.Inventory))
        {
            Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.Inventory);
        }
        if (Managers.UI.AnyOpen() == false)
        {
            if (KeyInput.IsPress(EUserAction.Setting))
            {
                Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.Setting);
            }
        }
        if (KeyInput.IsPress(EUserAction.ClueCollection))
        {
            Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.ClueCollection);
        }
        if (KeyInput.IsPress(EUserAction.CheckList))
        {
            Managers.UI.TogglePopupUI<UI_CheckList>();
        } 
        // 1. 조작 (대화 진행)
        if (Dialogue.IsPlaying == true && Managers.UI.IsOpen<UI_GetItem>() == false)
        {
            if (KeyInput.IsPress(EUserAction.NextDialog))
            {
                //대화 도중 아이템 획득 시 우선 처리
                if (Managers.UI.IsOpen<UI_GetItem>() == true)
                {
                    Managers.UI.ClosePopupUI<UI_GetItem>();
                }
                else
                {
                    if (Dialogue.HasChoice == true)
                        Dialogue.TrySkipTyping();
                    else
                        Dialogue.HandleAdvanceInput();
                }
            }
        }
        else
        {
            //사용자 경험 반영 키 기능 처리
            if (KeyInput.IsPress(EUserAction.NextDialog))
            {
                if (Managers.UI.IsOpen<UI_GetItem>() == true)
                {
                    Managers.UI.ClosePopupUI<UI_GetItem>();
                }
                if (Managers.UI.IsOpen<UI_CheckList>() == true)
                {
                    Managers.UI.ClosePopupUI<UI_CheckList>();
                }
            }
        }
        // 2. 상호작용 (대화 중이 아닐 때만 실행)
        if (Managers.InteractEffect.CurrentInteractable != null && Managers.InteractEffect.IsDuration == false
            && Dialogue.IsPlaying == false && Managers.UI.IsOpen<UI_GetItem>() == false
            && Managers.Game.SequenceSystem.LocksPlayerMovement == false)
        {
            var interact = Managers.InteractEffect;
            int targetId = interact.CurrentTarget.DataTemplateID;

            if (interact.CurrentInteractable.InteractData.IsHoldInteraction == true)
            {
                if (KeyInput.IsPress(EUserAction.Interaction, true))
                {
                    bool isDone = interact.Interaction.UpdateProgress(Time.deltaTime);
                    if (isDone)
                    {
                        interact.CurrentInteractable.Interact();
                    }
                }
                else
                {
                    interact.Interaction.Reload();
                }
            }
            else
            {
                if (KeyInput.IsPress(EUserAction.Interaction))
                {
                    interact.CurrentInteractable.Interact();
                }
            }
        }
        //플레이어 이동 
        if (moveDir != Vector2.zero && player.CanMove && Managers.UI.IsOpen<UI_GetItem>() == false && !IsTeleporting && !Managers.UI.AnyOpen())
        {
            bool isSprinting = KeyInput.IsPress(EUserAction.Sprint, true);
            player.SetMovementInfo(moveDir, isSprinting);
        }
        else
        {
            player.SetMovementInfo(Vector2.zero, false);
        }
    }

    public void ExecuteCommand(ActionCommand command, EAcquireSource source = EAcquireSource.None)
    {
        Debug.Log($"{command.Type}실행, 호출:{source}");
        switch (command.Type)
        {
            case EActionCommandType.GetClue:
                Clues.TryGetClue(command.TargetId, source);
                break;
            case EActionCommandType.GetItem:
                Inventory.AddItem(command.TargetId, command.IntValue, source);
                break;
            case EActionCommandType.ChageAnimation:
                var target = Managers.Object.FindCharacter(command.TargetId);
                if (target != null)
                    target.CharacterState = Util.ParseEnum<ECharacterState>(command.StringValue);
                break;
            case EActionCommandType.None:
                throw new Exception($"실행하려는 명령의 호출부가 입력되지 않았습니다");
            default:
                break;
        }
    }

    public void ExecuteCommands(IEnumerable<ActionCommand> commands, EAcquireSource source = EAcquireSource.None)
    {
        if (commands == null)
            return;
        foreach (var command in commands)
            ExecuteCommand(command, source);
    }

    public void ChangeArea(EAreaType newAreaType)
    {
        CurrentAreaType = newAreaType;
        OnChagedArea?.Invoke(newAreaType);
    }

    public void TeleportPlayer(Data.TeleportData data, bool immediately = false)
    {
        Player player = Managers.Object.Player;
        if (player == null)
            return;

        EAreaType targetArea = data.AreaType;

        if (targetArea == EAreaType.Road_Day || targetArea == EAreaType.Road_Night)
        {
            if (CurrentTime == Define.ETimeState.Night)
                targetArea = EAreaType.Road_Night;
            else
                targetArea = EAreaType.Road_Day;
        }

        IsTeleporting = true;
        player.SetDefaultState();
        Managers.UI.StartFade(0.5f, 0.2f,
            () =>
            {
                player.transform.position = data.Destination;
                player.RigidBody.position = data.Destination;
                player.LookLeft = data.LookLeft;
                player.LookLeft = data.LookLeft;
                player.SetDefaultState();
                Managers.UI.ShowPopupUI<UI_AreaInfo, EAreaType>(targetArea);
                Managers.Sound.Stop(ESound.Bgm);

            },
            () =>
            {
                IsTeleporting = false;
                Managers.InteractEffect.EndInteract();
                ChangeArea(targetArea);
            });

    }

    public void ApplyTimeState(ETimeState timeState)
    {
        var roadDayArea = Managers.Object.Areas.FirstOrDefault(a => a.gameObject.name == "Road_Day");
        var roadNightArea = Managers.Object.Areas.FirstOrDefault(a => a.gameObject.name == "Road_Night");

        var dayWindow = Managers.Object.WorldObjects.FirstOrDefault(a => a.gameObject.name == "Home_Window_A.M_0");
        var nightWindow = Managers.Object.WorldObjects.FirstOrDefault(a => a.gameObject.name == "Home_Window_P.M_0");

        if (timeState == ETimeState.Day)
        {
            if (roadDayArea != null) roadDayArea.gameObject.SetActive(true);
            if (roadNightArea != null) roadNightArea.gameObject.SetActive(false);
            if (dayWindow != null) dayWindow.gameObject.SetActive(true);
            if (nightWindow != null) nightWindow.gameObject.SetActive(false);
        }
        else // Night
        {
            if (roadDayArea != null) roadDayArea.gameObject.SetActive(false);
            if (roadNightArea != null) roadNightArea.gameObject.SetActive(true);
            if (dayWindow != null) dayWindow.gameObject.SetActive(false);
            if (nightWindow != null) nightWindow.gameObject.SetActive(true);
        }
    }
    #endregion
    public void Initialize()
    {
        Managers.Data.Init();
        Managers.Object.InitPreSpawnedObjects();
        OnChagedArea += areaType =>
        {
            CurrentAreaType = areaType;
            Managers.Sound.Play(Define.ESound.Bgm, CurrentArea.BgmName);
        };
        Inventory.OnGetItem += info => Managers.UI.ShowPopupUI<UI_GetItem, AcquisitionInfo>(info);
        Clues.OnCollectClue += info => Managers.UI.ShowPopupUI<UI_GetClue, AcquisitionInfo>(info);
        Dialogue.OnFinishedDialogue += (dialogId, choiceIndex) =>
        {
            var interact = Managers.InteractEffect;

            // 1. 일반적인 '대화(Dia)' 상호작용 중이었다면, 대사가 끝나는 이 시점에 정확히 EndInteract 호출
            if (interact.CurrentInteract != null && interact.CurrentInteract.InteractType == EInteractType.Dialogue)
            {
                interact.EndInteract();
            }
            // 2. 강제 상호작용 실패로 뜬 '거절 대사'가 끝났을 때, 플레이어가 아직 오브젝트 앞이라면 UI 복구
            else if (interact.CurrentTarget != null && interact.CurrentInteractable != null)
            {
                if (interact.Interaction != null)
                {
                    interact.Interaction.Show(interact.CurrentInteractable.InteractData);
                }
            }
        };
        Managers.Game.DaySystem.Init();
        Managers.UI.ShowSceneUI<UI_GameScene>();
        //튜토리얼용 1회성 이미지 비활성화
        Util.RegisterOnce<EAreaType>(h => OnChagedArea += h, h => OnChagedArea -= h, areaType => true, () =>
        {
            GameObject.Find("ControllGuide").SetActive(false);
        });
        Debug.Log("GameManager Initialized");
    }

    private readonly List<RaycastResult> _uiRaycastResults = new();
    private bool IsPointerOverClickableUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _uiRaycastResults);

        foreach (var result in _uiRaycastResults)
        {
            GameObject go = result.gameObject;

            if (go.GetComponentInParent<UnityEngine.UI.Button>() != null)
                return true;

            if (go.GetComponentInParent<IPointerClickHandler>() != null)
                return true;
        }

        return false;
    }
}
