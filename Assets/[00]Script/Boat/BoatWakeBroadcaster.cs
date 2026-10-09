using UnityEngine;

// ส่งตำแหน่งเรือเข้า global shader property ทุกเฟรม ให้ Water.shadergraph ดึงไปคำนวณ wake mask ได้
// (global property ไม่ต้อง expose ใน material, แค่ประกาศตัวแปรชื่อเดียวกันใน Custom Function Node ของ shader graph)
public class BoatWakeBroadcaster : MonoBehaviour
{
    private static readonly int BoatWakePosId = Shader.PropertyToID("_BoatWakePos");

    private void Update()
    {
        Shader.SetGlobalVector(BoatWakePosId, transform.position);
    }
}
