// ─────────────────────────────────────────────────────────────
// FishingCastController.cs
// ระบบตกปลาใหม่ (เขียนใหม่ทั้งหมด แยกจาก FishingOld) — ตอนนี้ทำแค่:
//   กดปุ่มค้างไว้ตกปลา -> ล็อกเดินทันที -> ยิ่งกดค้างนานยิ่งชาร์จแรงโยน (0.5x-2.0x ปรับได้)
//   -> ปล่อยปุ่ม -> โยน Hook ออกไปจริงด้วย physics ตามแรงที่ชาร์จไว้ -> กล้องไล่ตาม Hook ต่อเนื่อง
//   -> Hook ชนอะไรเช็ค Layer "Water": โดน = state รอปลากิน กล้องยังตาม Hook ต่อไปจนกว่าจะมีปลามากิน
//                                        (state นั้นยังไม่ทำ) ไม่โดน = กลับท่าเก็บเบ็ด + ปลดล็อกเดิน + กล้องกลับ idle
//   กด R ได้ตลอดเวลา (ระหว่างชาร์จ/บิน/ลอยรอปลากิน) เพื่อยกเลิกแล้วกลับเป็นผู้เล่นปกติทันที
// Attach: Player GameObject (เดียวกับ ExamplePlayer)
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController.Examples;
using Unity.Cinemachine;

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

        [Header("Hook Camera")]
        [Tooltip("กล้อง Cinemachine ที่จะไล่ตามตำแหน่ง Hook (Follow+LookAt) ตั้งแต่โยนจนกว่าจะยกเลิก/ปลากินเบ็ด (state นั้นยังไม่ทำ) — ปล่อยว่างได้ถ้ายังไม่อยากสลับกล้อง")]
        [SerializeField] private CinemachineCamera hookCamera;
        [Tooltip("Priority กล้องตอนกำลังไล่ตาม Hook — ต้องสูงกว่า priority กล้องหลักของเกมจริง ๆ (เกมนี้กล้องหลักปกติตั้งไว้ 100)")]
        [SerializeField] private int hookCameraActivePriority = 150;
        [Tooltip("Priority กล้องตอนไม่ได้ใช้ — ต้องต่ำกว่ากล้องหลักจริง ๆ ไม่ใช่แค่เท่ากัน ไม่งั้น Cinemachine เจอ tie แล้วไม่สลับกลับกล้องหลัก")]
        [SerializeField] private int hookCameraIdlePriority = -100;

        [Header("Hook Camera Offset (คำนวณหมุนตาม Player เอง ไม่พึ่ง Cinemachine BindingMode)")]
        [Tooltip("ค่า offset ก่อนหมุน (local) — (0,3,-5) คือ ขึ้น 3, ถอยหลังจากทิศที่ Player หันหน้า 5 หน่วย")]
        [SerializeField] private Vector3 hookCameraLocalOffset = new Vector3(0f, 3f, -5f);

        [Header("สายเบ็ด (LineRenderer จาก rodTip ไป Hook)")]
        [Tooltip("LineRenderer วาดสายเบ็ด — ปล่อยว่างได้ถ้ายังไม่อยากมีสาย")]
        [SerializeField] private LineRenderer fishingLine;
        [Tooltip("จำนวนช่วงของเส้นตอนหย่อน (ยิ่งมากเส้นโค้งเนียนขึ้น)")]
        [SerializeField] private int lineSagSegments = 12;
        [Tooltip("ความหย่อนของสาย (เมตร) ตอนไม่ตึง — ยิ่งมากสายยิ่งห้อยลงมาก")]
        [SerializeField] private float lineSagAmount = 1.2f;

        private InputAction swingRodAction;
        private GameObject activeHook;
        private bool isCharging;
        private float chargeTimer;
        private Transform hookChaseAnchor; // จุดที่ hookCamera Follow จริง — ตำแหน่ง = Hook เสมอ (ทิศคำนวณแยกไปที่ FollowOffset ด้านล่าง)
        private Unity.Cinemachine.CinemachineFollow hookCameraFollow; // cache ไว้ตั้งค่า FollowOffset หมุนเองทุกเฟรม
        private bool isLineTaut; // false = หย่อน/โค้ง (บิน/ลอยรอปลากิน), true = ตึงเป็นเส้นตรง (ปลากินเบ็ดแล้ว)
        private ItemSO equippedHookItem; // เบ็ดที่ถืออยู่ตอนเริ่ม cast — ล็อกไว้ทั้ง cast แม้จะสลับ hotbar ทีหลัง

        private void Awake()
        {
            swingRodAction = InputSystem.actions.FindAction("Interacting/SwingRod");

            // กันปัญหา priority tie ตั้งแต่โหลดฉาก (เจอบั๊กนี้มาแล้วกับ FishingCameraRig ของระบบเก่า)
            if (hookCamera != null)
            {
                hookCamera.Priority = hookCameraIdlePriority;
                hookCameraFollow = hookCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                FishingCameraUtil.ForceWorldSpaceBinding(hookCameraFollow);
            }
        }

        private void OnEnable() => swingRodAction?.Enable();
        private void OnDisable() => swingRodAction?.Disable();

        private void Update()
        {
            // กด R ยกเลิกตกปลาได้ทุกเมื่อ ไม่ว่าจะกำลังชาร์จ/Hook บินอยู่/ลอยรอปลากิน
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                CancelFishing();
            }

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

            UpdateHookChaseAnchor();
            UpdateFishingLine();
        }

        // ── วาดสายเบ็ดจาก rodTip ไป Hook — หย่อน/โค้งตามปกติ ตึงเป็นเส้นตรงเฉพาะตอนปลากินเบ็ดแล้ว (isLineTaut) ──
        private void UpdateFishingLine()
        {
            if (fishingLine == null) return;

            if (!IsCasting)
            {
                fishingLine.positionCount = 0;
                return;
            }

            Vector3 start = rodTip.position;
            Vector3 end = activeHook.transform.position;

            if (isLineTaut)
            {
                fishingLine.positionCount = 2;
                fishingLine.SetPosition(0, start);
                fishingLine.SetPosition(1, end);
                return;
            }

            // หย่อน/โค้งลงกลางเส้นแบบ quadratic bezier ตอนยังไม่ตึง
            int segments = Mathf.Max(2, lineSagSegments);
            fishingLine.positionCount = segments + 1;
            Vector3 mid = (start + end) * 0.5f - Vector3.up * lineSagAmount;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(Vector3.Lerp(start, mid, t), Vector3.Lerp(mid, end, t), t);
                fishingLine.SetPosition(i, p);
            }
        }

        // ── อัปเดตตำแหน่ง/ทิศ chase anchor ทุกเฟรม — ตำแหน่ง = ตำแหน่ง Hook จริง (กล้องไล่ตามต่อเนื่อง),
        // ทิศ = Player หันไปทางไหน (ไม่ใช่คำนวณจาก Player->Hook) ให้ FollowOffset (0,3,-5) บวกเข้ากับตำแหน่ง Hook
        // ได้ผลลัพธ์ = Hook.position + Player.forward*(-5) + up*3 เสมอ ไม่ว่า Player หันแกนไหน ──
        private void UpdateHookChaseAnchor()
        {
            if (!IsCasting || hookChaseAnchor == null) return;

            Quaternion rot = FishingCameraUtil.GetFlatYaw(transform);

            hookChaseAnchor.position = activeHook.transform.position;
            hookChaseAnchor.rotation = rot;

            FishingCameraUtil.ApplyOffset(hookCameraFollow, rot, hookCameraLocalOffset);
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

            // ต้องถือเบ็ด (equip อยู่ใน hotbar) ก่อนถึงตกปลาได้
            ItemSO heldItem = Inventory.instance != null ? Inventory.instance.EquippedItem : null;
            if (heldItem == null || heldItem.itemType != ItemType.EquipmentItem)
            {
                Debug.LogWarning("[FishingCastController] ไม่ได้ถือเบ็ดอยู่ (ต้อง equip item ประเภท EquipmentItem) — ตกปลาไม่ได้");
                return;
            }
            equippedHookItem = heldItem;

            Debug.Log("[FishingCastController] กดปุ่มตกปลา -> ล็อกการเดินทันที, เริ่มชาร์จแรงโยน");
            if (examplePlayer != null)
            {
                // เคลียร์ input ค้างก่อนปิด Update() — ไม่งั้น ExampleCharacterController (Motor) ยังใช้ input
                // เฟรมล่าสุดต่อไปตลอด (เช่นกด F ตอนเดินอยู่ ตัวละครไถลไม่หยุด เพราะไม่มีใครสั่ง zero ให้)
                if (examplePlayer.Character != null)
                {
                    var zeroInputs = new PlayerCharacterInputs();
                    examplePlayer.Character.SetInputs(ref zeroInputs);
                }
                examplePlayer.enabled = false;
            }

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
            isLineTaut = false; // สายหย่อน/โค้งตั้งแต่โยน จนกว่าจะมีปลากินเบ็ด
            activeHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);

            FishingHook hook = activeHook.GetComponent<FishingHook>();
            if (hook == null)
            {
                Debug.LogWarning("[FishingCastController] hookPrefab ไม่มี component FishingHook — เช็ค Layer Water ไม่ได้");
            }
            else
            {
                hook.Init(this, equippedHookItem);
            }

            Vector3 throwHorizontalDir = transform.forward; // เก็บทิศแนวนอนไว้ตั้ง yaw ให้ Hook ด้วย (ใช้เป็นแกนอ้างอิงกล้อง)

            Rigidbody rb = activeHook.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("[FishingCastController] hookPrefab ไม่มี Rigidbody — Hook จะไม่ถูกโยนออกไป (ค้างอยู่กับที่)");
            }
            else
            {
                Vector3 throwDir = (throwHorizontalDir + Vector3.up * upwardRatio).normalized;
                rb.AddForce(throwDir * throwForce * powerMultiplier, ForceMode.VelocityChange);
                // freeze rotation ตั้งแต่โยนเลย กัน physics หมุน/ตีลังกา Hook ระหว่างบินกลางอากาศ
                // ไม่งั้นตอนตกน้ำ yaw ที่ตั้งไว้ด้านล่างจะเพี้ยนไปแล้ว กล้องเลยอ้างอิงทิศผิดจากที่โยนจริง
                rb.freezeRotation = true;
            }

            // chase anchor: ตำแหน่ง = Hook จริง (กล้องไล่ตามต่อเนื่อง), ทิศ = แนว Player->Hook สดทุกเฟรม (อัปเดตใน Update)
            // ผลคือกล้องอยู่ฝั่งเดียวกับ Player เทียบกับ Hook เสมอ ไม่ว่า Hook จะขยับไปทางไหนต่อ (WASD/คลื่นตอนลอย) ไม่ใช่แค่ตอนโยนครั้งเดียว
            if (hookChaseAnchor == null)
            {
                hookChaseAnchor = new GameObject("FishingHookChaseAnchor (runtime)").transform;
            }
            UpdateHookChaseAnchor();

            // กล้องไล่ตาม Hook ตั้งแต่โยนออกไปเลย (state โยนเบ็ด) — จะตามต่อเนื่องจนกว่าจะยกเลิก/ไม่โดนน้ำ/ปลากินเบ็ด
            if (hookCamera != null)
            {
                hookCamera.Follow = hookChaseAnchor;
                hookCamera.LookAt = activeHook.transform;
                hookCamera.Priority = hookCameraActivePriority;
            }
        }

        /// <summary>เรียกจาก FishingHook ตอนรู้ผลว่า Hook ตกโดน Water หรือไม่</summary>
        public void OnHookLanded(bool inWater)
        {
            if (inWater)
            {
                // โดนน้ำ -> เข้า state รอปลากิน กล้องยังตาม Hook ต่อไปเรื่อย ๆ ไม่ปิด
                // (จะปิดตอนมีปลากินเบ็ดจริง ซึ่ง state นั้นยังไม่ได้ทำ หรือกด R ยกเลิกเอง)
                Debug.Log("[FishingCastController] Hook โดน Layer Water -> เข้า State: รอปลากิน (ยังไม่มี state จริง แค่ log ไปก่อน) — กล้องยังตาม Hook ต่อ");
                // TODO: ต่อ state machine จริงตรงนี้
            }
            else
            {
                Debug.Log("[FishingCastController] Hook ไม่โดน Layer Water -> กลับท่าเก็บเบ็ด, ปลดล็อกการเดิน, กล้องกลับปกติ");
                // TODO: trigger animation ท่าเก็บเบ็ดจริงตรงนี้

                if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;
                if (examplePlayer != null) examplePlayer.enabled = true;
                if (activeHook != null) Destroy(activeHook);
                activeHook = null;
            }
        }

        /// <summary>เรียกจาก FishingHook ตอนปลากินเบ็ดแล้ว ก่อนเริ่ม reel minigame — ปิด hookCamera กลับ idle
        /// ไม่งั้นจะมีกล้อง 2 ตัว (hookCamera + reelCamera) ชิง Priority เท่ากันพร้อมกัน</summary>
        public void OnBiteStarted()
        {
            if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;
            isLineTaut = true; // ปลากินเบ็ดแล้ว -> สายตึงเป็นเส้นตรง
        }

        /// <summary>กด R เรียกตัวนี้ — ยกเลิกตกปลาทุกสถานะ (ชาร์จ/Hook บินอยู่/ลอยรอปลากิน) กลับเป็นผู้เล่นปกติทันที</summary>
        public void CancelFishing()
        {
            if (!IsCasting && !isCharging) return; // ไม่มีอะไรให้ยกเลิกอยู่แล้ว

            Debug.Log("[FishingCastController] กด R -> ยกเลิกตกปลา กลับเป็นผู้เล่นปกติ");

            isCharging = false;
            chargeTimer = 0f;

            if (hookCamera != null) hookCamera.Priority = hookCameraIdlePriority;
            if (examplePlayer != null) examplePlayer.enabled = true;
            if (activeHook != null) Destroy(activeHook);
            activeHook = null;
        }
    }
}
