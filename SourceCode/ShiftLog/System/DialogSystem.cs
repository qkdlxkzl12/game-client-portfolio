using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EDialogEventType
{
    Random,             //랜덤의 단일 대화 출력
    Interactive,        //주고 받는 다중 대사
    Monologue,          //홀로 실행되는 단일 대사
    SequenceLoop,       //자동 반복하는 대사
    UnallowedInteract   //시퀀스의 인터페이스 제한 상황의 시스템 메세지
}

public class DialogSystem
{
    private UI_SpeechBubble _currentSpeechBubble;

    //진행되는 대화 ID
    private int _currentDialogueId;
    private int _currentLineIndex;
    private int _selectedChoiceIndex = -1;
    private Queue<Data.Dialogue> _selectedDialogue = new();

    public bool IsPlaying { get; private set; }

    public Action<int, int> OnFinishedDialogue;
    private Data.DialogData CurrentData
    {
        get
        {
            if (_currentDialogueId == 0)
                return null;
            return Managers.Data.DialogDic[_currentDialogueId];
        }
    }
    private Data.Dialogue CurrentDialogue
    {
        get
        {
            if (CurrentData == null || _currentLineIndex < 0 || _currentLineIndex >= CurrentData.Dialogues.Length)
                return null;

            //선택지 대화가 진행중이라면 선택지 대화 우선
            if (_selectedDialogue != null && _selectedDialogue.Count != 0)   
                return _selectedDialogue.Peek();
            else
                return CurrentData.Dialogues[_currentLineIndex];
        }
    }

    public bool HasChoice => _selectedChoiceIndex == -1 && CurrentDialogue != null && CurrentDialogue.Choices != null && CurrentDialogue.Choices.Length > 0;

    public void StartDialog(int dialogId, Action onFinished = null, int choiceIndex = -1)
    {
        // 이미 대화가 실행 중이라면 방어
        if (IsPlaying == true)
        {
            Debug.LogWarning("[DialogSystem] 이미 대화가 진행 중입니다.");
            return;
        }

        //대화 완료 콜백 함수 초기화
        if (onFinished != null)
        {
            Func<int, int, bool> condition = (id, choice) => (id == dialogId) && (choiceIndex == -1 || choiceIndex == choice);
            Util.RegisterOnce<int, int>(h => OnFinishedDialogue += h,
                h => OnFinishedDialogue -= h,
                condition,
                onFinished
                );
        }

        //대화 타입에 따른 기능 실행
        var data = Managers.Data.DialogDic[dialogId];
        switch (data.EventType)
        {
            case EDialogEventType.Random:
                PlayRandom(data);
                break;
            case EDialogEventType.Interactive:
                PlayInteractive(data);
                break;
            case EDialogEventType.Monologue:
                PlayMonologue(data);
                break;
            case EDialogEventType.SequenceLoop:
                PlaySequenceLoop(data);
                break;
            case EDialogEventType.UnallowedInteract:
                //시스템적 메세지도 혼잣말 형태로 전달
                PlayMonologue(data);
                break;
            default:
                Debug.LogError($"[DialogSystem] 알 수 없는 대화 이벤트 타입입니다: {data.EventType}");
                return;
        }
    }

