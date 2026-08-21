using UnityEngine;

namespace WaterSystem
{
    /// <summary>
    /// คำนวณ Gerstner wave แบบเดียวกับ GerstnerWaves.hlsl (ฝั่ง GPU/Shader Graph)
    /// เพื่อให้ค่าความสูง/ความเอียงของคลื่นตรงกัน 100% ระหว่างสิ่งที่เห็นบนจอ กับแรงลอยตัวของเรือ
    /// ไม่ใช้ AsyncGPUReadback เพราะมี latency — คำนวณสูตรซ้ำบน CPU แทน (ตามแนวทาง Boat Attack)
    /// รองรับทั้ง URP และ HDRP เพราะไม่ผูกกับ pipeline-specific API
    /// </summary>
    public static class GerstnerWaveMath
    {
        /// <summary>
        /// คำนวณ offset ตำแหน่ง (แนวนอน+แนวตั้ง) และ normal ของผิวน้ำ ณ ตำแหน่งโลกที่กำหนด
        /// สูตรตรงกับฟังก์ชัน GerstnerWaves_float ใน GerstnerWaves.hlsl
        /// </summary>
        /// <param name="worldPos">ตำแหน่งโลก (x, z ใช้คำนวณ, y ไม่ใช้)</param>
        /// <param name="time">เวลา (ควรใช้ Time.timeAsDouble หรือ Time.time ให้ตรงกับ shader ที่ผูกกับ Time node)</param>
        /// <param name="waves">ชุดคลื่น (amplitude, direction, wavelength, omniDir) — ต้องเป็นชุดเดียวกับที่ WaterManager ส่งเข้า waveData[]</param>
        /// <param name="offset">ผลลัพธ์: ตำแหน่ง offset ของผิวน้ำ</param>
        /// <param name="normal">ผลลัพธ์: normal ของผิวน้ำ (ใช้คำนวณความเอียงเรือ)</param>
        public static void SampleWaves(
            Vector3 worldPos,
            float time,
            Wave[] waves,
            out Vector3 offset,
            out Vector3 normal)
        {
            offset = Vector3.zero;
            Vector3 normalSum = Vector3.zero;

            int count = Mathf.Min(waves.Length, 10);
            for (int i = 0; i < count; i++)
            {
                GerstnerWaveSingle(waves[i], worldPos, time, ref offset, ref normalSum);
            }

            normal = normalSum.sqrMagnitude > 0.0001f ? normalSum.normalized : Vector3.up;
        }

        /// <summary>
        /// ค่า foam (0..1) ณ ตำแหน่งโลกที่กำหนด — ตรงกับ output "Foam" ของ GerstnerWaves_float ใน .hlsl
        /// มาก = คลื่นกำลังม้วน/แตกเป็นฟองที่ตำแหน่งนี้ (ใช้ทำ wake foam ที่หัวเรือใน BoatWakeFoam)
        /// </summary>
        public static float SampleFoam(Vector3 worldPos, float time, Wave[] waves)
        {
            float dDxDx = 0f, dDxDz = 0f, dDzDz = 0f;

            int count = Mathf.Min(waves.Length, 10);
            for (int i = 0; i < count; i++)
            {
                AccumulateJacobian(waves[i], worldPos, time, ref dDxDx, ref dDxDz, ref dDzDz);
            }

            float jacobian = (1f + dDxDx) * (1f + dDzDz) - dDxDz * dDxDz;
            return Mathf.Clamp01(1f - jacobian);
        }

