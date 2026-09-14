using UnityEngine;
using System.Collections.Generic;
using Unity.Properties;

[System.Serializable]
public class CartSlot 
{
    public ItemSO item;
    public int amount;
    public int sellPrice;  // ราคาขายต่อชิ้น (จะถูก set จาก FishData.Price ตอนใส่ปลาลงรถ)

    public bool HasItem => item != null && amount > 0;
    public int TotalValue => item != null ? sellPrice * amount : 0;
}

public class CartStorage : MonoBehaviour, IInteractable
{
    [Header("Storage")]
    public int cartSize = 10;
    public CartSlot[] storedItems;

    [Header("References")]
    [Tooltip("ลาก UI ของรถเข็น (ถ้ามี)")]
    public GameObject cartUI;

    private bool isOpen;

    private void Awake()
    {
        storedItems = new CartSlot[cartSize];
        for (int i = 0; i < cartSize; i++)
        {
            storedItems[i] = new CartSlot();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // UI Open/Close
    // ─────────────────────────────────────────────────────────────

    public void Interact()
    {
        if (!isOpen)
            OpenCart();
        else
            CloseCart();
    }

    void OpenCart()
    {
        Inventory.instance.OpenInventory(showInventorySlots: Inventory.instance.HasBag);
        Inventory.instance.ShowCartUI(this);

        isOpen = true;

        Slot[] slots = Inventory.instance.GetCartSlots();
        int limit = Mathf.Min(slots.Length, storedItems.Length);

        for (int i = 0; i < limit; i++)
        {
            var data = storedItems[i];
            if (data.item != null)
                slots[i].SetItem(data.item, data.amount);
            else
                slots[i].ClearSlot();
        }
    }

    public void CloseCart()
    {
        if (!isOpen) return;

        Slot[] slots = Inventory.instance.GetCartSlots();
        int limit = Mathf.Min(slots.Length, storedItems.Length);

        for (int i = 0; i < limit; i++)
        {
            if (slots[i].HasItem())
            {
                storedItems[i].item = slots[i].GetItem();
                storedItems[i].amount = slots[i].GetAmount();
            }
            else
            {
                storedItems[i].item = null;
                storedItems[i].amount = 0;
                storedItems[i].sellPrice = 0;
            }
        }

        isOpen = false;

        Inventory.instance.HideCartUI();
        Inventory.instance.CloseInventory();
    }

    public void ForceClose()
    {
        CloseCart();
    }

    // ─────────────────────────────────────────────────────────────
    // Item Management
    // ─────────────────────────────────────────────────────────────

    /// <summary>เพิ่ม item เข้ารถ — ถ้าเป็นปลา จะ lookup ราคาจาก FishData.Price อัตโนมัติ</summary>
    public bool AddItem(ItemSO itemToAdd, int amount)
    {
        if (itemToAdd == null || amount <= 0) return false;

        // ถ้า ItemSO มี linked FishData → ใช้ FishData.Price, ไม่งั้นใช้ ItemSO.sellPrice
        int resolvedPrice = itemToAdd.sellPrice;
        FishData fish = FishData.FindByLinkedItem(itemToAdd);
        if (fish != null)
            resolvedPrice = fish.Price;

        return AddItemInternal(itemToAdd, amount, resolvedPrice);
    }

    /// <summary>เพิ่ม item เข้ารถ + ระบุราคาขายต่อชิ้นเอง (override FishData.Price)</summary>
    public bool AddItemWithPrice(ItemSO itemToAdd, int amount, int customSellPrice)
    {
        if (itemToAdd == null || amount <= 0) return false;
        return AddItemInternal(itemToAdd, amount, customSellPrice);
    }

    private bool AddItemInternal(ItemSO itemToAdd, int amount, int resolvedPrice)
    {
        if (itemToAdd == null || amount <= 0) return false;

        int remaining = amount;

        // stack กับ slot ที่มี item เดียวกันก่อน
        foreach (var slot in storedItems)
        {
            if (slot.item == itemToAdd && slot.HasItem && slot.amount < itemToAdd.maxStackSize)
            {
                int spaceLeft = itemToAdd.maxStackSize - slot.amount;
                int amountToAdd = Mathf.Min(spaceLeft, remaining);
                slot.amount += amountToAdd;
                slot.sellPrice = resolvedPrice; // sync ราคา
                remaining -= amountToAdd;
                if (remaining <= 0) return true;
            }
        }

        // ใส่ slot ว่าง
        foreach (var slot in storedItems)
        {
            if (!slot.HasItem)
            {
                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.item = itemToAdd;
                slot.amount = amountToPlace;
                slot.sellPrice = resolvedPrice;
                remaining -= amountToPlace;
                if (remaining <= 0) return true;
            }
        }

        if (remaining > 0)
            Debug.LogWarning($"[CartStorage] รถเข็นเต็ม — ใส่ไม่ได้ {remaining} ชิ้นของ {itemToAdd.itemName}");

        return remaining <= 0;
    }

    /// <summary>ลบ item จาก slot ที่ระบุ (index)</summary>
    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= storedItems.Length) return;
        var slot = storedItems[slotIndex];
        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot.item = null;
            slot.amount = 0;
            slot.sellPrice = 0;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Sell Support (สำหรับ TradeManager)
    // ─────────────────────────────────────────────────────────────

    /// <summary>รถเข็นว่างหรือไม่</summary>
    public bool IsEmpty
    {
        get
        {
            foreach (var slot in storedItems)
                if (slot.HasItem) return false;
            return true;
        }
    }

    /// <summary>รวมจำนวน item ทั้งหมดในรถ</summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var slot in storedItems)
            if (slot.HasItem) total += slot.amount;
        return total;
    }

    /// <summary>รวมราคา sell ของ item ทั้งหมด (sum of sellPrice × amount)</summary>
    public int GetTotalValue()
    {
        int total = 0;
        foreach (var slot in storedItems)
            total += slot.TotalValue;
        return total;
    }

    /// <summary>ล้างทุก slot หลังขายสำเร็จ</summary>
    public void ClearAll()
    {
        for (int i = 0; i < storedItems.Length; i++)
        {
            storedItems[i].item = null;
            storedItems[i].amount = 0;
            storedItems[i].sellPrice = 0;
        }
    }

    /// <summary>ดึง item ทั้งหมด (return list สำหรับ TradeManager หรือ UI)</summary>
    public List<CartSlot> GetAllItems()
    {
        var items = new List<CartSlot>();
        foreach (var slot in storedItems)
        {
            if (slot.HasItem)
                items.Add(slot);
        }
        return items;
    }

    // ─────────────────────────────────────────────────────────────
    // IInteractable
    // ─────────────────────────────────────────────────────────────

    public void SetHighlighted(bool isHighlighted) { }
    public Transform GetTransform() => transform;
    public bool CanInteract() => true;
}
