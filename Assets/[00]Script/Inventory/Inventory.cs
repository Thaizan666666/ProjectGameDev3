using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.ComponentModel;
using KinematicCharacterController.Examples;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;
    [SerializeField] private CameraController cameraController;

    public ItemSO keyItem;
    public GameObject hotbatObj;
    public GameObject inventorySlotParent;
    public GameObject container;
    public Transform chestUI;
    public Transform cartUI;

    public Image dragIcon;

    private List<Slot> inventorySlots = new List<Slot>();
    private List<Slot> hotbarSlots = new List<Slot>();
    private List<Slot> allSlots = new List<Slot>();
    private List<Slot> chesUISlots = new List<Slot>();
    private List<Slot> CartUISlots = new List<Slot>();

    private Slot draggedSlot = null;
    private bool isDragging = false;
    public bool HasBag = false;

    private PlayerInputActions inputActions;
    private ExamplePlayer _examplePlayer;
    private StorageChest _openChest = null;
    private CartStorage _openCart = null;

    private void Awake()
    {
        instance = this;

        inventorySlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slot>());
        hotbarSlots.AddRange(hotbatObj.GetComponentsInChildren<Slot>());
        chesUISlots.AddRange(chestUI.GetComponentsInChildren<Slot>());
        CartUISlots.AddRange(cartUI.GetComponentsInChildren<Slot>());

        allSlots.AddRange(inventorySlots);
        allSlots.AddRange(hotbarSlots);

        chestUI.gameObject.SetActive(false);
        cartUI.gameObject.SetActive(false);

        inputActions = new PlayerInputActions();

        // Cache ExamplePlayer reference for input control
        _examplePlayer = FindFirstObjectByType<ExamplePlayer>();
    }

    void Start()
    {
        
    }

    void OnEnable()
    {
        inputActions.Interacting.Enable();
    }
    void OnDisable()
    {
        inputActions.Interacting.Disable();
    }

    void Update()
    {
        if (inputActions.Interacting.OpenInventory.WasPressedThisFrame())
        {
            Debug.Log("B has pressed.");
            HandleToggleAll();
        }

        // Only allow drag when inventory is open
        if (container.activeInHierarchy)
        {
            StartDrag();
            UpdateDragItemPosition();
            EndDrag();
        }
    }

    // ===== B Key: ปิดทุกอย่างถ้าเปิดอยู่ / เปิดแค่ inventory =====
    private void HandleToggleAll()
    {
        if (!HasBag) return;

        if (_openChest != null)
        {
            // Chest กำลังเปิด → ปิดทั้งคู่
            StorageChest chest = _openChest;
            chest.ForceClose();       // chest บันทึก item + reset isOpen
            CloseInventory();         // ปิด main inventory
        }
        else if (_openCart != null)
        {
            // Cart กำลังเปิด → sync ข้อมูล + ปิดทั้งคู่
            CartStorage cart = _openCart;
            _openCart.ForceClose();   // cart บันทึก item + reset isOpen
            CloseInventory();         // ปิด main inventory
        }
        else if (container.activeInHierarchy)
        {
            // Main inventory เปิดอยู่ (ไม่มี chest/cart) → ปิด
            CloseInventory();
        }
        else
        {
            // ทุกอย่างปิดอยู่ → เปิดแค่ main inventory
            OpenInventory();
        }
    }

    // ===== Open/Close Main Inventory (container + cursor + player input) =====
    public void OpenInventory(bool showInventorySlots = true)
    {
        container.SetActive(true);
        inventorySlotParent.SetActive(showInventorySlots);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (_examplePlayer != null)
        {
            _examplePlayer.SetControlEnabled(false);
            _examplePlayer.Character.StopAllInputs();
        }

        cameraController.SetCameraInput(false);
    }

    public void CloseInventory()
    {
        container.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (_examplePlayer != null)
            _examplePlayer.SetControlEnabled(true);

        cameraController.SetCameraInput(true);
    }

    public bool IsInventoryOpen() => container.activeInHierarchy;

    // ===== Chest UI Management =====
    #region Chest UI
    public void ShowChestUI(StorageChest chest)
    {
        _openChest = chest;
        chestUI.gameObject.SetActive(true);
        RefreshSlotLists();
    }

    public void HideChestUI()
    {
        _openChest = null;
        chestUI.gameObject.SetActive(false);
    }

    public StorageChest GetOpenChest() => _openChest;

    public Slot[] GetChestSlots() => chesUISlots.ToArray();
    #endregion

    // ===== Cart UI Management =====
    #region Cart UI
    public void ShowCartUI(CartStorage cart)
    {
        _openCart = cart;
        cartUI.gameObject.SetActive(true);
        RefreshSlotLists();
    }

    public void HideCartUI()
    {
        _openCart = null;
        cartUI.gameObject.SetActive(false);
    }

    public CartStorage GetOpenCart() => _openCart;

    public Slot[] GetCartSlots() => CartUISlots.ToArray();
    #endregion


    public void AddItem(ItemSO itemToAdd, int amount)
    {
        if (!HasBag)
        {
            AddItemToHotbar(itemToAdd, amount);
            return;
        }

        int remaining = amount;

        foreach (Slot slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if (currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                    remaining -= amountToAdd;

                    if (remaining <= 0) return;
                }
            }
        }

        foreach (Slot slot in allSlots)
        {
            if (!slot.HasItem())
            {
                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace;

                if (remaining <= 0) return;
            }
        }

        if (remaining > 0)
        {
            Debug.Log("Inventory is full, could not add " + remaining + " of " + itemToAdd.itemName);
        }
    }

    private void AddItemToHotbar(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;

        foreach (Slot slot in hotbarSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int max = itemToAdd.maxStackSize;
                int space = max - slot.GetAmount();
                if (space > 0)
                {
                    int move = Mathf.Min(space, remaining);
                    slot.SetItem(itemToAdd, slot.GetAmount() + move);
                    remaining -= move;
                    if (remaining <= 0) return;
                }
            }
        }

        foreach (Slot slot in hotbarSlots)
        {
            if (!slot.HasItem())
            {
                int place = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, place);
                remaining -= place;
                if (remaining <= 0) return;
            }
        }

        if (remaining > 0)
            Debug.LogWarning($"[Inventory] HotBar เต็ม — ไม่สามารถเก็บ {remaining}x {itemToAdd.itemName}");
    }


    private void StartDrag()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Slot hovered = GetHoveredSlot();
            Slot hovered = GetSlotUnderMouse();

            if (hovered != null && hovered.HasItem())
            {
                draggedSlot = hovered;
                isDragging = true;

                // show drag item
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
            Slot hovered = GetSlotUnderMouse(); // ใช้ raycast
            if (hovered != null && draggedSlot != null)
            {
                HandleDrop(draggedSlot, hovered);
            }
            else if (draggedSlot != null)
            {
                // ★ Snap back visual: item อยู่ใน draggedSlot เดิมอยู่แล้ว
                // แค่เล่น animation dragIcon กลับตำแหน่ง slot เดิม (ถ้าอยากดูสวย)
            }
            dragIcon.enabled = false;
            draggedSlot = null;
            isDragging = false;
        }
    }

    // private Slot GetHoveredSlot()
    // {
    //     foreach (Slot s in allSlots)
    //     {
    //         if (s.hovering) return s;
    //     }

    //     foreach (Slot s in chesUISlots)
    //     {
    //         if (s.hovering) return s;
    //     }

    //     foreach (Slot s in CartUISlots)
    //     {
    //         if (s.hovering) return s;
    //     }

    //     return null;
    // }

    private void HandleDrop(Slot from, Slot to)
    {
        if (from == to) return;

        // Stacking
        if (to.HasItem() && to.GetItem() == from.GetItem())
        {
            int max = to.GetItem().maxStackSize;
            int space = max - to.GetAmount();

            if (space > 0)
            {
                int move = Mathf.Min(space, from.GetAmount());

                to.SetItem(to.GetItem(), to.GetAmount() + move);
                from.SetItem(from.GetItem(), from.GetAmount() - move);

                if (from.GetAmount() <= 0) from.ClearSlot();

                return;
            }
        }

        // Different Item - swap
        if (to.HasItem())
        {
            ItemSO tempItem = to.GetItem();
            int tempAmount = to.GetAmount();

            to.SetItem(from.GetItem(), from.GetAmount());
            from.SetItem(tempItem, tempAmount);
            return;
        }

        // Empty Slot
        if (from != null)
        {
            to.SetItem(from.GetItem(), from.GetAmount());
            from.ClearSlot();
        }
    }

    private void UpdateDragItemPosition()
    {
        if (isDragging)
        {
            dragIcon.transform.position = Mouse.current.position.ReadValue();
        }
    }

    private Slot GetSlotUnderMouse()
    {
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = Mouse.current.position.ReadValue();
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<Slot>();
            if (slot != null) return slot;
        }
        return null;
    }

    void RefreshSlotLists()
    {
        chesUISlots.Clear();
        chesUISlots.AddRange(chestUI.GetComponentsInChildren<Slot>());
        CartUISlots.Clear();
        CartUISlots.AddRange(cartUI.GetComponentsInChildren<Slot>());
    }
}