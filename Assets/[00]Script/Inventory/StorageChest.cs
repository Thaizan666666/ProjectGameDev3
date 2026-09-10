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

    private static GameObject chestUI;
    private static Slot[] slots;

    private bool isOpen;

    private void Awake()
    {
        storedItems = new ChestItem[chestSize];

        for(int i = 0; i< chestSize; i++)
        {
            storedItems[i] = new ChestItem();
        }

        if(chestUI == null)
        {
            chestUI = Inventory.instance.chestUI.gameObject;
            slots = chestUI.GetComponentsInChildren<Slot>(true);

            chestUI.SetActive(false);
        }
    }

    public void Interact()
    {
        if(!isOpen)
            OpenChest();
        else
            CloseChest();
    }

    void OpenChest()
    {
        Inventory.instance.ToggleInventory();
        isOpen = true;
        chestUI.SetActive(true);

        for(int i = 0; i< slots.Length; i++)
        {
            var data = storedItems[i];

            if(data.item != null)
                slots[i].SetItem(data.item, data.amount);
            else
                slots[i].ClearSlot();
        }
    }

    public void CloseChest()
    {
        if(!isOpen) return;
        isOpen = false;

        for(int i = 0; i< slots.Length; i++)
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

        chestUI.SetActive(false);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        throw new System.NotImplementedException();
    }

    public Transform GetTransform()
    {
        throw new System.NotImplementedException();
    }

    public bool CanInteract()
    {
        throw new System.NotImplementedException();
    }
}
