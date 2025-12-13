using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemSlot : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public Image selectionOutline; // Optional für später

    public void UpdateSlot(ItemStack stack)
    {
        if (stack != null && stack.item != null)
        {
            // Item vorhanden
            iconImage.sprite = stack.item.icon;
            iconImage.enabled = true; // Icon sichtbar machen
            
            // Text nur anzeigen, wenn Menge > 1 (optional, aber schöner)
            amountText.text = stack.amount.ToString();
            amountText.enabled = true;
        }
        else
        {
            // Slot ist leer
            iconImage.sprite = null;
            iconImage.enabled = false;
            amountText.text = "";
            amountText.enabled = false;
        }
    }
}