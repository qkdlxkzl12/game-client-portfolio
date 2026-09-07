using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

//다중 입력 상태를 고려한 Flags값
[Flags]
public enum EUserAction
{ 
    None = 0,
    MoveForward = 1 << 0,
    MoveBackward = 1 << 1,
    MoveLeft = 1 << 2,
    MoveRight = 1 << 3,
    Sprint = 1 << 4,
    Interaction = 1 << 5,
    NextDialog = 1 << 6,
    CheckList = 1 << 7,
    Inventory = 1 << 8,
    ClueCollection = 1 << 9,
    Setting = 1 << 10,
    Cancle = 1 << 11,
}

// 내부적인 값으로 처리 우선도를 지정하는 방법도 고민했지만
// 우선도에 영향을 주는 변수가 상황 별로 존재할거 같아 키 눌림 정도만 판단
public class KeyInputSystem
{
    //행동-키값 캐싱
    public Dictionary<EUserAction, KeyCode> BindingKeys { get; private set; }

    public KeyInputSystem() 
    {
        Init();
    }

    //기본 설정값으로 초기화
    private void Init()
    {
        Dictionary<EUserAction, KeyCode> dic = new();

        dic.Add(EUserAction.MoveForward,KeyCode.D);
        dic.Add(EUserAction.MoveBackward, KeyCode.A);
        dic.Add(EUserAction.MoveLeft, KeyCode.W);
        dic.Add(EUserAction.MoveRight, KeyCode.S);
        dic.Add(EUserAction.Sprint, KeyCode.LeftShift);
        dic.Add(EUserAction.Interaction, KeyCode.F);
        dic.Add(EUserAction.NextDialog, KeyCode.Space);
        dic.Add(EUserAction.Inventory, KeyCode.T);
        dic.Add(EUserAction.ClueCollection, KeyCode.Y);
        dic.Add(EUserAction.CheckList, KeyCode.R);
        dic.Add(EUserAction.Setting, KeyCode.Escape);
        dic.Add(EUserAction.Cancle, KeyCode.Escape);

        BindingKeys = dic;
    }

    //다른 키로 변경하는 메서드
    IEnumerator BindNewKey(EUserAction action)
    {
        yield return new WaitUntil(() => Input.anyKeyDown);
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if(Input.GetKeyDown(key))
            {
                KeyCode pressKey = key;
                if(BindingKeys.Values.Contains(pressKey))
                {
                    //다른 동작에 해당 키가 바인딩되어있을 경우 에외 처리 구간
                    //1. 이전 키를 중복 동작에 넣는지
                    //2. 빈 칸을 유지하고 저장 때 판별하는지
                    //3.중복 동작이 존재할 경우 변경이 불가능하닞
                }
                BindingKeys[action] = pressKey;
            }
        }
    }

    //행돌 별 눌린 상태 확인 onHold = true 일 경우 지속되는 상태
    public bool IsPress(EUserAction action, bool onHold = false)
    {
        if (BindingKeys.ContainsKey(action) == false)
            return false;
        if (onHold)
            return Input.GetKey(BindingKeys[action]);
        else
            return Input.GetKeyDown(BindingKeys[action]);
    }

    //이동 방향에 선정 가중치를 부여해 Vector2 형태로 반환
    public Vector2 GetInputMoveDir()
    {
        Vector2 dir = Vector3.zero;
        dir.x = IsPress(EUserAction.MoveForward, true) ? 1f :
            IsPress(EUserAction.MoveBackward, true) ? -1 : 0;
        dir.y = IsPress(EUserAction.MoveLeft, true) ? 1f :
            IsPress(EUserAction.MoveRight, true) ? -1 : 0;
        return dir;
    }

    //어떤 키가 눌렸는지 알 필요 없이 어떤 행동에 대해 입력이 됐는지만 전달
    public EUserAction GetOnPressActionAll (bool onHold = false)
    {
        EUserAction result = EUserAction.None;
        foreach (var action in Enum.GetValues(typeof(EUserAction)))
        {
            if(IsPress((EUserAction)action, onHold))
            {
                result |= (EUserAction)action;
            }
        }
        return result;
    }
}
