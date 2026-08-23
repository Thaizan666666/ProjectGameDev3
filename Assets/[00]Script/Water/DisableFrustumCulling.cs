using UnityEngine;

namespace WaterSystem
{
    /// <summary>
    /// ขยาย bounds ของ mesh ให้ใหญ่เกินซีนไปเลย เพื่อไม่ให้ Unity cull renderer นี้ทิ้ง
    /// เมื่อกล้องหันไปทางอื่น (ป้องกัน "น้ำหาย" ตอนเรือแล่นออกไปแล้วไม่ได้หันกล้องมาทาง water plane)
    /// ใช้ instance ของ mesh เฉพาะ object นี้ ไม่กระทบ mesh asset ต้นฉบับ
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class DisableFrustumCulling : MonoBehaviour
    {
        private void Awake()
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            mesh.bounds = new Bounds(mesh.bounds.center, Vector3.one * 100000f);
        }
    }
}
