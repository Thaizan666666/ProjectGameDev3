using System.Collections.Generic;
using UnityEngine;

public class FishSlotManager : MonoBehaviour
{
    [SerializeField] private List<FishSlot> slots = new();

    public IReadOnlyList<FishSlot> Slots => slots;
    public int SlotCount => slots.Count;

    public void AddSlot(Fish fish = null, FishZone zone = null) =>
        slots.Add(new FishSlot { targetFish = fish, zone = zone });

    public bool RemoveSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return false;
        slots.RemoveAt(index);
        return true;
    }

    public void RemoveLastSlot()
    {
        if (slots.Count == 0) return;
        slots.RemoveAt(slots.Count - 1);
    }

    // UI Button entry point
    public void RandomizeFish(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].TryRandomize();
    }
}
