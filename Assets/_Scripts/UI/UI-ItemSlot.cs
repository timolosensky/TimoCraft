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
            // Daten setzen
            iconImage.sprite = stack.item.icon;
            amountText.text = stack.amount.ToString();
            
            // SICHTBARKEIT ERZWINGEN (Fix für unsichtbare Icons)
            iconImage.enabled = true;
            iconImage.color = Color.white; // Alpha auf 100% zwingen
            amountText.enabled = true;
        }
        else
        {
            // Leeren Slot aufräumen
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.color = Color.clear; // Komplett transparent machen
            amountText.text = "";
            amountText.enabled = false;
        }
    }
}