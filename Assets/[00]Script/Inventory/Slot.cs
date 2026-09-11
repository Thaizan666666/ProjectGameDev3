using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool hovering;

    private ItemSO heldItem;
    private int itemAmount;

    private Image iconImage;
    private TextMeshProUGUI amountTxt;
    private Image backgroundImage; // cache background so we never disable it

    private void Awake()
    {
        // 1) Cache background image (on root) so we never touch it
        backgroundImage = GetComponent<Image>();

        // 2) Try Icon child by name
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            iconImage = iconTransform.GetComponent<Image>();

        // 3) Fallback: search children for an Image that is NOT the background
        if (iconImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img != backgroundImage) // skip root background
                {
                    iconImage = img;
                    break;
                }
            }
        }

        // 4) Amount text: try "Amount" child first, then any TMP in children
        Transform amountTransform = transform.Find("Amount");
        if (amountTransform != null)
            amountTxt = amountTransform.GetComponent<TextMeshProUGUI>();
        if (amountTxt == null)
            amountTxt = GetComponentInChildren<TextMeshProUGUI>(true);

        // Debug: log what we found (remove after verified)
        // Debug.Log($"[Slot] iconImage={iconImage?.name ?? "NULL"}, background={backgroundImage?.name ?? "NULL"}, amountTxt={amountTxt?.name ?? "NULL"}");
    }

    public ItemSO GetItem() => heldItem;
    public int GetAmount() => itemAmount;

    public void SetItem(ItemSO item, int amount = 1)
    {
        heldItem = item;
        itemAmount = amount;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (iconImage == null || amountTxt == null) return;

        if (heldItem != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = heldItem.icon;
            amountTxt.text = itemAmount.ToString();
        }
        else
        {
            iconImage.enabled = false;
            amountTxt.text = "";
        }
    }

    public int AddAmount(int amountToAdd)
    {
        itemAmount += amountToAdd;
        UpdateSlot();
        return itemAmount;
    }

    public int RemoveAmount(int amountToRemove)
    {
        itemAmount -= amountToRemove;
        if (itemAmount <= 0)
        {
            ClearSlot();
        }
        else
        {
            UpdateSlot();
        }
        return itemAmount;
    }

    public void ClearSlot()
    {
        heldItem = null;
        itemAmount = 0;
        UpdateSlot();
    }

    public bool HasItem() => heldItem != null;

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;
    public void OnPointerExit(PointerEventData eventData) => hovering = false;
}