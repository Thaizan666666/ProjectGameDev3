using UnityEngine;

[System.Serializable]
public class ChestItem
{
    public ItemSO item;
    public int amount;
}

public class StorageChest : MonoBehaviour, IInteractable
{
    public int chestSize = 15;
    public ChestItem[] storedItems;

    // ไม่ใช่ static แล้ว — แต่ละ chest จะดึง UI จาก Inventory instance
    private bool isOpen;

    private void Awake()
    {
        storedItems = new ChestItem[chestSize];

        for (int i = 0; i < chestSize; i++)
        {
            storedItems[i] = new ChestItem();
        }
    }

    public void Interact()
    {
        if (!isOpen)
            OpenChest();
        else
            CloseChest();
    }

    void OpenChest()
    {
        // ขอให้ Inventory เปิด main inventory + chest UI
        Inventory.instance.OpenInventory();
        Inventory.instance.ShowChestUI(this);

        isOpen = true;

        // โหลด item เข้า chest UI slots
        Slot[] slots = Inventory.instance.GetChestSlots();
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

    public void CloseChest()
    {
        if (!isOpen) return;

        // บันทึก item จาก chest UI กลับ array
        Slot[] slots = Inventory.instance.GetChestSlots();
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
            }
        }

        isOpen = false;

        // ขอให้ Inventory ปิดทั้ง chest UI + main inventory
        Inventory.instance.HideChestUI();
        Inventory.instance.CloseInventory();
    }

    // เรียกจาก Inventory.HandleToggleAll() — บันทึก item แล้วปิด UI ทั้งคู่
    public void ForceClose()
    {
        CloseChest();
    }

    public void SetHighlighted(bool isHighlighted) { }

    public Transform GetTransform() => transform;

    public bool CanInteract() => true;
}