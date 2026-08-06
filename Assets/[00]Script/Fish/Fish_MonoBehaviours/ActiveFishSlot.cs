using UnityEngine;

// Remembers which slot the player is standing at. UI Button triggers the actual roll.
public class ActiveFishSlot : MonoBehaviour
{
    [SerializeField] private FishSlotManager slotManager;

    private int currentIndex = -1;

    public void SetActiveSlot(int index)
    {
        currentIndex = index;
        Debug.Log($"Current Zone number is : {currentIndex}");
    }

    public void ClearActiveSlot(int index)
    {
        if (currentIndex == index) currentIndex = -1;

        Debug.Log($"Current Zone number is : {currentIndex}");
    }

    // Bind to UI Button OnClick
    public void RandomizeActiveSlot()
    {
        if (currentIndex < 0)
        {
            Debug.LogWarning("No active fishing spot");
            return;
        }

        if (slotManager == null)
        {
            Debug.LogWarning("Missing FishSlotManager");
            return;
        }

        slotManager.RandomizeFish(currentIndex);
    }
}
