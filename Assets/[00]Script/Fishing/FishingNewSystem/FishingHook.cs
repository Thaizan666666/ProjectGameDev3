// ─────────────────────────────────────────────────────────────
// FishingHook.cs
// ติดที่ hookPrefab — เช็คว่า Hook ชนอะไรครั้งแรกเป็น Layer "Water" ไหม
// แล้ว report ผลกลับไปให้ FishingCastController ตัดสิน state ต่อ
// รองรับทั้งกรณี Water เป็น solid collider (OnCollisionEnter)
// และกรณีเป็น trigger volume (OnTriggerEnter) เผื่อ scene ตั้งไว้แบบไหนก็ได้
// ถ้าโดน Water จริง จะหยุดตก + ลอยตามคลื่นจริง (ใช้สูตรเดียวกับ BoatBuoyancy/GerstnerWaves.hlsl
// ผ่าน WaterManager ให้ตรงกับที่เห็นบนจอ ไม่ใช่แค่ลอยนิ่ง)
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using WaterSystem;

namespace FishingNewSystem
{
    [RequireComponent(typeof(Collider))]
    public class FishingHook : MonoBehaviour
    {
        [Tooltip("ระยะสูง/ต่ำจากผิวน้ำ (บวก = ลอยเหนือผิว, ลบ = จมลงไปนิดหน่อย)")]
        [SerializeField] private float floatHeightOffset = -0.1f;
        [Tooltip("WaterManager ในฉาก — ดึงชุดคลื่นมาคำนวณให้ Hook ลอยขึ้น-ลงตามคลื่นจริง (สูตรเดียวกับ BoatBuoyancy) ปล่อยว่างได้ถ้าอยากให้ลอยนิ่งที่ผิวน้ำเฉย ๆ")]
        [SerializeField] private WaterManager waterManager;

        private FishingCastController owner;
        private bool resolved;
        private bool isFloating;
        private float baseWaterLevel;

        public void Init(FishingCastController castController)
        {
            owner = castController;
            resolved = false;
            isFloating = false;

            // Hook ตอนนี้เป็น Project prefab asset แล้ว ผูก reference ข้าม scene ไว้ล่วงหน้าไม่ได้
            // (prefab asset ชี้ไปที่ object ใน scene ใดที่หนึ่งเจาะจงไม่ได้) เลย auto-find ตอน spawn จริงแทน
            // ยังปล่อยให้ override ผ่าน Inspector ได้เผื่ออยากชี้ WaterManager ตัวเฉพาะ
            if (waterManager == null) waterManager = FindFirstObjectByType<WaterManager>();
        }

        private void OnCollisionEnter(Collision collision) => HandleHit(collision.gameObject, collision.collider);

        private void OnTriggerEnter(Collider other) => HandleHit(other.gameObject, other);

        private void HandleHit(GameObject hitObject, Collider hitCollider)
        {
            if (resolved) return;
            resolved = true;

            bool isWater = hitObject.layer == LayerMask.NameToLayer("Water");
            Debug.Log($"[FishingHook] ชน \"{hitObject.name}\" (Layer: {LayerMask.LayerToName(hitObject.layer)}) — isWater = {isWater}");

            if (isWater) StartFloating(hitCollider);

            owner?.OnHookLanded(isWater);
        }

        // ── หยุดตก, สลับไปคุมตำแหน่งเองทุกเฟรมแทน physics (isKinematic) กันไม่ให้ physics แย่งคุมตำแหน่ง ──
        private void StartFloating(Collider waterCollider)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // ระดับน้ำนิ่งก่อนบวกคลื่น — ใช้ตำแหน่ง WaterManager ถ้ามี (ตรงกับที่ BoatBuoyancy ใช้)
            // ไม่มีก็ fallback ไปใช้ขอบบนของ collider น้ำที่ชนจริง
            baseWaterLevel = waterManager != null ? waterManager.transform.position.y : waterCollider.bounds.max.y;

            isFloating = true;
        }

        // ใช้ LateUpdate ให้ทำงานหลัง Update อื่น ๆ ในเฟรมเดียวกัน กันตำแหน่งเด้งตามหลัง 1 เฟรม
        private void LateUpdate()
        {
            if (!isFloating) return;

            float waterHeight = baseWaterLevel;

            if (waterManager != null && waterManager.Waves != null && waterManager.Waves.Length > 0)
            {
                GerstnerWaveMath.SampleWaves(transform.position, Time.time, waterManager.Waves, out Vector3 waveOffset, out _);
                waterHeight += waveOffset.y;
            }

            Vector3 pos = transform.position;
            pos.y = waterHeight + floatHeightOffset;
            transform.position = pos;
        }
    }
}
