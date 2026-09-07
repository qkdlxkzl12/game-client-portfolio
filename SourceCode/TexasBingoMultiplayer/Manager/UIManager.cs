using UnityEngine;
using TB.GameState;
using UnityEngine.UI;
using DG.Tweening;
using TB.Extention.GM;

public enum UIType
{
    EventCard,
    RevealedCard,
}

public class UIManager : StateSingleton<UIManager>
{
    [SerializeField] private Image _blackPanel;
    [Header("UI Objects")]
    [SerializeField] private UIObjectBase[] _uiObjects;


    private void Awake()
    {
        Initialize(GameState.Ingame);
        _blackPanel.color = new Color(0f, 0f, 0f, 0f);
    }

    //임시 코드
    public void Start()
    {
        GameManager.Instance.AddDurationPhase(TurnPhase.TurnStart, () => FadeBlackPanel(false), -1);
        GameManager.Instance.AddDurationPhase(TurnPhase.Placement, () => FadeBlackPanel(true), -1);
    }

    public void SetVisible<T>(bool visible) where T : UIObjectBase
    {
        var ui = GetUI<T>();
        ui.gameObject.SetActive(visible);
        if (visible && ui.UseBlackPanel && _blackPanel.gameObject.activeSelf)
        {
            _blackPanel.gameObject.SetActive(true);
            if (ui.BPFadeDuration != -1)
            {
                FadeBlackPanel(true, ui.BPFadeDuration);
            }
        }
    }

    public void FadeBlackPanel(bool isOut, float duration = 0.5f)
    {
        float startValue = isOut ? 0.8f : 0;
        float endValue = isOut ? 0 : 0.8f;
        var tempColor = _blackPanel.color;
        if (tempColor.a != startValue)
            return;
        _blackPanel.gameObject.SetActive(true);
        _blackPanel.color = tempColor;
        _blackPanel.DOFade(endValue, duration);
    }

    public T GetUI<T>() where T : UIObjectBase
    {
        foreach (var uiObject in _uiObjects)
        {
            if (uiObject is T)
                return uiObject as T;
        }
        throw new System.Exception($"존재하지 않는 UI에 접근하려 했습니다 {typeof(T)}");
    }
}
