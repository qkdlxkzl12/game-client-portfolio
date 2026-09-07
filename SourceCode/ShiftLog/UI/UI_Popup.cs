using DG.Tweening;
using UnityEngine;

public interface IInitializablePopup<T>
{
    void Initialize(T data);
}

public class UI_Popup : UI_Base
{
    public virtual bool UseBlinder => true;         //가림막(딤) 사용 여부
    public virtual bool IsInterruptible => false;   //연출 도중에 창이 비/황성화 여부
    public virtual bool IsTracked => true;          //UI매니저에서 추적 관리하는지 여부
    public virtual bool AffectedCancel => true;     //창 닫기 키에 영향을 받는지
    public bool IsOpening { get; protected set; }   //열람 연출 중인지
    public bool IsClosing { get; protected set; }   //닫는 연출 중인지
    public CanvasGroup CanvasGroup { get; private set; }

    public override bool Init()
    {
        if (base.Init() == false) 
            return false;

        //매니저에서 추적하지 않는데 블라인드를 사용할 수 없음
        //논리적 오류라 초기화 중 flase 반환
        if (IsTracked == false && UseBlinder == transform)
            return false;
        CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        IsClosing = false;
        Managers.UI.SetCanvas(gameObject, true);
        return true;
    }

    //연출 도중 변환 가능 여부와 현재상태 확인
    public bool CanSwapOther()
    {
        if (IsInterruptible == false && (IsClosing == true || IsOpening == true))
        {
            return false;
        }
        return true;
    }

    public virtual bool OpenPopupUI()
    {
        //이미 활성화 되어있다면, 열기 요청을 무시
        if (gameObject.activeSelf == true)
            return false;

        transform.DOKill();
        //연출 중에는 상호작용 불가하도록 설정
        IsOpening = true;
        CanvasGroup.interactable = false;
        gameObject.SetActive(true);
        var showSeq = CreateOpenSequence();
        showSeq.onPlay += ( ) =>
        {
            //딤 사용 여부에 따라 활성화
            if (UseBlinder == true)
            {
                var blinder = Managers.UI.ShowBlinder();
                int index = transform.GetSiblingIndex() - 1;
                blinder.SetSiblingIndex(index);
            }
        };
        //연출 완료 후, 상호작용 가능하도록 설정
        showSeq.onComplete += ( ) =>
        {
            CanvasGroup.interactable = true;
            IsOpening = false;
        };
        showSeq.Play();
        return true;
    }

    //연출 시퀀스 정의
    protected virtual Sequence CreateOpenSequence()
    {
        return DOTween.Sequence().SetTarget(transform);
    }
    //연출 시퀀스 정의
    protected virtual Sequence CreateCloseSequence()
    {
        return DOTween.Sequence().SetTarget(transform);
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }

    //기존 강의용 코드에서 종료연출 관련 코드 추가
    public void RequestClose()
    {
        if (gameObject.activeSelf == false)
        {
            Debug.Log($"Popup is already closing. Ignoring open request. Popup: {gameObject.name}");
            return;
        }

        transform.DOKill();
        CanvasGroup.interactable = false;
        IsClosing = true;
        var cloeseSequence = CreateCloseSequence();
        cloeseSequence.onPlay += () =>
        {
            if (UseBlinder == true)
                Managers.UI.CloseBlinder();
        };
        cloeseSequence.onComplete += () =>
        {
            CanvasGroup.interactable = false;
            IsClosing = false;
            gameObject.SetActive(false);
            if (IsTracked == true)
                Managers.UI.CurPopup = null;
        };

        cloeseSequence.Play();
    }
}