using UnityEngine;

namespace WaterSystem
{
    // แปะบน GameObject ที่ต้องการให้เกิด ripple/splash ตอนขยับแนวราบ (XZ) อยู่บนผิวน้ำ — ใช้ร่วมกันได้ทั้ง
    // Boat, Player, Hook (ไม่ต้องเขียนตรรกะ spawn ripple/splash แยกของแต่ละระบบ)
    // เช็คทุกเฟรม: ความเร็วแนวราบเกิน threshold + สูงใกล้ผิวน้ำพอ + cooldown หมดแล้ว -> spawn 1 ครั้ง
    public class WaterDisturbanceEmitter : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private WaterRipple ripplePrefab;
        [SerializeField] private GameObject splashPrefab;
        [Tooltip("ปล่อยว่างได้ auto-find ให้")]
        [SerializeField] private WaterManager waterManager;

        [Header("เงื่อนไขเกิด effect")]
        [Tooltip("ความเร็วแนวราบขั้นต่ำ (เมตร/วินาที) ที่นับว่า 'กำลังขยับ'")]
        [SerializeField] private float moveSpeedThreshold = 0.5f;
        [Tooltip("ระยะห่างแนวตั้งจากผิวน้ำจริง (เมตร) ที่ยังนับว่า 'อยู่บนผิวน้ำ' — กันเกิด effect ตอนอยู่บนบก/ลอยสูงเกินไป")]
        [SerializeField] private float waterSurfaceTolerance = 0.6f;
        [Tooltip("เวลาพักขั้นต่ำระหว่างการเกิด effect แต่ละครั้ง (วินาที) — ปรับได้ตามต้องการ")]
        [SerializeField] private float cooldown = 1f;

        private Vector3 lastPosition;
        private float cooldownTimer;

        private void Awake()
        {
            if (waterManager == null) waterManager = FindFirstObjectByType<WaterManager>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

            Vector3 current = transform.position;
            Vector2 horizontalDelta = new Vector2(current.x - lastPosition.x, current.z - lastPosition.z);
            float horizontalSpeed = Time.deltaTime > 0f ? horizontalDelta.magnitude / Time.deltaTime : 0f;
            lastPosition = current;

            if (horizontalSpeed < moveSpeedThreshold) return;
            if (cooldownTimer > 0f) return;

            float waterSurfaceY = SampleWaterSurfaceY(current);
            if (Mathf.Abs(current.y - waterSurfaceY) > waterSurfaceTolerance) return;

            Vector3 spawnPos = new Vector3(current.x, waterSurfaceY, current.z);
            Emit(spawnPos, waterSurfaceY);
            cooldownTimer = cooldown;
        }

        private float SampleWaterSurfaceY(Vector3 worldPos)
        {
            float y = waterManager != null ? waterManager.transform.position.y : worldPos.y;
            if (waterManager != null && waterManager.Waves != null && waterManager.Waves.Length > 0)
            {
                GerstnerWaveMath.SampleWaves(worldPos, Time.time, waterManager.Waves, out Vector3 waveOffset, out _);
                y += waveOffset.y;
            }
            return y;
        }

        private void Emit(Vector3 spawnPos, float baseWaterLevel)
        {
            if (ripplePrefab != null)
            {
                WaterRipple ripple = Instantiate(ripplePrefab, spawnPos, Quaternion.identity);
                ripple.Init(waterManager, baseWaterLevel, spawnPos);
            }

            if (splashPrefab != null)
            {
                Instantiate(splashPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
}