    #region Advance
    public void HandleAdvanceInput()
    {
        if (TrySkipTyping())
            return;

        AdvanceToNextLine();
    }
    public bool TrySkipTyping()
    {
        if (_currentSpeechBubble == null || !_currentSpeechBubble.IsTyping)
            return false;

        _currentSpeechBubble.SkipTyping();
        return true;
    }
    //인터렉티브 타입 전용
    private void AdvanceToNextLine()
    {
        // 방금 화면에 출력됐던 대사의 종료 커맨드 실행
        if (_currentShowingDialogue != null)
        {
            Managers.Game.ExecuteCommands(_currentShowingDialogue.EndCommands,Define.EAcquireSource.Dialogue);
        }

        var next = GetNextDialogue();

        if (next == null)
        {
            EndDialog();
            return;
        }

        StopSequenceLoop(next.SpeakerId);

        var character = Managers.Object.FindCharacter(next.SpeakerId);

        if (character == null || character.SpeechBubble == null)
        {
            Debug.LogError($"[DialogSystem] 화자 ID {next.SpeakerId} 캐릭터를 찾을 수 없거나 말풍선이 없습니다.");
            EndDialog();
            return;
        }

        var targetSpeechBubble = character.SpeechBubble;

        if (targetSpeechBubble != _currentSpeechBubble)
            _currentSpeechBubble?.Hide();

        targetSpeechBubble.Show(next.Conversation, CurrentData.EventType);
        _currentSpeechBubble = targetSpeechBubble;

        // 이제 이 대사가 현재 출력 중인 대사
        _currentShowingDialogue = next;

        Managers.Game.ExecuteCommands(
            _currentShowingDialogue.StartCommands,
            Define.EAcquireSource.Dialogue
        );

        if (next.Choices != null)
        {
            _selectedChoiceIndex = -1;
            Util.RegisterOnce(
                h => targetSpeechBubble.OnTypingCompleted += h,
                h => targetSpeechBubble.OnTypingCompleted -= h,
                () => Managers.UI.ShowPopupUI<UI_SelectChoice, Data.Choice[]>(next.Choices)
            );
        }
    }
    private Data.Dialogue GetNextDialogue()
    {
        // 선택지 대화가 진행 중이면 선택지 큐를 우선 소비
        if (_selectedDialogue.Count > 0)
        {
            var dialogue = _selectedDialogue.Dequeue();
            
            return dialogue;
        }

        // 일반 대화 진행
        if (CurrentData == null || TryGetDialog(++_currentLineIndex, out Data.Dialogue nextDialogue) == false)
            return null;

        return nextDialogue;
    }
    private bool TryGetDialog(int index, out Data.Dialogue dialogue)
    {
        dialogue = null;
        
        if (CurrentData.Dialogues == null || index < 0 || index >= CurrentData.Dialogues.Length)
            return false;

        dialogue = CurrentData.Dialogues[index];
        return true;
    }
    public void EndDialog()
    {
        int finishedDialogId = _currentDialogueId;

        _currentSpeechBubble?.Hide();
        _currentSpeechBubble = null;
        _currentDialogueId = 0;
        _selectedChoiceIndex = -1;
        _selectedDialogue.Clear();
        // EndCommands가 두번 호출되는 버그를 막기위해 추가
        _currentShowingDialogue = null;
        IsPlaying = false;

        OnFinishedDialogue?.Invoke(finishedDialogId, _selectedChoiceIndex);
    }
    #endregion
    #region PlayDialogue
    private void PlayRandom(Data.DialogData data)
    {
        if (data.Dialogues == null || data.Dialogues.Length == 0) 
            return;
        if (Managers.InteractEffect.CurrentTarget == null) 
            return;


        var target = Managers.Object.FindCharacter(Managers.InteractEffect.CurrentTarget.DataTemplateID);
        if (target == null) return;

        StopSequenceLoop(target.DataTemplateID);

        _currentDialogueId = data.TemplateId;
        _currentSpeechBubble = target.SpeechBubble;
        IsPlaying = true;

        Sequence seq = DOTween.Sequence();
        var randomDialog = data.Dialogues[UnityEngine.Random.Range(0, data.Dialogues.Length)];
        float typingTime = randomDialog.Conversation.Length * UI_SpeechBubble.CharTypingDuration;
        float waitTime = 2f;

        seq.AppendCallback(() => target.SpeechBubble.Show(randomDialog.Conversation, data.EventType));
        seq.AppendInterval(typingTime + waitTime);
        seq.AppendCallback(() => {
            target.SpeechBubble.Hide();
            EndDialog();
        });

        if (DialogSequences.ContainsKey(target))
            DialogSequences[target] = seq;
        else
            DialogSequences.Add(target, seq);
    }
    private void PlayInteractive(Data.DialogData data)
    {
        _currentDialogueId = data.TemplateId;
        _currentLineIndex = -1;
        IsPlaying = true;

        AdvanceToNextLine();
    }
    private void PlayMonologue(Data.DialogData data)
    {
        if (data.Dialogues.Length != 1)
            throw new Exception("[DialogSystem] TalkOneSelf 메서드는 대화가 한 줄인 경우에만 사용해야 합니다.");

        int targetId = data.Dialogues[0].SpeakerId;
        var target = Managers.Object.FindCharacter(targetId);

        StopSequenceLoop(targetId);

        _currentDialogueId = data.TemplateId;
        _currentSpeechBubble = target.SpeechBubble;
        IsPlaying = true;

        Sequence seq = DOTween.Sequence();

        foreach (var dialog in data.Dialogues)
        {
            float typingTime = dialog.Conversation.Length * UI_SpeechBubble.CharTypingDuration;
            float waitTime = 2f;

            seq.AppendCallback(() => target.SpeechBubble.Show(dialog.Conversation, data.EventType));
            seq.AppendInterval(typingTime + waitTime);
            seq.AppendCallback(() => target.SpeechBubble.Hide());
        }

        seq.OnComplete(() => {
            EndDialog();
        });

        DialogSequences.Add(target, seq);
    }
    #endregion
    #region Choice
    public void SelectChoice(int choiceIndex)
    {
        if (CurrentData == null || CurrentDialogue.Choices == null || choiceIndex < 0 || choiceIndex >= CurrentDialogue.Choices.Length)
        {
            Debug.LogError($"[DialogSystem] 선택할 수 없는 선택지 인덱스입니다: {choiceIndex}");
            return;
        }
        _selectedChoiceIndex = choiceIndex;
        var selectedChoice = CurrentDialogue.Choices[choiceIndex];
        _selectedDialogue = new Queue<Data.Dialogue>(selectedChoice.Dialogues);
        AdvanceToNextLine();
    }
    #endregion
}