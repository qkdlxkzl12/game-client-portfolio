using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class UI_Binder_Inventory : UI_BinderSection
{
    //슬롯창 관련
    private Image[] _itemImages;
    private TMP_Text[] _itemCountTexts;
    //선택창 관련
    private Image _selectItemImage;
    private TMP_Text _selectItemDisplayText;
    private TMP_Text _selectIteDescriptionText;
    private Button _useButton;
    private int _selectSlotIndex = -1;

    private InventorySystem Inventory => Managers.Game.Inventory;

    public override EBinderSectionType State => EBinderSectionType.Inventory;
    public UI_Binder_Inventory(UI_Binder owner)
    {
        Owner = owner;
    }
    public override void InitSection(RectTransform left, RectTransform right)
    {
        InitInventorySlot(Util.FindChild(left.gameObject, "Slots", true));
        InitSelectInfo(Util.FindChild(right.gameObject, "SelectInfo", true));
    }

    public override void OnSectionOpen()
    {
        for (int i = 0; i < _itemImages.Length; i++)
        {
            UpdateItemSlot(i);
        }
        if (Inventory.GetSlot(0) != null)
            SelectItem(0);
        else        {
            _selectItemImage.sprite = null;
            _selectItemImage.gameObject.SetActive(false);
            _selectItemDisplayText.text = "";
            _selectIteDescriptionText.text = "";
            _useButton.gameObject.SetActive(false);
        }
    }

    public void SelectItem(int slotIndex)
    {
        if (_selectSlotIndex == slotIndex)
            return; 
        var slot = Inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
            return;
        _selectSlotIndex = slotIndex;
        _selectItemImage.sprite = slot.ItemData.Sprite.Load();
        _selectItemImage.gameObject.SetActive(true);
        _selectItemDisplayText.text = slot.ItemData.Name;
        _selectIteDescriptionText.text = slot.ItemData.Description;
        _useButton.gameObject.SetActive(slot.ItemData.Useable);
        Owner.PlayButtonSound();
    }

    private void UpdateItemSlot(int slotIndex)
    {
        var slot = Inventory.GetSlot(slotIndex);
        var image = _itemImages[slotIndex];
        var countText = _itemCountTexts[slotIndex];
        if (slot == null || slot.IsEmpty)
        {
            image.gameObject.SetActive(false);
            image.sprite = null;
            countText.text = "";
        }
        else
        {
            image.gameObject.SetActive(true);
            image.sprite = slot.ItemData.Sprite.Load();
            countText.text = (slot.Count == 1) ? "" : slot.Count.ToString();
        }
    }
    private void InitInventorySlot(GameObject area)
    {
        var slotRoot = area.transform;

        if (slotRoot.childCount != InventorySystem.MaxSlots)
            Debug.LogError($"Not Valid Inventory Slot Count. Current:{slotRoot.childCount}, Required:{InventorySystem.MaxSlots}");
        _itemImages = new Image[slotRoot.childCount];
        _itemCountTexts = new TMP_Text[slotRoot.childCount];

        for (int i = 0; i < slotRoot.childCount; i++)
        {
            var slot = slotRoot.GetChild(i).GetComponent<Image>();
            _itemImages[i] = slot.transform.GetChild(0).GetComponent<Image>();
            _itemCountTexts[i] = slot.transform.GetChild(1).GetComponent<TMP_Text>();
            var eventHandler = Util.GetOrAddComponent<UI_EventHandler>(slot.gameObject);
            int index = i;
            eventHandler.OnPointerEnterHandler += _ =>
            {
                slot.DOKill();
                slot.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad).SetTarget(slot);
            };
            eventHandler.OnPointerExitHandler += _ =>
            {
                slot.DOKill();
                slot.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad).SetTarget(slot);
            };

            eventHandler.OnClickHandler += _ =>
            {
                SelectItem(index);
            };
        }
    }
    private void InitSelectInfo(GameObject area)
    {
        _selectItemImage = Util.FindChild(area.gameObject, "IconImage", true).GetComponent<Image>();
        _selectItemDisplayText = Util.FindChild(area.gameObject, "DisplayText", true).GetComponent<TMP_Text>();
        _selectIteDescriptionText = Util.FindChild(area.gameObject, "DescriptionText", true).GetComponent<TMP_Text>();
        _useButton = Util.FindChild(area.gameObject, "UseButton", true).GetComponent<Button>();
        _useButton.onClick.AddListener(UseSelectedItem);
    }
    private void UseSelectedItem()
    {
        int beforeCount = Inventory.GetSlot(_selectSlotIndex).Count;
        if (Inventory.TryUseItem(_selectSlotIndex) == true)
        {
            //사용 후 빈 슬롯이 됐을 경우
            if (beforeCount == 1)
            {
                for (int i = 0; i < _itemImages.Length; i++)
                {
                    UpdateItemSlot(i);
                }
                //혹은 이전 슬롯의 아이템 지정
                if (Inventory.GetSlot(0) != null)
                    SelectItem(0);
            }
            else
                UpdateItemSlot(_selectSlotIndex);
        }
    }
}
