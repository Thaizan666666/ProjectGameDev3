using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterSystem
{
    /// <summary>
    /// สร้าง mesh ทะเลแบบ camera-relative clipmap: วงแหวนสี่เหลี่ยมซ้อนกันรอบ followTarget (ปกติคือกล้อง)
    /// Ring 0 = สี่เหลี่ยมตันขนาดเล็ก/ละเอียดสุด, Ring ถัดไปเป็น "กรอบ" (frame) ล้อมรอบ ring ก่อนหน้า ใหญ่ขึ้น/หยาบลงเรื่อยๆ
    /// mesh ของแต่ละ ring ถูก generate "ครั้งเดียว" ตอน Rebuild — ทุกเฟรมแค่เลื่อนตำแหน่ง (snap ตาม grid cell ของ ring นั้น)
    /// เพื่อลด popping และไม่ต้อง regenerate vertex buffer ทุกเฟรม (สำคัญมากต่อ 60 FPS)
    /// ความสูงของคลื่นทั้งหมดคำนวณใน vertex shader (ดู Water.shadergraph / GerstnerWaves.hlsl) ไม่ใช่ที่นี่ —
    /// สคริปต์นี้สร้างแค่ "กระดาษแบน" ที่ตามกล้องไป ให้ shader มีพื้นที่พอสำหรับใส่คลื่นได้ไกลสุดลูกหูลูกตา
    /// </summary>
    [ExecuteAlways]
    public class OceanMeshGenerator : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("ตำแหน่งอ้างอิงให้ ring เลื่อนตาม — ปกติลาก Main Camera มาใส่")]
        public Transform followTarget;

        [Tooltip("ระดับน้ำคงที่ (world Y) — ควรตรงกับ WaterManager.transform.position.y เพื่อให้ buoyancy กับภาพตรงกัน")]
        public float waterLevelY = 0f;

        [Header("Ring Configuration")]
        [Tooltip("จำนวนวงแหวนซ้อนกัน (ring[0] = ในสุด/ละเอียดสุด, ring[last] = นอกสุด/หยาบสุด)")]
        [Range(1, 10)]
        public int ringCount = 6;

        [Tooltip("จำนวน vertex ต่อด้าน (ต่อ ring) — index ตรงกับ ring แต่ละวง ยิ่งเลขมากยิ่งละเอียด")]
        public int[] verticesPerRing = { 64, 64, 48, 48, 32, 32 };

        [Tooltip("ขนาดความกว้างของแต่ละ ring (หน่วยโลก) — ring ถัดไปต้อง 'ใหญ่กว่า' ring ก่อนหน้าเสมอ (แนะนำ ~2 เท่าต่อวง)")]
        public float[] ringSize = { 20, 40, 80, 160, 320, 640 };

        [Tooltip("ความหนา (จำนวน quad) ของกรอบ ring ที่ไม่ใช่ ring 0 — ใช้ค่าเดียวกันทุก ring เพื่อความเรียบง่าย")]
        [Range(1, 8)]
        public int frameThicknessSegments = 2;

        [Header("Material")]
        [Tooltip("Material ของ Water.shadergraph (ต้องใช้ material เดียวกันกับทุก ring เพื่อ static batching/instancing ได้)")]
        public Material oceanMaterial;

        private readonly List<Transform> _ringTransforms = new List<Transform>();
        private readonly List<Vector2> _lastSnapCenter = new List<Vector2>();

        private void OnEnable()
        {
            ValidateArrays();
            RebuildRings();
        }

        private void OnDisable()
        {
            ClearRings();
        }

        private void OnValidate()
        {
            // แค่ clamp ค่าที่พิมพ์ผิดพลาด ไม่ generate mesh ตรงนี้ (Unity ห้ามแก้ asset หนักๆ ระหว่าง serialization callback)
            ringCount = Mathf.Clamp(ringCount, 1, 10);
            ValidateArrays();
        }

        private void ValidateArrays()
        {
            ResizeArray(ref verticesPerRing, ringCount, 32);
            ResizeArray(ref ringSize, ringCount, 20f);

            for (int i = 0; i < ringCount; i++)
            {
                verticesPerRing[i] = Mathf.Max(2, verticesPerRing[i]);
            }

            // การันตี ring ถัดไปใหญ่กว่า ring ก่อนหน้าเสมอ (จำเป็นต่อการสร้าง frame mesh)
            for (int i = 1; i < ringCount; i++)
            {
                if (ringSize[i] <= ringSize[i - 1])
                    ringSize[i] = ringSize[i - 1] * 2f;
            }
        }

        private static void ResizeArray<T>(ref T[] array, int size, T defaultValue)
        {
            if (array != null && array.Length == size) return;
            var newArray = new T[size];
            int copyCount = array != null ? Mathf.Min(array.Length, size) : 0;
            for (int i = 0; i < copyCount; i++) newArray[i] = array[i];
            for (int i = copyCount; i < size; i++) newArray[i] = defaultValue;
            array = newArray;
        }

        /// <summary>เรียกจาก context menu หลังแก้ค่าใน Inspector เพื่อสร้าง mesh ใหม่ทั้งหมด</summary>
        [ContextMenu("Rebuild Rings")]
        public void RebuildRings()
        {
            ValidateArrays();
            ClearRings();

            for (int i = 0; i < ringCount; i++)
            {
                Mesh mesh = i == 0
                    ? BuildSolidRingMesh(verticesPerRing[i], ringSize[i])
                    : BuildFrameRingMesh(verticesPerRing[i], ringSize[i], ringSize[i - 1], frameThicknessSegments);

                var ringGO = new GameObject($"Ring_{i}");
                ringGO.transform.SetParent(transform, false);
                ringGO.hideFlags = HideFlags.DontSave;

                var mf = ringGO.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = ringGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = oceanMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                // Bound เดิมของ mesh ไม่ครอบคลุม displacement ที่ shader ทำใน vertex stage (คลื่นสูงได้หลายเมตร)
                // ขยาย bounds ตรงนี้กันโดน frustum culling ผิดพลาดตอนคลื่นสูงใกล้ขอบจอ
                var b = mesh.bounds;
                b.Expand(20f);
                mesh.bounds = b;

                _ringTransforms.Add(ringGO.transform);
                _lastSnapCenter.Add(new Vector2(float.NaN, float.NaN)); // บังคับ snap รอบแรกเสมอ
            }
        }

        private void ClearRings()
        {
            foreach (var t in _ringTransforms)
            {
                if (t == null) continue;
                var mesh = t.GetComponent<MeshFilter>()?.sharedMesh;
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
                if (mesh != null)
                {
                    if (Application.isPlaying) Destroy(mesh);
                    else DestroyImmediate(mesh);
                }
            }
            _ringTransforms.Clear();
            _lastSnapCenter.Clear();
        }

        private void LateUpdate()
        {
            if (followTarget == null || _ringTransforms.Count != ringCount) return;

            Vector3 targetPos = followTarget.position;

            for (int i = 0; i < ringCount; i++)
            {
                // cell size ของ ring นี้ — snap ตำแหน่งศูนย์กลาง ring ให้ตกบนกริดขนาดนี้เท่านั้น
                // ป้องกัน "popping": ผิวน้ำเป็นฟังก์ชันต่อเนื่องของตำแหน่งโลกอยู่แล้ว (คำนวณใน shader)
                // การขยับทีละ cell เต็มๆ แค่เปลี่ยน "ลายกริดสามเหลี่ยม" ไม่เปลี่ยนรูปทรงคลื่นที่เห็น
                float cell = ringSize[i] / Mathf.Max(1, verticesPerRing[i] - 1);
                float snappedX = Mathf.Round(targetPos.x / cell) * cell;
                float snappedZ = Mathf.Round(targetPos.z / cell) * cell;

                var last = _lastSnapCenter[i];
                if (!Mathf.Approximately(last.x, snappedX) || !Mathf.Approximately(last.y, snappedZ))
                {
                    _ringTransforms[i].position = new Vector3(snappedX, waterLevelY, snappedZ);
                    _lastSnapCenter[i] = new Vector2(snappedX, snappedZ);
                }
            }
        }

        // ---------- Mesh building (Job System + Burst) ----------

        private static Mesh BuildSolidRingMesh(int resolution, float size)
        {
            float half = size * 0.5f;
            return BuildGridMesh(resolution, resolution, -half, half, -half, half, $"OceanRing_Solid_{size}");
        }

        /// <summary>สร้าง mesh รูป "กรอบ" (สี่เหลี่ยมใหญ่เจาะรูสี่เหลี่ยมเล็กตรงกลาง) จาก 4 แถบ บน/ล่าง/ซ้าย/ขวา
        /// เพื่อไม่ให้พื้นที่ตรงกลาง (ที่ ring ละเอียดกว่าคลุมอยู่แล้ว) ถูก render ซ้ำซ้อน</summary>
        private static Mesh BuildFrameRingMesh(int resolution, float outerSize, float innerSize, int thicknessSegments)
        {
            float outerHalf = outerSize * 0.5f;
            float innerHalf = innerSize * 0.5f;
            float cell = outerSize / Mathf.Max(1, resolution - 1);
            int innerRes = Mathf.Max(2, Mathf.RoundToInt((innerHalf * 2f) / cell) + 1);
            int thicknessRes = thicknessSegments + 1;

            var combine = new List<CombineInstance>(4);

            void AddStrip(int cols, int rows, float xMin, float xMax, float zMin, float zMax)
            {
                Mesh strip = BuildGridMesh(cols, rows, xMin, xMax, zMin, zMax, "OceanRing_Strip");
                combine.Add(new CombineInstance { mesh = strip, transform = Matrix4x4.identity });
            }

            // บน / ล่าง: กว้างเต็มขอบนอก (รวมมุม)
            AddStrip(resolution, thicknessRes, -outerHalf, outerHalf, innerHalf, outerHalf);
            AddStrip(resolution, thicknessRes, -outerHalf, outerHalf, -outerHalf, -innerHalf);
            // ซ้าย / ขวา: สูงแค่ช่วงรูตรงกลาง (กันซ้อนมุมกับแถบบน/ล่าง)
            AddStrip(thicknessRes, innerRes, -outerHalf, -innerHalf, -innerHalf, innerHalf);
            AddStrip(thicknessRes, innerRes, innerHalf, outerHalf, -innerHalf, innerHalf);

            var frameMesh = new Mesh
            {
                name = $"OceanRing_Frame_{outerSize}",
                indexFormat = IndexFormat.UInt32
            };
            frameMesh.CombineMeshes(combine.ToArray(), true, false);
            frameMesh.RecalculateBounds();

            foreach (var ci in combine)
            {
                if (Application.isPlaying) Destroy(ci.mesh);
                else DestroyImmediate(ci.mesh);
            }

            return frameMesh;
        }

        /// <summary>สร้างกริดแบนราบ (rows x cols vertex) บนระนาบ XZ ด้วย Job System + Burst</summary>
        private static Mesh BuildGridMesh(int cols, int rows, float xMin, float xMax, float zMin, float zMax, string meshName)
        {
            cols = Mathf.Max(2, cols);
            rows = Mathf.Max(2, rows);

            int vertexCount = cols * rows;
            int quadCount = (cols - 1) * (rows - 1);
            int triangleIndexCount = quadCount * 6;

            var vertices = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
            var normals = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
            var uvs = new NativeArray<Vector2>(vertexCount, Allocator.TempJob);
            var triangles = new NativeArray<int>(triangleIndexCount, Allocator.TempJob);

            var vertexJob = new GridVertexJob
            {
                Cols = cols,
                Rows = rows,
                XMin = xMin,
                XMax = xMax,
                ZMin = zMin,
                ZMax = zMax,
                Vertices = vertices,
                Normals = normals,
                Uvs = uvs
            };

            var triangleJob = new GridTriangleJob
            {
                Cols = cols,
                Triangles = triangles
            };

            JobHandle vertexHandle = vertexJob.Schedule(vertexCount, 64);
            JobHandle triangleHandle = triangleJob.Schedule(quadCount, 64);
            JobHandle.CompleteAll(ref vertexHandle, ref triangleHandle);

            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = vertexCount > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            vertices.Dispose();
            normals.Dispose();
            uvs.Dispose();
            triangles.Dispose();

            return mesh;
        }

        [BurstCompile]
        private struct GridVertexJob : IJobParallelFor
        {
            public int Cols;
            public int Rows;
            public float XMin, XMax, ZMin, ZMax;

            [WriteOnly] public NativeArray<Vector3> Vertices;
            [WriteOnly] public NativeArray<Vector3> Normals;
            [WriteOnly] public NativeArray<Vector2> Uvs;

            public void Execute(int index)
            {
                int ix = index % Cols;
                int iz = index / Cols;

                float tx = Cols > 1 ? (float)ix / (Cols - 1) : 0f;
                float tz = Rows > 1 ? (float)iz / (Rows - 1) : 0f;

                float x = Mathf.Lerp(XMin, XMax, tx);
                float z = Mathf.Lerp(ZMin, ZMax, tz);

                Vertices[index] = new Vector3(x, 0f, z);
                Normals[index] = Vector3.up; // normal จริงคำนวณใหม่ใน shader จาก wave displacement
                Uvs[index] = new Vector2(tx, tz);
            }
        }

        [BurstCompile]
        private struct GridTriangleJob : IJobParallelFor
        {
            public int Cols;
            [WriteOnly] public NativeArray<int> Triangles;

            public void Execute(int quadIndex)
            {
                int quadsPerRow = Cols - 1;
                int qx = quadIndex % quadsPerRow;
                int qz = quadIndex / quadsPerRow;

                int i0 = qz * Cols + qx;
                int i1 = i0 + 1;
                int i2 = i0 + Cols;
                int i3 = i2 + 1;

                int t = quadIndex * 6;
                Triangles[t + 0] = i0;
                Triangles[t + 1] = i2;
                Triangles[t + 2] = i1;
                Triangles[t + 3] = i1;
                Triangles[t + 4] = i2;
                Triangles[t + 5] = i3;
            }
        }
    }
}
