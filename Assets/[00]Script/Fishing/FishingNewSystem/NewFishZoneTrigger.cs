using UnityEngine;

namespace FishingNewSystem
{
    /// <summary>
    /// ติด Trigger Collider คลุมจุดตกปลาของ FishZone — คนละตัวกับ FishZoneTrigger เดิม (ตัวนั้นผูก PlayerFishing ระบบเก่า)
    /// ตัวนี้ผูกกับ FishSpawnController ของระบบใหม่แทน ใช้ FishZone (asset/GameObject) ตัวเดียวกับระบบเก่าได้เลย ไม่ต้องสร้างโซนซ้ำ
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NewFishZoneTrigger : MonoBehaviour
    {
        [SerializeField] private FishZone zone;
        [Tooltip("ตรวจจับจาก Tag ของ Hook (ไม่ใช่ Layer) — โซนจะ active ตามตำแหน่งเบ็ดจริง ไม่ใช่ตำแหน่งที่ผู้เล่นยืน")]
        [SerializeField] private string hookTag = "Hook";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private bool IsHook(Collider other) => other.CompareTag(hookTag);

        private void OnTriggerEnter(Collider other)
        {
            if (!IsHook(other) || zone == null) return;
            if (FishSpawnController.instance != null)
                FishSpawnController.instance.SetCurrentZone(zone);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsHook(other) || zone == null) return;
            if (FishSpawnController.instance != null)
                FishSpawnController.instance.ClearCurrentZone(zone);
        }
    }
}
