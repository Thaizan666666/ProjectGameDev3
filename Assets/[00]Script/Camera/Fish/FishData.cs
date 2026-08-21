using System;
using UnityEngine;

[Serializable]
public class FishData
{
    public FishName fishName;
    public FishTier fishTier;

    public int minWeight;
    public int maxWeight;

    public float percentRate;

    public int Price;

    public Sprite Icon;
    public GameObject Prefab;

    [HideInInspector]
    public int fishID => (int)fishName;
}
