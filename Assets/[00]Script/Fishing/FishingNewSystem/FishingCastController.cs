// ─────────────────────────────────────────────────────────────
// FishingCastController.cs
// ระบบตกปลาใหม่ (เขียนใหม่ทั้งหมด แยกจาก FishingOld) — ตอนนี้ทำแค่:
//   กดปุ่มค้างไว้ตกปลา -> ล็อกเดินทันที -> ยิ่งกดค้างนานยิ่งชาร์จแรงโยน (0.5x-2.0x ปรับได้)
//   -> ปล่อยปุ่ม -> โยน Hook ออกไปจริงด้วย physics ตามแรงที่ชาร์จไว้
//   -> Hook ชนอะไรเช็ค Layer "Water": โดน = state รอปลากิน (ยังไม่มี state จริง, log ไปก่อน)
//                                      ไม่โดน = กลับท่าเก็บเบ็ด + ปลดล็อกเดิน
// Attach: Player GameObject (เดียวกับ ExamplePlayer)
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController.Examples;

namespace FishingNewSystem
{
    public class FishingCastController : MonoBehaviour
    {
        [Header("Player Lock")]
        [Tooltip("ExamplePlayer บน GameObject เดียวกัน — ปิดตอนเริ่มตกปลา (กดปุ่มปุ๊บล็อกทันที)")]
        [SerializeField] private ExamplePlayer examplePlayer;

        [Header("Hook Throw")]
        [Tooltip("Prefab เบ็ด ต้องมี Rigidbody + Collider และติด component FishingHook")]
        [SerializeField] private GameObject hookPrefab;
        [Tooltip("จุดปลายคัน/ปลายเบ็ด ที่ Hook จะ spawn ออกมา")]
        [SerializeField] private Transform rodTip;
        [Tooltip("แรงโยนพื้นฐาน (ก่อนคูณด้วย power จากการกดค้าง)")]
        [SerializeField] private float throwForce = 8f;
        [Tooltip("สัดส่วนแรงยกขึ้น (0-1) ผสมกับแรงพุ่งไปข้างหน้า ให้ได้ส่วนโค้งธรรมชาติจาก physics")]
        [SerializeField, Range(0f, 1f)] private float upwardRatio = 0.4f;

        [Header("Charge Power")]
        [Tooltip("ตัวคูณแรงโยนต่ำสุด — ตอนแตะปุ่มแล้วปล่อยทันที (ไม่ชาร์จเลย)")]
        [SerializeField] private float minPowerMultiplier = 0.5f;
        [Tooltip("ตัวคูณแรงโยนสูงสุด — ตอนกดค้างจนชาร์จเต็ม")]
        [SerializeField] private float maxPowerMultiplier = 2.0f;
        [Tooltip("เวลากดค้าง (วินาที) ที่ใช้ชาร์จจนสุด (ถึง maxPowerMultiplier) — กดค้างเกินนี้ก็ยังคูณสูงสุดแค่เท่านี้")]
        [SerializeField] private float maxChargeDuration = 1.5f;

        private InputAction swingRodAction;
        private GameObject activeHook;
        private bool isCharging;
        private float chargeTimer;

        private void Awake()
        {
            swingRodAction = InputSystem.actions.FindAction("Interacting/SwingRod");
        }

        private void OnEnable() => swingRodAction?.Enable();
        private void OnDisable() => swingRodAction?.Disable();

        private void Update()
        {
            if (swingRodAction == null) return;

            if (swingRodAction.WasPressedThisFrame())
            {
                TryStartCharge();
            }

            if (isCharging)
            {
                chargeTimer += Time.deltaTime;

                if (swingRodAction.WasReleasedThisFrame())
                {
                    ReleaseCast();
                }
            }
        }

        // กันกดปุ่มซ้ำระหว่าง Hook ยังลอยอยู่ (ยังไม่ resolved) — ไม่งั้นได้ Hook ตัวที่ 2 ซ้อน
        // ตัวแรกหลุด reference เลย (ไม่ถูก Destroy) แล้วยังยิง OnHookLanded ย้อนมาสับสน state กันเอง
        private bool IsCasting => activeHook != null;

        private void TryStartCharge()
        {
            if (IsCasting || isCharging)
            {
                Debug.LogWarning("[FishingCastController] กดปุ่มตกปลาซ้ำระหว่างยังชาร์จ/Hook ยังลอยอยู่ — ไม่ทำอะไร");
                return;
            }

            // เช็คให้พร้อมโยนได้จริงก่อน ค่อยล็อกเดิน — ไม่งั้นถ้า hookPrefab/rodTip ไม่ได้ผูกไว้
            // จะล็อกเดินค้างตลอดไปโดยไม่มี Hook ให้ resolve กลับมาปลดล็อกเลย
            if (hookPrefab == null || rodTip == null)
            {
                Debug.LogWarning("[FishingCastController] ไม่ได้ผูก hookPrefab หรือ rodTip ใน Inspector — โยนเบ็ดไม่ได้ ไม่ล็อกเดิน");
                return;
            }

            Debug.Log("[FishingCastController] กดปุ่มตกปลา -> ล็อกการเดินทันที, เริ่มชาร์จแรงโยน");
            if (examplePlayer != null) examplePlayer.enabled = false;

            isCharging = true;
            chargeTimer = 0f;
        }

        private void ReleaseCast()
        {
            isCharging = false;

            float chargeRatio = maxChargeDuration > 0f ? Mathf.Clamp01(chargeTimer / maxChargeDuration) : 1f;
            float powerMultiplier = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, chargeRatio);

            Debug.Log($"[FishingCastController] ปล่อยปุ่ม -> ชาร์จ {chargeTimer:0.00}s ({chargeRatio:P0}) -> power x{powerMultiplier:0.00}");

            ThrowHook(powerMultiplier);
        }

        private void ThrowHook(float powerMultiplier)
        {
            activeHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);

            FishingHook hook = activeHook.GetComponent<FishingHook>();
            if (hook == null)
            {
                Debug.LogWarning("[FishingCastController] hookPrefab ไม่มี component FishingHook — เช็ค Layer Water ไม่ได้");
            }
            else
            {
                hook.Init(this);
            }

            Rigidbody rb = activeHook.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("[FishingCastController] hookPrefab ไม่มี Rigidbody — Hook จะไม่ถูกโยนออกไป (ค้างอยู่กับที่)");
            }
            else
            {
                Vector3 throwDir = (transform.forward + Vector3.up * upwardRatio).normalized;
                rb.AddForce(throwDir * throwForce * powerMultiplier, ForceMode.VelocityChange);
            }
        }

        /// <summary>เรียกจาก FishingHook ตอนรู้ผลว่า Hook ตกโดน Water หรือไม่</summary>
        public void OnHookLanded(bool inWater)
        {
            if (inWater)
            {
                Debug.Log("[FishingCastController] Hook โดน Layer Water -> เข้า State: รอปลากิน (ยังไม่มี state จริง แค่ log ไปก่อน)");
                // TODO: ต่อ state machine จริงตรงนี้
            }
            else
            {
                Debug.Log("[FishingCastController] Hook ไม่โดน Layer Water -> กลับท่าเก็บเบ็ด, ปลดล็อกการเดิน");
                // TODO: trigger animation ท่าเก็บเบ็ดจริงตรงนี้

                if (examplePlayer != null) examplePlayer.enabled = true;
                if (activeHook != null) Destroy(activeHook);
                activeHook = null;
            }
        }
    }
}
