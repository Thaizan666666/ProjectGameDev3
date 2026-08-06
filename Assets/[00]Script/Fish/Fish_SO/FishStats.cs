using UnityEngine;
using System.Collections.Generic;

namespace TableForge.Fish
{
    [CreateAssetMenu(fileName = "FishStats", menuName = "Fish_SO/Fish Stats")]    
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

        public int fishID => (int)fishName;
    }
}
