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
    #region Function
    //입력값을 우선순위 및 예외처리에 맞게 기능 실행
    public void HandleInput()
    {
        var player = Managers.Object.Player;
        var actions = KeyInput.GetOnPressActionAll();
        var moveDir = KeyInput.GetInputMoveDir();
        //빈 화면 클릭 시
        if(Input.GetKeyDown(KeyCode.Mouse0) && IsPointerOverClickableUI() == false)
        {
            //맨 처음 등장하는 뉴스 기사, 임시 UI
            if (Managers.UI.IsOpen<UI_TestUI>() == true)
            {
                Managers.UI.ClosePopupUI<UI_TestUI>();
            }
            //아이템 창 닫기
            if (Managers.UI.IsOpen<UI_GetItem>() == true)
            {
                Managers.UI.ClosePopupUI<UI_GetItem>();
            }

        }
        //창 닫기
        if (Managers.UI.AnyOpen() == true)
        {
            //강제로 닫을 수 있는 창일 경우
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
        //인벤토리
        if (KeyInput.IsPress(EUserAction.Inventory))
        {
            Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.Inventory);
        }
        //세팅창 - Esc 기능 중복으로 열람 창 확인
        if (Managers.UI.AnyOpen() == false)
        {
            if (KeyInput.IsPress(EUserAction.Setting))
            {
                Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.Setting);
            }
        }
        //증거함
        if (KeyInput.IsPress(EUserAction.ClueCollection))
        {
            Managers.UI.TogglePopupUI<UI_Binder, EBinderSectionType>(EBinderSectionType.ClueCollection);
        }
        //체크리스트
        if (KeyInput.IsPress(EUserAction.CheckList))
        {
            Managers.UI.TogglePopupUI<UI_CheckList>();
        } 
        // 대화 진행
        //대화 진행 중일 때
        if (Dialogue.IsPlaying == true && Managers.UI.IsOpen<UI_GetItem>() == false)
        {
            //대화 연출 스킵 혹은 다음 대화 진행
            if (KeyInput.IsPress(EUserAction.NextDialog))
            {
                //대화 도중 아이템 획득 시 우선 처리
                if (Managers.UI.IsOpen<UI_GetItem>() == true)
                {
                    //대화 도중 아이템 획득 시 창 닫기
                    Managers.UI.ClosePopupUI<UI_GetItem>();
                }
                else
                {
                    if (Dialogue.HasChoice == true)
                    //연출 스킵
                        Dialogue.TrySkipTyping();
                    else
                    //다음 대화 진행
                        Dialogue.HandleAdvanceInput();
                }
            }
        }
        else
        {
            //사용자 경험 반영 키 기능 처리
            //대화 진행 키로도 일부 UI 닫는 기능 구현
            if (KeyInput.IsPress(EUserAction.NextDialog))
            {
                //아이템 창  닫기
                if (Managers.UI.IsOpen<UI_GetItem>() == true)
                {
                    Managers.UI.ClosePopupUI<UI_GetItem>();
                }
                //체크리스트 닫기
                if (Managers.UI.IsOpen<UI_CheckList>() == true)
                {
                    Managers.UI.ClosePopupUI<UI_CheckList>();
                }
            }
        }
        //상호작용
        //상호작용이 진행 중이지 않으면서 상호작용 물체가 존재할 때
        if (Managers.InteractEffect.CurrentInteractable != null && Managers.InteractEffect.IsDuration == false
            && Dialogue.IsPlaying == false && Managers.UI.IsOpen<UI_GetItem>() == false
            && Managers.Game.SequenceSystem.LocksPlayerMovement == false)
        {
            var interact = Managers.InteractEffect;
            int targetId = interact.CurrentTarget.DataTemplateID;

            //키 홀드 방식의 상호작용
            if (interact.CurrentInteractable.InteractData.IsHoldInteraction == true)
            {
                //홀드가 유지될 때
                if (KeyInput.IsPress(EUserAction.Interaction, true))
                {
                    bool isDone = interact.Interaction.UpdateProgress(Time.deltaTime);
                    if (isDone)
                    {
                        interact.CurrentInteractable.Interact();
                    }
                }
                //홀드가 시작할 때
                else
                {
                    //홀드 진행도 초기화
                    interact.Interaction.Reload();
                }
            }
            //키 인풋 방식의 상호작용
            else
            {
                //즉시 상호작용
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

    //연출, 대화, 청소 등에서 호출 가능한 명령 단위 실행 흐름 
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

    public void TeleportPlayer(Data.TeleportData data, bool immediately = false)
    {
        Player player = Managers.Object.Player;
        if (player == null)
            return;

        EAreaType targetArea = data.AreaType;

        //지역 이동 시작
        IsTeleporting = true;
        player.SetDefaultState();
        //이동 전 페이드인
        Managers.UI.StartFade(0.5f, 0.2f,
                //페이드인 완료 후 콜백
            () =>
            {
                //실제로 지역이 변경되는 구간
                player.transform.position = data.Destination;
                player.RigidBody.position = data.Destination;
                player.LookLeft = data.LookLeft;
                player.LookLeft = data.LookLeft;
                player.SetDefaultState();
                Managers.UI.ShowPopupUI<UI_AreaInfo, EAreaType>(targetArea);
                Managers.Sound.Stop(ESound.Bgm);

            },
                //페이드아웃 완료 후 콜백
            () =>
            {
                //지역 이동 종료
                IsTeleporting = false;
                Managers.InteractEffect.EndInteract();
                ChangeArea(targetArea);
            });

    }
    public void ChangeArea(EAreaType newAreaType)
    {
        CurrentAreaType = newAreaType;
        OnChagedArea?.Invoke(newAreaType);
    }

    #endregion

    //게임 시작 전 초기화
    public void Initialize()
    {
        //지역 이동 시 변경
        OnChagedArea += areaType =>
        {
            CurrentAreaType = areaType;
            Managers.Sound.Play(Define.ESound.Bgm, CurrentArea.BgmName);
        };
        //아이템 및 증거 획득 시 UI 표시
        Inventory.OnGetItem += info => Managers.UI.ShowPopupUI<UI_GetItem, AcquisitionInfo>(info);
        Clues.OnCollectClue += info => Managers.UI.ShowPopupUI<UI_GetClue, AcquisitionInfo>(info);
        Managers.UI.ShowSceneUI<UI_GameScene>();
        //가이드용 1회성 이미지 공간 변경 시 비활성화
        Util.RegisterOnce<EAreaType>(h => OnChagedArea += h, h => OnChagedArea -= h, areaType => true, () =>
        {
            GameObject.Find("ControllGuide").SetActive(false);
        });
    }
}
