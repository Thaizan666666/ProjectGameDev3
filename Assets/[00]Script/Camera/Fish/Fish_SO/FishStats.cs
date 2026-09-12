using UnityEngine;
using System.Collections.Generic;

namespace TableForge.Fish
{
    [CreateAssetMenu(fileName = "FishStats", menuName = "Scriptable Objects/Fish Stats")]   
    public class FishStats : ScriptableObject
    {
        public FishName fishName;
        public FishTier fishTier;

        public int minWeight;
        public int maxWeight;

        public float percentRate;

        public int Price;
        public Sprite Icon;
        public GameObject Prefab;

        [Header("Inventory Integration")]
        public ItemSO linkedItem; // ItemSO ที่ตรงกับปลาตัวนี้ใน Inventory

        public int fishID => (int)fishName;
    }
}

