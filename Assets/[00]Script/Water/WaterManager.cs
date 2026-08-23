using UnityEngine;

namespace WaterSystem
{
    /// <summary>
    /// เจ้าของข้อมูลคลื่น (Wave[]) เพียงจุดเดียวของฉาก — เทียบเท่า Water.cs แบบย่อจาก Unity Boat Attack
    /// หน้าที่หลัก:
    ///   1. ส่งค่าคลื่นเข้า GPU ผ่าน global shader properties (waveData / _WaveCount) ให้ GerstnerWaves.hlsl ใช้
    ///   2. เป็นแหล่งข้อมูลเดียวกันให้ BoatBuoyancy / BoatWakeFoam ดึงไปคำนวณฝั่ง CPU
    ///   3. (ไม่บังคับ) ปรับ amplitude/direction ของคลื่นตาม Wind Zone ในฉาก
    /// การมี "single source of truth" นี้คือสิ่งที่ทำให้ CPU (buoyancy) กับ GPU (shader) เห็นคลื่นตรงกันเสมอ
    /// </summary>
    public class WaterManager : MonoBehaviour
    {
        [ExecuteAlways]
        [Header("ชุดคลื่นพื้นฐาน (ก่อนถูก Wind Zone ปรับค่า)")]
        [SerializeField]
        private Wave[] baseWaves =
        {
            new Wave(0.4f, 20f, 22f, Vector2.zero, false),
            new Wave(0.35f, 70f, 14f, Vector2.zero, false),
            new Wave(0.25f, 130f, 9f, Vector2.zero, false)
        };

        [Header("Wind Zone (ไม่บังคับ — ไม่ใส่ก็ใช้ baseWaves ตรงๆ)")]
        [Tooltip("ลาก WindZone (Directional) ในฉากมาใส่ เพื่อให้แรง/ทิศทางลมควบคุมคลื่น")]
        [SerializeField] private WindZone windZone;

        [Tooltip("ช่วงค่า windMain ที่จะแม็ปเป็นตัวคูณ amplitude (0..1 ก่อนคูณกับ amplitude multiplier)")]
        [SerializeField] private float windMainMin = 0f;
        [SerializeField] private float windMainMax = 20f;

        [Tooltip("ตัวคูณ amplitude ต่ำสุด/สูงสุดตามแรงลม")]
        [SerializeField] private float ampMultiplierMin = 0.4f;
        [SerializeField] private float ampMultiplierMax = 1.6f;

        [Tooltip("windTurbulence คูณค่านี้ = ความสุ่มของทิศทางคลื่นแต่ละลูก (องศา)")]
        [SerializeField] private float turbulenceDirectionJitterDeg = 25f;

        private Wave[] _runtimeWaves;
        private Vector4[] _waveDataBuffer; // [0..9] amplitude/direction/wavelength/omni, [10..19] origin.xy

        /// <summary>ชุดคลื่นปัจจุบัน (หลังปรับตาม Wind Zone) — ให้ BoatBuoyancy/BoatWakeFoam ดึงไปใช้</summary>
        public Wave[] Waves => _runtimeWaves;

        private void Awake()
        {
            _runtimeWaves = new Wave[baseWaves.Length];
            _waveDataBuffer = new Vector4[20];
        }

        private void LateUpdate()
        {
            UpdateRuntimeWaves();
            PushToGpu();
        }

        private void UpdateRuntimeWaves()
        {
            float ampMultiplier = 1f;
            float windDirectionDeg = 0f;
            float turbulenceJitter = 0f;
            bool hasWind = windZone != null;

            if (hasWind)
            {
                float windT = Mathf.InverseLerp(windMainMin, windMainMax, windZone.windMain);
                ampMultiplier = Mathf.Lerp(ampMultiplierMin, ampMultiplierMax, windT);

                Vector3 fwd = windZone.transform.forward;
                windDirectionDeg = Mathf.Atan2(fwd.z, fwd.x) * Mathf.Rad2Deg;
                turbulenceJitter = windZone.windTurbulence * turbulenceDirectionJitterDeg;
            }

            for (int i = 0; i < baseWaves.Length; i++)
            {
                Wave w = baseWaves[i];
                w.amplitude *= ampMultiplier;

                if (hasWind && !w.onmiDir)
                {
                    // Perlin noise ต่อคลื่นแต่ละลูก กันไม่ให้ทุกลูกสั่นทิศพร้อมกันเป๊ะ ๆ
                    float noise = Mathf.PerlinNoise(Time.time * 0.3f, i * 17.31f) - 0.5f;
                    w.direction = windDirectionDeg + noise * 2f * turbulenceJitter;
                }

                _runtimeWaves[i] = w;
            }
        }

        private void PushToGpu()
        {
            int count = Mathf.Min(_runtimeWaves.Length, 10);
            for (int i = 0; i < count; i++)
            {
                Wave w = _runtimeWaves[i];
                _waveDataBuffer[i] = new Vector4(w.amplitude, w.direction, w.wavelength, w.onmiDir ? 1f : 0f);
                _waveDataBuffer[i + 10] = new Vector4(w.origin.x, w.origin.y, 0f, 0f);
            }

            Shader.SetGlobalVectorArray("waveData", _waveDataBuffer);
            Shader.SetGlobalInt("_WaveCount", count);
        }
    }
}
