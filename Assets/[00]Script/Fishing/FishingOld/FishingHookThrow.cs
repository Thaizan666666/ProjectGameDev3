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
    [Tooltip("จุดปลายเบ็ด/ปลายคัน — Hook จะโผล่ออกมาจากตรงนี้ ปล่อยว่างไว้จะใช้ตำแหน่งผู้เล่นแทน (ประมาณเอา)")]
    [SerializeField] private Transform rodTip;
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

    [Header("Fishing Line")]
    [Tooltip("LineRenderer ที่ใช้วาดสายเบ็ด (ปลายเบ็ด -> Hook) ปล่อยว่างได้ถ้ายังไม่อยากวาดสาย")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("จำนวนช่วงย่อยของเส้นโค้ง ยิ่งเยอะยิ่งเนียน")]
    [SerializeField] private int lineSegments = 20;
    [Tooltip("ระยะหย่อนสูงสุดตรงกลางสาย (หน่วยเมตร) ตอน tension = 0 (สายหย่อนสุด)")]
    [SerializeField] private float maxSagAmount = 1.5f;
    [Range(0f, 1f)]
    [Tooltip("0 = สายหย่อนเต็มที่ (ท้องช้อนลงตามแรงโน้มถ่วง), 1 = สายตึงสุด (เส้นตรง) — เรียก SetLineTension() จากที่อื่นเพื่อเปลี่ยนสด ๆ ได้ (ค่อย ๆ ไล่ ไม่สแนปทันที)")]
    [SerializeField] private float lineTension = 0.3f;
    [Tooltip("ความเร็วไล่ตึง/หย่อน (หน่วยต่อวินาที, ค่าเต็ม 0->1 ใช้เวลา 1/ค่านี้ วินาที) ยิ่งมากยิ่งไล่เร็ว")]
    [SerializeField] private float tensionSmoothSpeed = 2f;

    private GameObject activeHook;
    private Transform lineEndOverride;
    private float displayedTension = 0.3f;

    /// <summary>ตำแหน่งปัจจุบันของ Hook — null ถ้ายังไม่มี Hook (ยังไม่โยน/โดน CleanupHook ไปแล้ว) ต้องอ่านค่านี้ก่อนเรียก CleanupHook เท่านั้น</summary>
    public Vector3? HookPosition => activeHook != null ? activeHook.transform.position : (Vector3?)null;

    /// <summary>ให้สายลากไปหา target นี้แทน Hook — ใช้ตอนปลากินเบ็ดแล้ว (Hook หายไป) แต่อยากให้สายยังลากต่อไปหาปลาระหว่างสู้กัน</summary>
    public void SetLineEndTarget(Transform target) => lineEndOverride = target;

    /// <summary>เลิกลากสายไปหา target — เรียกตอน encounter จบ (จับได้/เบ็ดขาด) ให้สายหายไปจริง ๆ</summary>
    public void ClearLineEndTarget() => lineEndOverride = null;

    private void Awake()
    {
        // กันปัญหา priority tie ตั้งแต่โหลดฉาก (เจอบั๊กนี้มาแล้วกับ FishingCameraRig)
        if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;
        if (lineRenderer != null) lineRenderer.enabled = false;
        displayedTension = lineTension; // เริ่มจากค่าที่ตั้งไว้ใน Inspector เป๊ะ ๆ ไม่ต้องไล่จาก 0 ตอนเปิดเกม
    }

    /// <summary>เรียกจากที่อื่นเพื่อเปลี่ยนความตึงของสายสด ๆ (เช่น ผูกกับค่า tension ตอนสวนทางปลาจริง) — 0 = หย่อนสุด, 1 = ตึงสุด</summary>
    public void SetLineTension(float tension01) => lineTension = Mathf.Clamp01(tension01);

    // อัปเดตทุกเฟรมหลัง Update() ทั้งหมด (รวม coroutine ที่ขยับ activeHook) รันจบแล้ว กัน line เด้งตามหลัง 1 เฟรม
    private void LateUpdate()
    {
        if (lineRenderer == null) return;

        // ไล่ displayedTension เข้าหาค่าเป้าหมาย (lineTension) ทีละนิดทุกเฟรม แทนการสแนปทันที ให้ตึง/หย่อนดูนุ่มนวลขึ้น
        displayedTension = Mathf.MoveTowards(displayedTension, lineTension, tensionSmoothSpeed * Time.deltaTime);

        // ปกติลากไปหา Hook ระหว่างรอกินเบ็ด — แต่ถ้ามี lineEndOverride (ตั้งไว้ตอนเริ่มสู้ปลาจริง) ให้ลากไปหาตัวนั้นแทน
        // เพราะ Hook ถูก CleanupHook ทำลายไปแล้วตอนเข้าสู่ fight phase
        Transform endTarget = lineEndOverride != null ? lineEndOverride : (activeHook != null ? activeHook.transform : null);

        if (rodTip == null || endTarget == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        DrawLine(rodTip.position, endTarget.position);
    }

    // ── วาดสายเบ็ดเป็นเส้นโค้ง quadratic Bezier จาก 3 จุด: ปลายเบ็ด(P0) - จุดกลางที่ห้อยลงตาม tension(P1) - Hook(P2) ──
    private void DrawLine(Vector3 rodTipPos, Vector3 hookPos)
    {
        Vector3 straightMid = Vector3.Lerp(rodTipPos, hookPos, 0.5f);
        float sag = Mathf.Lerp(maxSagAmount, 0f, displayedTension); // tension 0 = หย่อนเต็มที่, 1 = เส้นตรง
        Vector3 midPoint = straightMid + Vector3.down * sag;

        int pointCount = Mathf.Max(lineSegments, 1) + 1;
        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            float oneMinusT = 1f - t;
            // quadratic Bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
            Vector3 point = oneMinusT * oneMinusT * rodTipPos
                             + 2f * oneMinusT * t * midPoint
                             + t * t * hookPos;
            lineRenderer.SetPosition(i, point);
        }
    }

    /// <summary>รอ animation สะบัดคันเล่นจบ -> โยน Hook เป็นส่วนโค้งไป targetPos -> เปิดกล้องจ้อง Hook
    /// เรียกจาก PlayerFishing ด้วย yield return ก่อนเริ่มนับเวลารอกินเบ็ด</summary>
    public IEnumerator PlayThrowSequence(Vector3 targetPos)
    {
        yield return new WaitForSeconds(rodSwingDuration);

        Vector3 launchPos = rodTip != null
            ? rodTip.position
            : transform.position + transform.forward * 0.5f + Vector3.up * 1f;
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
