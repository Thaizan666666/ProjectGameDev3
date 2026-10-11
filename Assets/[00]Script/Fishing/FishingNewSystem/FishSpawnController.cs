using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishingNewSystem
{
    /// <summary>
    /// คุมว่า cast ครั้งต่อไปจะได้ปลาชนิดไหน — baseline สุ่มถ่วงน้ำหนัก (percentRate) ข้ามทุก tier ยกเว้น Boss
    /// (Boss จับได้เฉพาะตอน Boss-only mode เปิดอยู่ ตามที่ตั้งใจว่า Boss ต้องรับ Quest ก่อนถึงจับได้)
    /// รองรับ 2 override:
    ///   - ForceNextCatches: บังคับ N รอบถัดไปให้เป็นปลาชนิดที่กำหนด (เช่น ใช้ตอน tutorial/quest สะสมปลาชนิดเดียว)
    ///   - SetBossOnlyMode: เปิด/ปิดโหมดจับได้เฉพาะ Boss (เรียกจาก Event ตอนตัดฉากเข้าสู้ Boss)
    /// </summary>
    public class FishSpawnController : MonoBehaviour
    {
        public static FishSpawnController instance;

        [Tooltip("FishDatabase ในฉาก — ปล่อยว่างได้ auto-find ให้")]
        [SerializeField] private FishDatabase fishDatabase;

        [Header("Debug Override (ตั้งจาก Inspector ตอนเทส/design)")]
        [Tooltip("ติ๊กแล้วตกได้เฉพาะ Boss ทันที ไม่ติ๊กกลับเป็นสุ่มปกติ (เทียบเท่าเรียก SetBossOnlyMode ผ่าน code)")]
        [SerializeField] private bool bossOnly;
        [Tooltip("เลือกปลาที่จะบังคับให้ได้ N รอบถัดไป — ลาก FishStats asset มาใส่")]
        [SerializeField] private TableForge.Fish.FishStats forceThisFish;
        [Tooltip("จำนวนรอบที่จะบังคับเป็นปลาด้านบน")]
        [SerializeField] private int forceRounds = 5;
        [Tooltip("ติ๊กเพื่อ apply การบังคับด้านบน (one-shot — ติ๊กแล้วจะ auto ปลดให้เอง ไม่ต้องไปปลดเอง)")]
        [SerializeField] private bool applyForce;

        private FishData forcedFish;
        private int forcedRemaining;
        private bool bossOnlyMode;
        private bool prevBossOnly;
        private FishZone currentZone; // โซนที่ Player ยืนอยู่ตอนนี้ (ตั้ง/เคลียร์จาก NewFishZoneTrigger) — null = ใช้ FishDatabase ทั้งหมด (ไม่จำกัดโซน)

        /// <summary>เรียกจาก NewFishZoneTrigger ตอน Player เดินเข้าโซนตกปลา</summary>
        public void SetCurrentZone(FishZone zone) => currentZone = zone;

        /// <summary>เรียกจาก NewFishZoneTrigger ตอน Player เดินออกจากโซน — เช็ค zone ตรงกันก่อนเคลียร์ กันเคส เข้าโซน B ทับโซน A แล้วออกจาก A ไปลบ B ทิ้งผิดตัว</summary>
        public void ClearCurrentZone(FishZone zone)
        {
            if (currentZone == zone) currentZone = null;
        }

        private void Awake()
        {
            instance = this;
            if (fishDatabase == null) fishDatabase = FindFirstObjectByType<FishDatabase>();
        }

        private void Update()
        {
            // Boss Only toggle จาก Inspector — ตรวจจับการเปลี่ยนค่าแล้วค่อยเรียก (ไม่เรียกทุกเฟรมโดยไม่จำเป็น)
            if (bossOnly != prevBossOnly)
            {
                SetBossOnlyMode(bossOnly);
                prevBossOnly = bossOnly;
            }

            // Force fish ชั่วคราวจาก Inspector — ติ๊ก applyForce ครั้งเดียว (one-shot) แล้ว reset ให้อัตโนมัติ
            if (applyForce)
            {
                applyForce = false;
                if (forceThisFish != null && fishDatabase != null)
                {
                    FishData fd = fishDatabase.GetByName(forceThisFish.fishName);
                    if (fd != null) ForceNextCatches(fd, forceRounds);
                    else Debug.LogWarning($"[FishSpawnController] หา FishData ของ {forceThisFish.fishName} ใน FishDatabase ไม่พบ");
                }
            }
        }

        /// <summary>บังคับ N รอบถัดไปให้ได้ปลาตัวนี้แน่นอน ไม่สุ่ม (นับถอยทีละ 1 ทุกครั้งที่ GetNextFish ถูกเรียก)</summary>
        public void ForceNextCatches(FishData fish, int count)
        {
            forcedFish = fish;
            forcedRemaining = Mathf.Max(0, count);
        }

        /// <summary>ยกเลิก force ก่อนครบรอบ (ถ้าต้องการ)</summary>
        public void ClearForcedCatches()
        {
            forcedFish = null;
            forcedRemaining = 0;
        }

        /// <summary>เปิด/ปิดโหมดจับได้เฉพาะ Boss — เรียกจาก Event ตอนตัดฉากเข้าสู้ Boss</summary>
        public void SetBossOnlyMode(bool enabled)
        {
            bossOnlyMode = enabled;
            Debug.Log($"[FishSpawnController] Boss-only mode -> {enabled}");
        }

        public bool IsBossOnlyMode => bossOnlyMode;

        /// <summary>เรียกตอนปลากินเบ็ด (bite) — คืนปลาที่จะใช้ในรอบนี้ ตามลำดับ priority: forced > boss-only > สุ่มปกติ</summary>
        public FishData GetNextFish()
        {
            if (forcedRemaining > 0 && forcedFish != null)
            {
                forcedRemaining--;
                return forcedFish;
            }

            if (fishDatabase == null)
            {
                Debug.LogWarning("[FishSpawnController] ไม่มี FishDatabase ในฉาก — คืน null");
                return null;
            }

            if (bossOnlyMode)
            {
                return currentZone != null ? currentZone.GetRandomFish(FishTier.Boss) : fishDatabase.GetRandomFish(FishTier.Boss);
            }

            // อยู่ในโซนตกปลา -> สุ่มจากปลาที่โซนนี้กำหนดไว้เท่านั้น (weighted ตาม percentRate เหมือนเดิม)
            if (currentZone != null)
            {
                return currentZone.GetRandomFish();
            }

            return GetRandomNonBossFish();
        }

        private FishData GetRandomNonBossFish()
        {
            List<FishData> pool = fishDatabase.GetAll().Where(f => f.fishTier != FishTier.Boss).ToList();
            if (pool.Count == 0)
            {
                Debug.LogWarning("[FishSpawnController] ไม่มีปลา (นอก Boss) ใน FishDatabase เลย");
                return null;
            }
            return WeightedRandomPicker.Pick(pool, f => f.percentRate);
        }
    }
}
