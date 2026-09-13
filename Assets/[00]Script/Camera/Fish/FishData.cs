using System;
using UnityEngine;

[Serializable]
public class FishData
{
    public FishName fishName;
    public FishTier fishTier;
    public FishSize fishSize;

    public int minWeight;
    public int maxWeight;

    public float percentRate;

    public int Price;
    public Sprite Icon;
    public GameObject Prefab;

    public ItemSO linkedItem; // คัดลอกมาจาก FishStats ตอนสร้าง FishData

    [HideInInspector]
    public int fishID => (int)fishName;

    private static FishDatabase _cachedDB;

    public static FishData FindByLinkedItem(ItemSO item)
    {
        if (item == null) return null;

        if (_cachedDB == null)
            _cachedDB = UnityEngine.Object.FindFirstObjectByType<FishDatabase>();

        if (_cachedDB == null) return null;

        foreach (var fish in _cachedDB.GetAll())
        {
            if (fish.linkedItem == item) return fish;
        }
        return null;
    }
}

