using UnityEngine;
using UnityEngine.UI;

public class UI_Binder_Setting : UI_BinderSection
{
    enum ESliderType
    {
        Total,
        BGM,
        SFX,
        Max,
    }

    private Slider[] _sliders = new Slider[(int)ESliderType.Max];

    public override EBinderSectionType State => EBinderSectionType.Setting;

    public UI_Binder_Setting(UI_Binder owner)
    {
        Owner = owner;
    }

    public override void InitSection(RectTransform left, RectTransform right)
    {
        InitKeySetting(left);
        InitGameSetting(right);
    }

    private void InitKeySetting(Transform area)
    {
        //todo : 키 설정
    }

    private void InitGameSetting(Transform area)
    {
        //음량 관련
        _sliders[(int)ESliderType.Total] = Util.FindChild<Slider>(area.gameObject, "TotalSlider", true);
        Debug.Log($"{_sliders[(int)ESliderType.Total]}");
        _sliders[(int)ESliderType.Total].onValueChanged.AddListener(value =>
        {
            Managers.Sound.Mixer.SetDecibel("Master", value/100);
        });
        _sliders[(int)ESliderType.BGM] = Util.FindChild<Slider>(area.gameObject, "BGMSlider", true);
        _sliders[(int)ESliderType.BGM].onValueChanged.AddListener(value =>
        {
            Managers.Sound.Mixer.SetDecibel("BGM", value/100);
        });
        _sliders[(int)ESliderType.SFX] = Util.FindChild<Slider>(area.gameObject, "SFXSlider", true);
        _sliders[(int)ESliderType.SFX].onValueChanged.AddListener(value =>
        {
            Managers.Sound.Mixer.SetDecibel("Effect", value/100);
        });
        //저장 관련
        var saveButton = Util.FindChild<Button>(area.gameObject, "SaveButton", true);
        saveButton.onClick.AddListener(() =>
        {
            Owner.PlayButtonSound();
            //todo : 저장
        });
        var exitButton = Util.FindChild<Button>(area.gameObject, "ExitButton", true);
        exitButton.onClick.AddListener(() =>
        {
            Owner.PlayButtonSound();
            Managers.Scene.LoadScene(Define.EScene.TitleScene);
            //todo : 저장
        });

    }

    public override void OnSectionOpen()
    {
    }
}
