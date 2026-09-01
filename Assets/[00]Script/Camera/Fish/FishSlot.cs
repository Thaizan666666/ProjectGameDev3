using System;
using UnityEngine;

[Serializable]
public class FishSlot
{
    public Fish targetFish;
    public FishZone zone;

    [HideInInspector] public FishData lastResult;

    public bool TryRandomize()
    {
        if (zone == null) return false;

        FishData data = zone.GetRandomFish();
        if (data == null) return false;

        lastResult = data;
        targetFish?.SetData(data);
        zone.SetPendingFish(data);   // จองไว้ให้ตัวที่ตกจริงในโซนนี้ต้องเป็นปลาตัวเดียวกับที่โชว์ในตู้
        return true;
    }
}
