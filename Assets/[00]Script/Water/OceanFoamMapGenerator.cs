using UnityEngine;
using UnityEngine.VFX;

namespace WaterSystem
{
    /// <summary>
    /// Dispatch FoamMap.compute ทุกเฟรม เขียนค่า foam (0..1) รอบๆ followTarget ลง RenderTexture
    /// แล้ว (ถ้าตั้ง targetVfx ไว้) push texture + ตำแหน่ง/ขนาด map เข้า VFX Graph ผ่าน exposed properties
    /// ให้ VFX Graph เอาไป sample ตัดสินใจ spawn ฟองคลื่น/wake เฉพาะจุดที่คลื่นกำลังแตกจริงๆ
    /// สูตร foam ตรงกับ GerstnerWaveMath.SampleFoam (CPU) และ GerstnerWaves.hlsl (ผิวน้ำที่เห็น) ทุกประการ
    /// เพราะอ่านชุดคลื่นจาก WaterManager.Waves ตัวเดียวกัน ไม่ได้คำนวณแยกเอง
    /// </summary>
    public class OceanFoamMapGenerator : MonoBehaviour
    {
        [Header("Wave Source")]
        [Tooltip("ลาก WaterManager ในฉากมาใส่ — จะดึงชุดคลื่นปัจจุบัน (Waves) จากตรงนี้")]
        public WaterManager waterManager;

        [Header("Follow")]
        [Tooltip("ตำแหน่งศูนย์กลาง map ให้ตามไป — ปกติลาก Main Camera หรือเรือมาใส่")]
        public Transform followTarget;
        [Tooltip("ความกว้างของ map รอบ followTarget (หน่วยโลก)")]
        public float mapSize = 60f;
        [Range(32, 512)]
        public int resolution = 256;
        [Tooltip("ระยะที่ followTarget ต้องขยับก่อน re-center map ใหม่ (กันขยับทุกพิกเซลทุกเฟรมโดยไม่จำเป็น)")]
        public float recenterThreshold = 5f;

        [Header("Compute")]
        public ComputeShader foamMapCompute;

        [Header("VFX Output (ไม่บังคับ — ไม่ใส่ก็ยังอ่าน FoamMap property นี้จากสคริปต์อื่นได้)")]
        public VisualEffect targetVfx;
        public string vfxTextureProperty = "FoamMap";
        public string vfxCenterProperty = "MapCenter";
        public string vfxSizeProperty = "MapSize";

        /// <summary>RenderTexture ผลลัพธ์ (R = foam 0..1) — VFX Graph หรือสคริปต์อื่น sample ต่อได้</summary>
        public RenderTexture FoamMap { get; private set; }
        /// <summary>จุดศูนย์กลาง (world XZ) ของ FoamMap ปัจจุบัน</summary>
        public Vector2 MapCenterWS { get; private set; }

        private int _kernel;
        private readonly Vector4[] _waveBuffer = new Vector4[20];

        private void OnEnable()
        {
            if (foamMapCompute == null)
            {
                Debug.LogWarning($"[OceanFoamMapGenerator] {name} ยังไม่ได้ตั้งค่า foamMapCompute");
                enabled = false;
                return;
            }

            FoamMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RHalf)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "OceanFoamMap"
            };
            FoamMap.Create();

            _kernel = foamMapCompute.FindKernel("CSMain");
            MapCenterWS = followTarget != null
                ? new Vector2(followTarget.position.x, followTarget.position.z)
                : Vector2.zero;
        }

        private void OnDisable()
        {
            if (FoamMap != null)
            {
                FoamMap.Release();
                FoamMap = null;
            }
        }

        private void Update()
        {
            if (waterManager == null || followTarget == null || FoamMap == null) return;

            Vector2 targetXZ = new Vector2(followTarget.position.x, followTarget.position.z);
            if ((targetXZ - MapCenterWS).sqrMagnitude > recenterThreshold * recenterThreshold)
                MapCenterWS = targetXZ;

            PushWaveData();

            foamMapCompute.SetTexture(_kernel, "_FoamMap", FoamMap);
            foamMapCompute.SetVector("_MapCenterWS", MapCenterWS);
            foamMapCompute.SetFloat("_MapSize", mapSize);
            foamMapCompute.SetInt("_MapResolution", resolution);
            foamMapCompute.SetFloat("_TimeValue", Time.time);

            int groups = Mathf.CeilToInt(resolution / 8f);
            foamMapCompute.Dispatch(_kernel, groups, groups, 1);

            if (targetVfx != null)
            {
                targetVfx.SetTexture(vfxTextureProperty, FoamMap);
                targetVfx.SetVector2(vfxCenterProperty, MapCenterWS);
                targetVfx.SetFloat(vfxSizeProperty, mapSize);
            }
        }

        private void PushWaveData()
        {
            Wave[] waves = waterManager.Waves;
            int count = waves != null ? Mathf.Min(waves.Length, 10) : 0;

            for (int i = 0; i < count; i++)
            {
                Wave w = waves[i];
                _waveBuffer[i] = new Vector4(w.amplitude, w.direction, w.wavelength, w.onmiDir ? 1f : 0f);
                _waveBuffer[i + 10] = new Vector4(w.origin.x, w.origin.y, 0f, 0f);
            }

            foamMapCompute.SetVectorArray("waveData", _waveBuffer);
            foamMapCompute.SetInt("_WaveCount", count);
        }
    }
}
