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
        public FishSize fishSize;

        [Header("Reel Minigame Tuning")]
        [Tooltip("ความเร็ว drift ของปลาตอน reel minigame (min-max) — สุ่มใหม่ทุกครั้งที่ตกปลาตัวนี้ ไม่ให้เล่นซ้ำเหมือนเดิมตลอด")]
        public float driftSpeedMin = 0.2f;
        public float driftSpeedMax = 0.3f;
        [Tooltip("จำนวนรอบ QTE ที่ต้องดึงให้ครบถึงจะจับได้ — ส่งเข้า FishingReelMinigame.roundsRequired")]
        public int roundsRequired = 3;

        [Header("Inventory Integration")]
        public ItemSO linkedItem; // ItemSO ที่ตรงกับปลาตัวนี้ใน Inventory

        public int fishID => (int)fishName;
    }
}

