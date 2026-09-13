// ─────────────────────────────────────────────────────────────
// FishingHookThrow.cs
// แยกออกมาจาก PlayerFishing: โยน Hook (placeholder ทรงกลม) ออกไปเป็นส่วนโค้ง
// หลัง animation สะบัดคันเล่นจบ แล้วสลับกล้องไปจ้อง Hook ระหว่างรอปลากินเบ็ด
// Attach: GameObject เดียวกับ PlayerFishing
// ─────────────────────────────────────────────────────────────
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class FishingHookThrow : MonoBehaviour
{
    [Header("Rod Swing Animation")]
    [Tooltip("ความยาว animation สะบัดคัน (วินาที) — ต้องรอเล่นจบก่อนค่อยโยน Hook ออกไปจริง (ดู Rod_Swing.anim)")]
    [SerializeField] private float rodSwingDuration = 1f;

    [Header("Hook Throw")]
    [Tooltip("กล้อง Cinemachine ที่จ้อง Hook ระหว่างรอปลากินเบ็ด — ปล่อยว่างได้ถ้ายังไม่อยากสลับกล้อง")]
    [SerializeField] private CinemachineCamera hookCamera;
    [Tooltip("เวลาที่ Hook ใช้ลอยจากปลายคันไปจุดเป้าหมาย (วินาที)")]
    [SerializeField] private float hookThrowDuration = 0.7f;
    [Tooltip("ความสูงสุดของส่วนโค้งตอน Hook ลอยออกไป")]
    [SerializeField] private float hookArcHeight = 1.5f;
    [SerializeField] private float hookPlaceholderScale = 0.15f;
    [Tooltip("Priority กล้อง Hook ตอน active — ต้องสูงกว่ากล้องหลักของ player")]
    [SerializeField] private int hookCameraActivePriority = 20;
    [Tooltip("Priority กล้อง Hook ตอนไม่ได้ใช้ — ต้องต่ำกว่ากล้องหลักจริง ๆ ไม่ใช่แค่เท่ากัน ไม่งั้น Cinemachine tie แล้วไม่สลับกลับ (ดูปัญหาเดียวกันใน FishingCameraRig)")]
    [SerializeField] private int hookCameraIdlePriority = -100;

    private GameObject activeHook;

    private void Awake()
    {
        // กันปัญหา priority tie ตั้งแต่โหลดฉาก (เจอบั๊กนี้มาแล้วกับ FishingCameraRig)
        if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;
    }

    /// <summary>รอ animation สะบัดคันเล่นจบ -> โยน Hook เป็นส่วนโค้งไป targetPos -> เปิดกล้องจ้อง Hook
    /// เรียกจาก PlayerFishing ด้วย yield return ก่อนเริ่มนับเวลารอกินเบ็ด</summary>
    public IEnumerator PlayThrowSequence(Vector3 targetPos)
    {
        yield return new WaitForSeconds(rodSwingDuration);

        Vector3 launchPos = transform.position + transform.forward * 0.5f + Vector3.up * 1f;
        activeHook = CreateHookPlaceholder(launchPos);

        yield return StartCoroutine(ThrowHookArc(activeHook.transform, targetPos));

        if (hookCamera != null)
        {
            hookCamera.LookAt = activeHook.transform;
            hookCamera.Priority = hookCameraActivePriority;
        }
    }

    /// <summary>เรียกตอนปลากินเบ็ดแล้ว (หรือยกเลิกกลางทาง) — ลบ Hook ทิ้งและปิดกล้องกลับเป็น idle</summary>
    public void CleanupHook()
    {
        if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;

        if (activeHook != null)
        {
            Destroy(activeHook);
            activeHook = null;
        }
    }

    // ── Hook ลอยจากปลายคันไป targetPos เป็นส่วนโค้ง (lerp ตำแหน่ง + sine โค้งขึ้นตรงกลางทาง) ──
    private IEnumerator ThrowHookArc(Transform hook, Vector3 targetPos)
    {
        Vector3 start = hook.position;
        float elapsed = 0f;

        while (elapsed < hookThrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hookThrowDuration);
            Vector3 pos = Vector3.Lerp(start, targetPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * hookArcHeight;
            hook.position = pos;
            yield return null;
        }

        hook.position = targetPos;
    }

    // ── Placeholder ทรงกลมแทน Hook จริง (ยังไม่มี model) ─────────────
    private GameObject CreateHookPlaceholder(Vector3 spawnPos)
    {
        GameObject hook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hook.name = "FishingHook (placeholder)";
        hook.transform.position = spawnPos;
        hook.transform.localScale = Vector3.one * hookPlaceholderScale;
        Destroy(hook.GetComponent<Collider>()); // กันชนกับ trigger/physics อื่นระหว่างลอย
        return hook;
    }
}
