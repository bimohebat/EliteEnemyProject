using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;   // tambahkan using ini

public class ItemSlotButton : MonoBehaviour
{
    [Header("Referensi UI (drag dari child prefab)")]
    public Image icon;
    public TMP_Text nameText;       // ganti dari Text jadi TMP_Text
    public TMP_Text quantityText;   // ganti dari Text jadi TMP_Text
    public Button button;

    private ConsumableItem boundItem;
    private Action<ConsumableItem> onSelected;

    public void Bind(InventorySlot slot, Action<ConsumableItem> onSelectedCallback)
    {
        boundItem = slot.item;
        onSelected = onSelectedCallback;

        if (icon != null) icon.sprite = slot.item.icon;
        if (nameText != null) nameText.text = slot.item.itemName;
        if (quantityText != null) quantityText.text = $"x{slot.quantity}";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(boundItem));
    }
}