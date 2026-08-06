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
        return true;
    }
}
