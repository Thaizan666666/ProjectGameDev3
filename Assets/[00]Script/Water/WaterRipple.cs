using UnityEngine;

namespace WaterSystem
{
    // ตำแหน่ง/ความเอียงของ ripple ขยับตามคลื่นจริง — สุ่มสูตรเดียวกับ GerstnerWaveMath ที่ BoatBuoyancy/FishingHook
    // ใช้ (single source of truth จาก WaterManager) กันไม่ให้ ripple ลอยผิดจากผิวน้ำที่เห็นบนจอ
    // ขนาด/ความจางของ ripple เอง ใช้ Size/Color over Lifetime ของ ParticleSystem ตั้งค่าไว้ใน prefab แล้ว ไม่ต้องคุมในสคริปต์นี้
    [RequireComponent(typeof(ParticleSystem))]
    public class WaterRipple : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.5f;
        [Tooltip("ระยะยกสูงจากผิวน้ำจริง (เมตร) — กัน ripple จม/clip เข้าไปในผิวน้ำ ให้ลอยโชว์เหนือผิวนิดหน่อย")]
        [SerializeField] private float heightOffset = 0.3f;

        private WaterManager waterManager;
        private float baseWaterLevel;
        private Vector3 anchorXZ;

        public void Init(WaterManager waterManager, float baseWaterLevel, Vector3 spawnPosition)
        {
            this.waterManager = waterManager;
            this.baseWaterLevel = baseWaterLevel;
            anchorXZ = spawnPosition;
            Destroy(gameObject, lifetime);
        }

        private void LateUpdate()
        {
            float y = baseWaterLevel + heightOffset;
            Vector3 horizontalDrift = Vector3.zero;
            Vector3 normal = Vector3.up;

            if (waterManager != null && waterManager.Waves != null && waterManager.Waves.Length > 0)
            {
                GerstnerWaveMath.SampleWaves(anchorXZ, Time.time, waterManager.Waves, out Vector3 waveOffset, out normal);
                y += waveOffset.y;
                horizontalDrift = new Vector3(waveOffset.x, 0f, waveOffset.z);
            }

            transform.position = new Vector3(anchorXZ.x + horizontalDrift.x, y, anchorXZ.z + horizontalDrift.z);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
    }
}
