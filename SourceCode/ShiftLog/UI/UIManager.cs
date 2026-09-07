using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager
{
    private int _order = 10;

    Dictionary<string, UI_Popup> _popupDict = new Dictionary<string, UI_Popup>();
    public event Action<UI_Popup, bool> OnPopupToggled;

    public UI_Popup _curPopup;
    public UI_Popup CurPopup { 
        get => _curPopup;
        set
        {
            _curPopup = value;
        }
    }

    private Image _fader;
    public Image Fader
    {
        get
        {
            if (_fader == null)
            {
                GameObject go = Managers.Resource.Instantiate("UI_Fader", Root.transform);
                Managers.UI.SetCanvas(go);
                _fader = go.GetComponentInChildren<Image>();
                _fader.gameObject.SetActive(false);
                _fader.color = new Color(0, 0, 0, 0);
            }
            return _fader;
        }
    }
    private Image _blinder;
    public Image Blinder
    {
        get
        {
            if (_blinder == null)
            {
                _blinder = Managers.Resource.Instantiate("UI_Blinder", Root.transform).GetComponent<Image>();
                _blinder.gameObject.SetActive(false);
                _blinder.color = new Color(0, 0, 0, 0.7f);
            }
            return _blinder;
        }
    }
    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
            return root;
        }
    }
    #region Fader
    public void StartFade(float duration = 1f, float startFadOutDelay = 0.2f, Action onFadeIn = null, Action onFadeOut = null)
    {
        Fader.gameObject.SetActive(true);
        int index = Root.transform.childCount -1;
        Fader.transform.SetSiblingIndex(index);
        Fader.color = new Color(0, 0, 0, 0);
        DOTween.Sequence()
            .Append(Fader.DOFade(1f, duration))
            .AppendCallback(() => onFadeIn?.Invoke())
            .AppendInterval(startFadOutDelay)
            .Append(Fader.DOFade(0f, duration))
            .AppendCallback(() => onFadeOut?.Invoke())
            .OnComplete(() => Fader.gameObject.SetActive(false));
    }
    #endregion

    #region Blinder
    public RectTransform ShowBlinder()
    {
        Blinder.gameObject.SetActive(true);
        return Blinder.rectTransform;

    }
    public void CloseBlinder()
    {
        Blinder.gameObject.SetActive(false);
    }
    #endregion
    public bool IsOpen<T>() where T : UI_Popup
    {
        if(_popupDict.TryGetValue(typeof(T).Name, out UI_Popup popup) == false)
            return false;
        if (popup.IsTracked == true)
        {
            if (CurPopup == popup)
                return true;
        }
        else
        {
            return true;
        }
        return false;
    }

    public bool AnyOpen()
    {
        return CurPopup != null;
    }


    public void SetCanvas(GameObject go, bool sort = true, int sortOrder = 0)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
        }

        CanvasScaler cs = go.GetOrAddComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        go.GetOrAddComponent<GraphicRaycaster>();
        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = sortOrder;
        }
    }
    #region PopupUI
    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (CurPopup != null && CurPopup.CanSwapOther() == false)
            return null;

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        //생성되지 않은 팝업창일 경우
        if (_popupDict.TryGetValue(name, out UI_Popup popup) == false)
        {
            GameObject go = Managers.Resource.Instantiate(name);
            popup = go.GetComponent<T>();
            popup.gameObject.SetActive(false);

            if (popup == null)
                Debug.LogError($"[UIManager] ShowPopupUI 실패! {name} 프리팹에 UI_Popup 컴포넌트가 없습니다.");

            go.transform.SetParent(Root.transform);
            _popupDict.Add(name, popup);
        }
        else
        {
            //비관리 팝업일 때 이전 창이 켜있다면 삭제 후 새 창 생성
            if (popup.IsTracked == false)
            {
                GameObject.Destroy(popup.gameObject);
                _popupDict.Remove(name);

                GameObject go = Managers.Resource.Instantiate(name);
                popup = go.GetComponent<T>();
                popup.gameObject.SetActive(false);
                go.transform.SetParent(Root.transform);
                _popupDict.Add(name, popup);
            }
        }

        if (popup.IsTracked == true)
        {
            //이전 창이 닫힐 수 없는 상태라면 창을 새로 열지 않음
            if (CurPopup?.AffectedCancel == false)
                return null;
            //이전 팝업이 존재한다면 닫음
            if (CurPopup != null && CurPopup != popup)
            {
                CurPopup.gameObject.SetActive(false);
                OnPopupToggled?.Invoke(CurPopup, false);
            }

            CurPopup = popup;
        }
        popup.OpenPopupUI();
        OnPopupToggled?.Invoke(popup, true);

        return popup as T;
    }
    //열람 시 초기화가 필수로 이루어져야할 경우 사용
    public T ShowPopupUI<T, D>(D data, string name = null) where T : UI_Popup, IInitializablePopup<D>
    {
        if (CurPopup != null && CurPopup.CanSwapOther() == false)
            return null;

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;


        if (_popupDict.TryGetValue(name, out UI_Popup popup) == false)
        {
                GameObject go = Managers.Resource.Instantiate(name);
            popup = go.GetComponent<T>();
            popup.gameObject.SetActive(false);

            if (popup == null)
                Debug.LogError($"[UIManager] ShowPopupUI 실패! {name} 프리팹에 UI_Popup 컴포넌트가 없습니다.");

            go.transform.SetParent(Root.transform);
            _popupDict.Add(name, popup);
        }
        else
        {
            //비관리 팝업일 때 이전 창이 켜있다면 삭제 후 새 창 생성
            if(popup.IsTracked == false)
            {
                _popupDict.Remove(name);

                GameObject go = Managers.Resource.Instantiate(name);
                popup = go.GetComponent<T>();
                popup.gameObject.SetActive(false);
                go.transform.SetParent(Root.transform);
                _popupDict.Add(name, popup);
            }
        }

        //관리되는 창이라면
        if (popup.IsTracked == true)
        {
            //이전 창이 닫힐 수 없는 상태라면 창을 새로 열지 않음
            if (CurPopup?.AffectedCancel == false)
                return null;
            if (CurPopup != null && CurPopup != popup)
            {
                CurPopup.gameObject.SetActive(false);
                OnPopupToggled?.Invoke(CurPopup, false);
            }
            CurPopup = popup;
        }

            var initializable = popup.GetComponent<IInitializablePopup<D>>();
        initializable.Initialize(data);

        popup.OpenPopupUI();
        OnPopupToggled?.Invoke(popup, true);

        return popup as T;
    }
    //득청 팝업창 닫기
    public void ClosePopupUI(UI_Popup popup)
    {
        if (popup.CanSwapOther() == false)
        {
            return;
        }
        if (popup.IsTracked == false)
        {
            if(_popupDict.ContainsValue(popup) == false)
            {
                return;
            }
            OnPopupToggled?.Invoke(popup, false);
            _popupDict.Remove(popup.name);
            //여기서가 맞을까?
            //ClosePopupUI();
            GameObject.Destroy(popup.gameObject);
            return;
        }
        if (CurPopup == null) return;
        if (CurPopup != popup)
        {
            Debug.Log($"{CurPopup.GetType()}-{popup.GetType()}");
            Debug.Log("Close Popup Failed!");
            return;
        }
        ClosePopupUI();
    }
    //현재 팝업창 닫기
    public void ClosePopupUI()
    {
        if (CurPopup == null) return;

        if (CurPopup.CanSwapOther() == false)
            return;

        UI_Popup prevPopup = CurPopup;
        if (prevPopup != null)
        {
            prevPopup.RequestClose();
            OnPopupToggled?.Invoke(prevPopup, false);
        }
    }
    public void ClosePopupUI<T>() where T : UI_Popup
    {
        if (_popupDict.TryGetValue(typeof(T).Name, out UI_Popup popup) == false)
            return;
        ClosePopupUI(popup);
    }
    public void TogglePopupUI<T>(string name = null) where T : UI_Popup
    {
        if (IsOpen<T>() == true)
        {
            ClosePopupUI();
        }
        else
        {
            ShowPopupUI<T>(name);
        }
    }

    public void TogglePopupUI<T,D>(D data, string name = null) where T : UI_Popup, IInitializablePopup<D>
    {
        if (IsOpen<T>() == true)
        {
            ClosePopupUI();
        }
        else
        {
            ShowPopupUI<T,D>(data, name);
        }
    }
    #endregion
}
