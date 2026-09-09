using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public ItemSO keyItem;
    public GameObject hotbatObj;
    public GameObject inventorySlotParent;

    public Image dragIcon;

    private List<Slot> inventorySlots = new List<Slot>();
    private List<Slot> hotbarSlots = new List<Slot>();
    private List<Slot> allSlots = new List<Slot>();

    private Slot draggedSlot = null;
    private bool isDragging = false;

    private void Awake()
    {
        inventorySlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slot>());
        hotbarSlots.AddRange(hotbatObj.GetComponentsInChildren<Slot>());

        allSlots.AddRange(inventorySlots);
        allSlots.AddRange(hotbarSlots);
    }

    void Start()
    {
        AddItem(keyItem, 1);
    }

    void Update()
    {
        StartDrag();
        UpdateDragItemPosition();
        EndDrag();
    }

    public void AddItem(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;

        foreach(Slot slot in allSlots)
        {
            if(slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if(currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                    remaining -= amountToAdd;

                    if(remaining <= 0) return;
                }
            }
        }

        foreach(Slot slot in allSlots)
        {
            if (!slot.HasItem())
            {
                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace; 

                if(remaining <= 0) return;
            }
        }

        if(remaining > 0)
        {
            Debug.Log("Inventory is full, could not add " + remaining + " of " + itemToAdd.itemName);
        }
    }

    private void StartDrag()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Slot hovered = GetHoveredSlot();

            Debug.Log("Mouse left button has pressed.");

            if(hovered != null && hovered.HasItem())
            {
                Debug.Log("Is hovering.");
                draggedSlot = hovered;
                isDragging = true;

                //show drag item
                dragIcon.sprite = hovered.GetItem().icon;
                dragIcon.color = new Color(1, 1, 1, 0.5f);
                dragIcon.enabled = true;
            }
        }
    }

    private void EndDrag()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Debug.Log("Mouse left button has relesed.");

            Slot hovered = GetHoveredSlot();

            if(hovered != null)
            {
                HandleDrop(draggedSlot, hovered);

                dragIcon.enabled = false;

                draggedSlot = null;
                isDragging = false;
            }
        }
    }

    private Slot GetHoveredSlot()
    {
        foreach(Slot s in allSlots)
        {
            if(s.hovering) return s;
        }

        return null;
    }

    private void HandleDrop(Slot from, Slot to)
    {
        if(from == to) return;

        //Stacking
        if(to.HasItem() && to.GetItem() == from.GetItem())
        {
            int max = to.GetItem().maxStackSize;
            int space = max - to.GetAmount();

            if(space > 0)
            {
                int move = Mathf.Min(space, from.GetAmount());

                to.SetItem(to.GetItem(), to.GetAmount() + move);
                from.SetItem(from.GetItem(), from.GetAmount() - move);

                if(from.GetAmount() <= 0) from.ClearSlot();

                return;
            }
        }

        //Different Item
        if (to.HasItem())
        {
            ItemSO tempItem = to.GetItem();
            int tempAmount = to.GetAmount();

            to.SetItem(from.GetItem(), from.GetAmount());
            from.SetItem(tempItem, tempAmount);
            return;
        }

        //Empty Slot
        to.SetItem(from.GetItem(), from.GetAmount());
        from.ClearSlot();
    }

    private void UpdateDragItemPosition()
    {
        if (isDragging)
        {
            Debug.Log("updating position.");
            dragIcon.transform.position = Mouse.current.position.ReadValue();
        }
    }
}
