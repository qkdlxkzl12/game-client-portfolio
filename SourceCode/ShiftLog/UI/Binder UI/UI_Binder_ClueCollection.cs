using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//인벤토리 색션을 인용
public class UI_Binder_ClueCollection : UI_BinderSection
{
    public override EBinderSectionType State => EBinderSectionType.ClueCollection;
    class ClueSlot
    {
        public GameObject Frame { get; set; }
        public Image IconImage { get; set; }
        public int ClueId;

        public Data.ClueData Data => Managers.Data.ClueDic[ClueId];
        public bool IsEmpty => ClueId == -1;

        public ClueSlot(GameObject frame, Image icon)
        {
            Frame = frame;
            IconImage = icon;
            ClueId = -1;
        }
    }
    ClueSlot[] _slots;
    private int _selectSlotIndex = -1;

    private Image _selectItemImage;
    private TMP_Text _selectItemDisplayText;
    private TMP_Text _selectIteDescriptionText;

    public UI_Binder_ClueCollection(UI_Binder owner)
    {
        Owner = owner;
    }

    public override void InitSection(RectTransform left, RectTransform right)
    {
        InitCollectionSlot(Util.FindChild(left.gameObject, "ClueCollection", true));
        InitSelectInfo(Util.FindChild(right.gameObject, "SelectInfo", true));
    }


    private void InitCollectionSlot(GameObject area)
    {
        var slotRoot = area.transform;

        _slots = new ClueSlot[slotRoot.childCount];
        for (int i = 0; i < slotRoot.childCount; i++)
        {
            var frame = slotRoot.GetChild(i).GetChild(0);
            var IconImage = frame.transform.GetChild(0).GetComponent<Image>();
            _slots[i] = new ClueSlot(frame.gameObject, IconImage);
            var eventHandler = Util.GetOrAddComponent<UI_EventHandler>(frame.gameObject);
            int index = i;
            eventHandler.OnPointerEnterHandler += _ =>
            {
                frame.DOKill();
                frame.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad).SetTarget(frame);
            };
            eventHandler.OnPointerExitHandler += _ =>
            {
                frame.DOKill();
                frame.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad).SetTarget(frame);
            };
            eventHandler.OnClickHandler += _ => 
            {
                Select(index);
            };
        }
    }
    public void Select(int slotIndex)
    {
        if (_selectSlotIndex == slotIndex)
            return;
        var slot = _slots[slotIndex];
        if (slot.IsEmpty)
            return;
        _selectSlotIndex = slotIndex;
        _selectItemImage.sprite = slot.Data.Sprite.Load();
        _selectItemImage.gameObject.SetActive(true);
        _selectItemDisplayText.text = slot.Data.Name;
        _selectIteDescriptionText.text = slot.Data.Description;
        Owner.PlayButtonSound();
    }
    private void InitSelectInfo(GameObject area)
    {
        _selectItemImage = Util.FindChild(area.gameObject, "IconImage", true).GetComponent<Image>();
        _selectItemDisplayText = Util.FindChild(area.gameObject, "DisplayText", true).GetComponent<TMP_Text>();
        _selectIteDescriptionText = Util.FindChild(area.gameObject, "DescriptionText", true).GetComponent<TMP_Text>();
    }

    public override void OnSectionOpen()
    {
        var collectedClueIndexs = Managers.Game.Clues.GetRemainClues();
        UpdateSlots(collectedClueIndexs);

        _selectSlotIndex = -1;
        if (collectedClueIndexs.Length != 0)
            Select(0);
        else        {
            _selectItemImage.sprite = null;
            _selectItemImage.gameObject.SetActive(false);
            _selectItemDisplayText.text = "";
            _selectIteDescriptionText.text = "";
        }

    }

    private void UpdateSlots(int[] slotIndexs)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            //현재 슬롯에 획득한 단서로 초기화
            if (i < slotIndexs.Length)
            {
                int clueId = slotIndexs[i];
                var clueData = Managers.Data.ClueDic[clueId];
                slot.ClueId = clueId;
                slot.Frame.SetActive(true);
                slot.IconImage.sprite = clueData.Sprite.Load();
                slot.IconImage.gameObject.SetActive(true);
            }
            else
            {
                slot.ClueId = -1;
                slot.Frame.SetActive(false);
                slot.IconImage.sprite = null;
                slot.IconImage.gameObject.SetActive(false);
            }
        }
    }


}
