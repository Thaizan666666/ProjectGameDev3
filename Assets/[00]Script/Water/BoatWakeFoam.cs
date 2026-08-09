using UnityEngine;

namespace WaterSystem
{
    /// <summary>
    /// ควบคุม Particle System ให้เกิด foam/whitewater ที่หัวเรือ
    /// เงื่อนไข: (1) เรือแล่นเร็วพอ หรือ (2) จุดหัวเรือตกอยู่บนยอดคลื่นที่กำลังม้วน/แตก
    ///          (ใช้ GerstnerWaveMath.SampleFoam ค่าเดียวกับ Foam output ของ GerstnerWaves.hlsl)
    /// ต้องตั้งค่า ParticleSystem เอง (shape/texture/สี) — สคริปต์นี้คุมแค่ emission rate
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BoatWakeFoam : MonoBehaviour
    {
        [Header("อ้างอิง")]
        [Tooltip("จุดหัวเรือที่จะเกิด foam/wake ถ้าไม่ใส่จะใช้ตำแหน่งของ GameObject นี้เอง")]
        public Transform bowPoint;
        [Tooltip("Rigidbody ของเรือ ใช้ความเร็วมาคุมความหนาแน่นของ wake foam")]
        public Rigidbody boatRigidbody;
        [Tooltip("ถ้ามี WaterManager ในฉากจะดึงชุดคลื่นจากตรงนี้แทน (แนะนำ)")]
        public WaterManager waterManager;
        [Tooltip("ใช้เมื่อไม่มี WaterManager ในฉาก — ต้องเป็นชุดคลื่นเดียวกับที่ BoatBuoyancy ใช้")]
        public Wave[] waves;

        [Header("เงื่อนไข wake จากความเร็วเรือ")]
        [Tooltip("ความเร็วเรือขั้นต่ำ (m/s) ที่เริ่มเกิด wake foam")]
        public float minSpeedForWake = 1.5f;
        [Tooltip("ความเร็วเรือที่ foam หนาแน่นสุด (m/s)")]
        public float maxSpeedForWake = 8f;

        [Header("เงื่อนไข foam จากยอดคลื่นธรรมชาติ")]
        [Tooltip("ค่า Foam (0..1) จาก GerstnerWaveMath.SampleFoam ที่เริ่มนับว่าเป็นยอดคลื่นแตก แม้เรือจอดนิ่ง")]
        [Range(0f, 1f)] public float waveFoamThreshold = 0.6f;

        [Header("Emission")]
        public float minEmissionRate = 0f;
        public float maxEmissionRate = 40f;

        private ParticleSystem _ps;
        private ParticleSystem.EmissionModule _emission;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _emission = _ps.emission;
            if (bowPoint == null) bowPoint = transform;
        }

        private void LateUpdate()
        {
            Wave[] activeWaves = waterManager != null ? waterManager.Waves : waves;

            if (activeWaves == null || activeWaves.Length == 0)
            {
                _emission.rateOverTime = 0f;
                return;
            }

            if (bowPoint != transform)
                transform.position = bowPoint.position;

            float speed = boatRigidbody != null ? boatRigidbody.linearVelocity.magnitude : 0f;
            float speedT = Mathf.InverseLerp(minSpeedForWake, maxSpeedForWake, speed);

            float waveFoam = GerstnerWaveMath.SampleFoam(bowPoint.position, Time.time, activeWaves);
            float naturalFoamT = waveFoam >= waveFoamThreshold
                ? Mathf.InverseLerp(waveFoamThreshold, 1f, waveFoam)
                : 0f;

            float intensity = Mathf.Max(speedT, naturalFoamT);
            _emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, intensity);
        }
    }
}