        private static void GerstnerWaveSingle(
            Wave wave,
            Vector3 worldPos,
            float time,
            ref Vector3 offset,
            ref Vector3 normalSum)
        {
            float amplitude = wave.amplitude;
            float wavelength = Mathf.Max(wave.wavelength, 0.001f);

            float w = 6.28318f / wavelength;              // wave number (2*PI / wavelength)
            float speed = Mathf.Sqrt(9.8f * w);            // dispersion relation (น้ำลึก)
            // Clamp กันยอดคลื่นพับทับตัวเอง (self-intersect) เวลา amplitude สูงเทียบกับ wavelength — เกิดเป็นเส้นคมตัดผิวน้ำ
            float steepness = Mathf.Clamp01(amplitude * w);

            Vector2 dir = WaveDirection(wave, worldPos);

            float phase = (dir.x * worldPos.x + dir.y * worldPos.z) * w + time * speed;
            float sinP = Mathf.Sin(phase);
            float cosP = Mathf.Cos(phase);

            offset.x += steepness * amplitude * dir.x * cosP;
            offset.z += steepness * amplitude * dir.y * cosP;
            offset.y += amplitude * sinP;

            normalSum.x += -dir.x * w * amplitude * cosP;
            normalSum.z += -dir.y * w * amplitude * cosP;
            normalSum.y += 1.0f - steepness * w * amplitude * sinP;
        }

        /// <summary>
        /// Jacobian ของ horizontal displacement (dDxDx, dDxDz, dDzDz) สะสมจากคลื่นลูกเดียว
        /// สูตรตรงกับส่วน jacobianSum ใน GerstnerWave() ของไฟล์ .hlsl
        /// </summary>
        private static void AccumulateJacobian(
            Wave wave,
            Vector3 worldPos,
            float time,
            ref float dDxDx,
            ref float dDxDz,
            ref float dDzDz)
        {
            float amplitude = wave.amplitude;
            float wavelength = Mathf.Max(wave.wavelength, 0.001f);

            float w = 6.28318f / wavelength;
            float speed = Mathf.Sqrt(9.8f * w);
            float steepness = Mathf.Clamp01(amplitude * w);

            Vector2 dir = WaveDirection(wave, worldPos);

            float phase = (dir.x * worldPos.x + dir.y * worldPos.z) * w + time * speed;
            float sinP = Mathf.Sin(phase);

            float k = steepness * amplitude * w * sinP;
            dDxDx += -k * dir.x * dir.x;
            dDxDz += -k * dir.x * dir.y;
            dDzDz += -k * dir.y * dir.y;
        }

        private static Vector2 WaveDirection(Wave wave, Vector3 worldPos)
        {
            if (wave.onmiDir)
            {
                Vector2 toPoint = new Vector2(worldPos.x, worldPos.z) - wave.origin;
                return toPoint.sqrMagnitude > 0.000001f ? toPoint.normalized : Vector2.right;
            }

            float rad = wave.direction * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    /// <summary>
    /// ใส่ component นี้กับเรือที่มี Rigidbody
    /// ใช้ 4 จุดลอย (หัว/ท้าย/ซ้าย/ขวา) เพื่อให้เรือเอียงและโยกตามรูปทรงคลื่นจริง ไม่ใช่แค่ขึ้นลงตรงๆ
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoatBuoyancy : MonoBehaviour
    {
        [Header("จุดลอยตัว (ลาก Transform ลูกของเรือมาใส่ เช่น หัว/ท้าย/ซ้าย/ขวา)")]
        public Transform[] floatPoints;

        [Header("ค่าฟิสิกส์การลอยตัว")]
        [Tooltip("แรงลอยตัวต่อจุด ยิ่งมากเรือยิ่งลอยเร็ว/แข็งกระด้าง")]
        public float buoyancyForce = 3f;
        [Tooltip("ความหน่วงตอนอยู่ในน้ำ ลดการโยกที่แรงเกินไป")]
        public float waterDrag = 0.6f;
        public float waterAngularDrag = 0.3f;
        [Tooltip("หน่วงความเร็วแนวตั้ง ณ จุดลอยตัวแต่ละจุดตอนจมน้ำ (ยิ่งมากยิ่งกันเรือ 'เด้ง/จมเกิน' equilibrium — แรงลอยตัวเฉยๆ ไม่มี damping จะทำให้เรือแกว่งขึ้นลงเป็นสปริงได้)")]
        public float buoyancyDamping = 2f;

        [Header("ป้องกันเรือปลิว (เวลาชนกับ kinematic collider เช่นตัวละคร KCC)")]
        [Tooltip("จำกัดความเร็วที่ PhysX ใช้ 'ดีด' เรือออกตอน resolve การซ้อนทับกับ collider อื่น (ค่า default ของ Unity แทบไม่จำกัด ทำให้เรือดีดแรงผิดปกติได้)")]
        public float maxDepenetrationVelocity = 2f;
        [Tooltip("เพดานความเร็วเชิงเส้นของเรือ กันแรงกระชากผิดปกติจากทุกแหล่ง")]
        public float maxLinearVelocity = 15f;
        [Tooltip("เพดานความเร็วเชิงมุม (rad/s) ของเรือ กันเรือหมุนติ้วผิดปกติ")]
        public float maxAngularVelocity = 5f;
        [Tooltip("ลาก Collider ของตัวละคร (เช่น Capsule Collider บน Player) มาใส่ — เรือจะไม่ชนทาง physics กับ collider เหล่านี้เลย ไม่ว่าจะกระโดด/ลงกระแทกแรงแค่ไหนก็ไม่มีแรงส่งถึงเรือ (KCC ยังคงยืน/เดินบนเรือได้ปกติ เพราะ KCC ตรวจพื้นด้วย raycast ของตัวเอง ไม่เกี่ยวกับ physics collision ตรงนี้)")]
        public Collider[] ignoreCollisionWith;

        [Header("อ้างอิงข้อมูลคลื่น")]
        [Tooltip("ถ้าใส่ WaterManager จะดึงชุดคลื่นจากตรงนี้แทน (แนะนำ — การันตี CPU/GPU sync กันทั้งฉาก)")]
        public WaterManager waterManager;
        [Tooltip("ใช้เมื่อไม่มี WaterManager ในฉาก — ต้องเป็นชุดคลื่นเดียวกับที่ส่งเข้า Shader (waveData)")]
        public Wave[] waves;
        [Tooltip("ระดับผิวน้ำปกติ (world Y) ใช้เมื่อไม่มี waterManager — ถ้ามี waterManager จะใช้ตำแหน่ง transform ของมันแทนค่านี้อัตโนมัติ")]
        public float waterLevel = 0f;

        private Rigidbody _rb;
        private float _defaultDrag;
        private float _defaultAngularDrag;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _defaultDrag = _rb.linearDamping;
            _defaultAngularDrag = _rb.angularDamping;
            _rb.maxDepenetrationVelocity = maxDepenetrationVelocity;

            if (ignoreCollisionWith != null)
            {
                Collider[] myColliders = GetComponentsInChildren<Collider>();
                foreach (var playerCol in ignoreCollisionWith)
                {
                    if (playerCol == null) continue;
                    foreach (var myCol in myColliders)
                    {
                        Physics.IgnoreCollision(myCol, playerCol, true);
                    }
                }
            }

            if (floatPoints == null || floatPoints.Length == 0)
                Debug.LogWarning($"[BoatBuoyancy] {name} ยังไม่ได้ตั้งค่า floatPoints — เรือจะไม่ลอยน้ำ");
        }

        private void FixedUpdate()
        {
            Wave[] activeWaves = waterManager != null ? waterManager.Waves : waves;

            if (floatPoints == null || floatPoints.Length == 0 || activeWaves == null || activeWaves.Length == 0)
                return;

            // ระดับผิวน้ำนิ่ง (ก่อนบวกคลื่น) — ใช้ตำแหน่ง Y ของ WaterManager ถ้ามี ไม่งั้น fallback เป็น waterLevel ที่ตั้งเอง
            float baseWaterLevel = waterManager != null ? waterManager.transform.position.y : waterLevel;

            float time = Time.time;
            int submergedCount = 0;

            foreach (var point in floatPoints)
            {
                if (point == null) continue;

                GerstnerWaveMath.SampleWaves(point.position, time, activeWaves, out var waveOffset, out var waveNormal);

                // ความสูงผิวน้ำจริง ณ ตำแหน่ง x,z ของจุดนี้
                float waterHeight = baseWaterLevel + waveOffset.y;
                float depth = waterHeight - point.position.y;

                if (depth > 0f)
                {
                    submergedCount++;
                    // แรงลอยตัวตามความลึกที่จม (Archimedes: ยิ่งจมลึก แรงยิ่งมาก แต่ clamp ไว้กันเรือดีดหลุด)
                    float force = Mathf.Clamp01(depth) * buoyancyForce / floatPoints.Length;

                    // หน่วงตามความเร็วแนวตั้ง ณ จุดนี้ (spring damping) กันเรือแกว่งเกิน equilibrium แล้วจมลึกกว่าปกติเป็นพักๆ
                    float pointVelocityY = _rb.GetPointVelocity(point.position).y;
                    float damping = -pointVelocityY * buoyancyDamping;

                    _rb.AddForceAtPosition(Vector3.up * (force + damping), point.position, ForceMode.Acceleration);

                    // เพิ่มแรงเอียงตาม normal ของคลื่น เพื่อให้เรือ "โยก" ตามความชันผิวน้ำ ไม่ใช่แค่ลอยตรงๆ
                    Vector3 tiltForce = Vector3.Cross(waveNormal, Vector3.up) * force * 0.15f;
                    _rb.AddForceAtPosition(tiltForce, point.position, ForceMode.Acceleration);
                }
            }

            // ปรับ drag ตามสัดส่วนจุดที่จมน้ำ (จมมาก = หน่วงมาก, ลอยพ้นน้ำหมด = ใช้ drag ปกติ)
            float submergedRatio = (float)submergedCount / floatPoints.Length;
            _rb.linearDamping = Mathf.Lerp(_defaultDrag, waterDrag, submergedRatio);
            _rb.angularDamping = Mathf.Lerp(_defaultAngularDrag, waterAngularDrag, submergedRatio);

            // กันเรือปลิว: clamp ความเร็วหลังใส่แรงทุกอย่างแล้ว เผื่อโดนแรงกระชากผิดปกติ (เช่น ตัวละครกระโดดชน collider)
            if (_rb.linearVelocity.sqrMagnitude > maxLinearVelocity * maxLinearVelocity)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxLinearVelocity;
            }
            if (_rb.angularVelocity.sqrMagnitude > maxAngularVelocity * maxAngularVelocity)
            {
                _rb.angularVelocity = _rb.angularVelocity.normalized * maxAngularVelocity;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (floatPoints == null) return;
            Gizmos.color = Color.cyan;
            foreach (var p in floatPoints)
            {
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.2f);
            }
        }
    }

    /// <summary>
    /// โครงสร้างคลื่นเดียวกับที่ใช้ใน WaterManager (namespace WaterSystem)
    /// ถ้าโปรเจกต์มี struct Wave อยู่แล้วจาก Boat Attack ให้ใช้ตัวนั้นแทน ไม่ต้องประกาศซ้ำ
    /// </summary>
    [System.Serializable]
    public struct Wave
    {
        public float amplitude;
        public float direction;   // องศา
        public float wavelength;
        public Vector2 origin;    // จุดกำเนิด (ใช้เมื่อ onmiDir = true)
        public bool onmiDir;

        public Wave(float amplitude, float direction, float wavelength, Vector2 origin, bool onmiDir)
        {
            this.amplitude = amplitude;
            this.direction = direction;
            this.wavelength = wavelength;
            this.origin = origin;
            this.onmiDir = onmiDir;
        }
    }
}
