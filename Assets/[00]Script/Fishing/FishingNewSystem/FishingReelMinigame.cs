// ─────────────────────────────────────────────────────────────
// FishingReelMinigame.cs
// Minigame ตอนปลากินเบ็ดแล้ว (เรียกจาก FishingHook ตอน bite — ยังเป็น timer จำลองไปก่อน):
//   ตอนเริ่ม ย้าย Hook ไปจุดตายตัวหน้า Player (frontDistance) ทันที — ล็อกความรู้สึกว่า "ปลาติดเบ็ดอยู่หน้าเรา"
//   กล้อง top-down "นิ่ง" จ้องจุดตายตัวนั้น (ไม่ไล่ตาม Hook ที่ขยับ) — ตัว Hook เองคือสิ่งที่ขยับไปมา
//   ผู้เล่นต้องลากเมาส์สวนทาง Hook เพื่อดึงกลับมา "ตรงกลาง" จอ (เป้าหมาย = Hook อยู่กลาง)
//   มีวงกลม 2 วง (ใหญ่/เล็ก=เขียวตรงกลาง วาดเป็น UI overlay คงที่กลางจอ) — Hook หลุดวงใหญ่ = ปลาหลุด จบ (กลับ Idle)
//   Hook อยู่กลาง (วงเขียว) ครบเวลาที่กำหนด -> เข้า QTE (วงเหลืองหดลง กดวรรคให้พอดีวงเล็ก)
//     - กดพอดี -> ดึงเข้ามา 1 รอบ (ครบ roundsRequired รอบ = จับได้)
//     - กดพลาด/ไม่กดจนวงหดเลยไป -> กลับไปไล่ดึง Hook เข้ากลางใหม่ (ไม่ลดจำนวนรอบที่ทำได้แล้ว)
// UI วาดด้วย OnGUI placeholder ไปก่อน (เส้น/จุดง่าย ๆ) จนกว่าจะมี UI จริง
// Attach: Player GameObject (เดียวกับ FishingCastController)
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace FishingNewSystem
{
    public class FishingReelMinigame : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("กล้อง Cinemachine มองจากด้านบนจุดตายตัวหน้า Player ตอนเล่น minigame นี้")]
        [SerializeField] private CinemachineCamera reelCamera;
        [Tooltip("Priority ตอน active — ต้องสูงกว่ากล้องหลักของเกมจริง ๆ (ปกติกล้องหลักตั้งไว้ 100)")]
        [SerializeField] private int reelCameraActivePriority = 150;
        [Tooltip("Priority ตอนไม่ได้ใช้ — ต้องต่ำกว่ากล้องหลักจริง ๆ ไม่ใช่แค่เท่ากัน")]
        [SerializeField] private int reelCameraIdlePriority = -100;

        [Header("Reel Camera Offset (คำนวณหมุนตาม Player เอง ไม่พึ่ง Cinemachine BindingMode)")]
        [Tooltip("ค่า offset ก่อนหมุน (local) — เอียงขึ้น-ถอยหลังจากทิศที่ Player หันหน้า")]
        [SerializeField] private Vector3 reelCameraLocalOffset = new Vector3(0f, 8f, -3f);

        [Header("จุดล็อก Hook หน้า Player")]
        [Tooltip("ระยะต่ำสุดหน้า Player (เมตร) ตอนล็อก Hook — ถ้าโยนได้ไกลกว่านี้ใช้ระยะจริงที่โยนไปแทน ถ้าใกล้กว่านี้ดึงมาไว้ที่ระยะนี้เป็นอย่างน้อย")]
        [SerializeField] private float minFrontDistance = 3f;
        [Tooltip("ปรับความสูงจุดล็อกเพิ่มจากความสูงที่ Hook ลอยอยู่เดิม (เมตร)")]
        [SerializeField] private float frontHeightOffset = 0.3f;
        [Tooltip("รัศมีจริงในโลก (เมตร) ที่ Hook ว่ายออกจากจุดล็อกได้สูงสุด (ตอน fishOffset magnitude = 1)")]
        [SerializeField] private float worldRadius = 1.5f;

        [Header("วงกลม UI (หน่วยพิกเซลบนจอ, คงที่กลางจอ)")]
        [SerializeField] private float bigCircleRadius = 220f;
        [SerializeField] private float smallCircleRadius = 70f;

        [Header("Hook วิ่งหนีออกจากกลาง")]
        [Tooltip("ความเร็วที่ Hook ไหลออกจากกลาง (สัดส่วนต่อวินาที — 1.0 คือถึงขอบวงใหญ่ใน 1 วิ)")]
        [SerializeField] private float driftSpeed = 0.25f;
        [SerializeField] private float driftDirectionChangeInterval = 1.2f;

        [Header("ผู้เล่นลากเมาส์สวนทาง Hook ดึงกลับกลาง")]
        [SerializeField] private float pullStrength = 0.6f;
        [Tooltip("ขนาดการขยับเมาส์ขั้นต่ำต่อเฟรม (px) ถึงจะนับว่ากำลังลากสวนทาง")]
        [SerializeField] private float counterMouseThreshold = 2f;

        [Header("ต้องอยู่ในวงเขียวนานแค่ไหนถึงเข้า QTE")]
        [SerializeField] private float holdTimeToTriggerQte = 1f;
        [Tooltip("รอเวลานี้ก่อน (วินาที) นับแต่เริ่มรอบ Centering ถึงจะเริ่มนับ hold ได้ — กัน QTE auto-trigger ทันทีตอนปลาเริ่มอยู่กลาง (fishOffset=0 ตอนเริ่ม) โดยไม่สนว่าปลาหลุดวงไปแล้วหรือยัง (กันเคสปลาสุ่ม drift ไม่หลุดวงเขียวเลยแล้วค้างไม่เข้า QTE ตลอดไป)")]
        [SerializeField] private float minCenteringGrace = 1.5f;

        [Header("QTE (วงเหลืองหดเข้ามา)")]
        [SerializeField] private float qteShrinkDuration = 1.5f;
        [SerializeField] private float qteStartRadius = 220f;
        [SerializeField] private float qteTolerance = 15f;

        [Header("จำนวนรอบที่ต้องดึงให้ครบ")]
        [SerializeField] private int roundsRequired = 3;

        [Header("ดึง Hook เข้ามาแบบ smooth ตอน QTE สำเร็จ (ไม่วาปไปตำแหน่งเลย)")]
        [SerializeField] private float pullDuration = 0.5f;

        private enum Phase { Idle, Centering, Qte, Pulling }
        private Phase phase = Phase.Idle;

        private Vector3 pullFromPos;
        private Vector3 pullToPos;
        private float pullTimer;
        private bool pullIsFinalCatch;

        private FishingHook hook;
        private Transform anchorTransform; // จุดตายตัวหน้า Player ที่กล้อง top-down จ้องนิ่ง ๆ ไม่ไล่ตาม Hook
        private Unity.Cinemachine.CinemachineFollow reelCameraFollow; // cache ไว้ตั้งค่า FollowOffset หมุนเองทุกเฟรม
        private Vector2 fishOffset; // -1..1 ต่อแกน, magnitude 1 = ขอบวงใหญ่ — ใช้คำนวณตำแหน่งจริงของ Hook ด้วย
        private Vector2 driftDir;
        private float driftTimer;
        private float holdTimer;
        private float qteTimer;
        private int roundsCompleted;
        private float quarterPullStep; // 1/4 ของระยะ anchor<->Player ตอนเริ่ม — ดึงเข้าทีละขั้นนี้ทุกครั้งที่ QTE สำเร็จ (3 ครั้ง + ครั้งสุดท้ายตอนจับได้ = ครบ 4/4)
        private float centeringElapsed; // เวลาที่ผ่านไปตั้งแต่เริ่มรอบ Centering นี้ — ใช้คู่กับ minCenteringGrace

        private float basePullStrength;
        private float baseHoldTimeToTriggerQte;

        private void Awake()
        {
            basePullStrength = pullStrength;
            baseHoldTimeToTriggerQte = holdTimeToTriggerQte;

            if (reelCamera != null)
            {
                reelCamera.Priority = reelCameraIdlePriority;
                reelCameraFollow = reelCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                FishingCameraUtil.ForceWorldSpaceBinding(reelCameraFollow);
            }
        }

        /// <summary>เซ็ตค่าจาก FishData (ปลาที่ติดเบ็ด) และ ItemSO ของเบ็ด/คันเบ็ดที่ใช้อยู่ — เรียกก่อน StartReeling
        /// เพื่อให้ความยาก/แรงดึงต่างกันไปตามชนิดปลาและอุปกรณ์จริง ไม่ต้องเรียกถ้าอยากใช้ค่า default ใน Inspector</summary>
        public void ApplyFishLoadout(FishData fishData, ItemSO hookItem)
        {
            if (fishData != null)
            {
                driftSpeed = Random.Range(fishData.driftSpeedMin, fishData.driftSpeedMax); // สุ่มใหม่ทุกครั้ง ไม่ให้เล่นซ้ำเหมือนเดิมตลอด
                if (fishData.roundsRequired > 0) roundsRequired = fishData.roundsRequired;
            }

            if (hookItem != null)
            {
                pullStrength = basePullStrength * Mathf.Max(0.01f, hookItem.hookPullStrengthMultiplier);
                holdTimeToTriggerQte = baseHoldTimeToTriggerQte * (1f - Mathf.Clamp(hookItem.hookHoldTimeReductionPercent, 0f, 0.9f));
            }
        }

        /// <summary>เรียกจาก FishingHook ตอนปลากินเบ็ด — เริ่ม minigame นี้ (ย้าย Hook ไปล็อกหน้า Player ทันที)</summary>
        public void StartReeling(FishingHook activeHook)
        {
            hook = activeHook;
            roundsCompleted = 0;
            fishOffset = Vector2.zero; centeringElapsed = 0f; // เริ่มจากจุดกลางก่อน แล้วค่อยเคลื่อนออกเองด้วย drift ให้ดูเป็นธรรมชาติ
            PickNewDrift();
            holdTimer = 0f;
            phase = Phase.Centering;

            // จุดล็อกหน้า Player — คำนวณครั้งเดียวตอนเริ่ม ไม่ขยับอีกจนกว่า minigame จบ
            // ใช้ระยะจริงที่โยนไป (ทิศจริงที่ Hook ลอยอยู่ด้วย) ถ้าไกลกว่าระยะต่ำสุด ไม่งั้นดึงมาไว้ที่ระยะต่ำสุดเป็นอย่างน้อย
            Vector3 toHook = hook.transform.position - transform.position;
            toHook.y = 0f;
            float actualDistance = toHook.magnitude;
            Vector3 direction = actualDistance > 0.01f ? toHook.normalized : transform.forward;
            float effectiveDistance = Mathf.Max(actualDistance, minFrontDistance);

            Vector3 anchorPoint = transform.position + direction * effectiveDistance;
            anchorPoint.y = hook.transform.position.y + frontHeightOffset;

            if (anchorTransform == null)
            {
                anchorTransform = new GameObject("FishingReelAnchor (runtime)").transform;
            }
            anchorTransform.position = anchorPoint;
            // yaw เดียวกับทิศที่โยน/ทิศผู้เล่น ให้ reelCamera (FollowOffset หมุนตาม BindingMode: LockToTargetWithWorldUp)
            // อยู่ฝั่ง/ทิศเดียวกับผู้เล่นตอนมองลงมา ไม่ใช่แกนโลกตายตัว
            anchorTransform.rotation = Quaternion.LookRotation(direction);

            // ระยะจาก anchor ถึง Player ตอนเริ่ม หาร 4 — ดึงเข้าทีละ 1/4 ทุกครั้งที่ QTE สำเร็จ
            // (3 ครั้งระหว่างเล่น + อีก 1 ครั้งตอนจับได้สำเร็จ = ครบ 4/4 ถึงตัว Player เป๊ะ)
            float totalPullDistance = Vector3.Distance(anchorPoint, transform.position);
            quarterPullStep = totalPullDistance * 0.25f;

            // ย้าย Hook ไปจุดล็อกทันที — ล็อกความรู้สึกว่าปลาติดเบ็ดอยู่หน้าเราแล้วจริง ๆ
            hook.transform.position = anchorPoint;

            if (reelCamera != null)
            {
                // กล้อง "นิ่ง" จ้องจุดล็อก ไม่ไล่ตาม Hook ที่กำลังจะขยับไปมา
                reelCamera.Follow = anchorTransform;
                reelCamera.LookAt = anchorTransform;
                reelCamera.Priority = reelCameraActivePriority;
            }

            Debug.Log("[FishingReelMinigame] ปลากินเบ็ด -> ย้าย Hook ไปหน้า Player แล้วเริ่ม reel minigame");
        }

        private bool IsActive => phase != Phase.Idle;

        private void Update()
        {
            if (!IsActive) return;

            switch (phase)
            {
                case Phase.Centering: UpdateCentering(); break;
                case Phase.Qte: UpdateQte(); break;
                case Phase.Pulling: UpdatePulling(); break;
            }

            UpdateHookWorldPosition();
            UpdateReelCameraOffset();
        }

        // ── หมุน reelCameraLocalOffset ด้วย yaw ของ Player เองตรงๆ แล้วยัดใส่ FollowOffset ทุกเฟรม (เหมือน hookCamera) ──
        private void UpdateReelCameraOffset()
        {
            Quaternion rot = FishingCameraUtil.GetFlatYaw(transform);
            FishingCameraUtil.ApplyOffset(reelCameraFollow, rot, reelCameraLocalOffset);
        }

        // ── เริ่มดึงจุดล็อก (anchor) เข้าหา Player แบบ smooth ทีละ distance เมตร ไม่ให้เลยตัว Player ไป ──
        private void StartPulling(float distance, bool isFinalCatch)
        {
            if (anchorTransform == null) return;

            pullFromPos = anchorTransform.position;

            Vector3 toPlayer = transform.position - anchorTransform.position;
            float currentDist = toPlayer.magnitude;
            float moveDist = Mathf.Min(distance, currentDist);
            pullToPos = currentDist > 0.0001f ? anchorTransform.position + (toPlayer / currentDist) * moveDist : anchorTransform.position;

            pullTimer = 0f;
            pullIsFinalCatch = isFinalCatch;
            phase = Phase.Pulling;
        }

        // ── ค่อย ๆ เลื่อน anchor จากจุดเดิมไปจุดใหม่ด้วย smoothstep ไม่วาปทันที ──
        private void UpdatePulling()
        {
            pullTimer += Time.deltaTime;
            float t = pullDuration > 0f ? Mathf.Clamp01(pullTimer / pullDuration) : 1f;
            float smoothT = t * t * (3f - 2f * t);
            anchorTransform.position = Vector3.Lerp(pullFromPos, pullToPos, smoothT);

            if (t < 1f) return;

            if (pullIsFinalCatch)
            {
                EndMinigame(true);
                return;
            }

            fishOffset = Vector2.zero; centeringElapsed = 0f; // เริ่มจากจุดกลางก่อน แล้วค่อยเคลื่อนออกเองด้วย drift ให้ดูเป็นธรรมชาติ
            PickNewDrift();
            holdTimer = 0f;
            phase = Phase.Centering;
        }

        // ── แปลง fishOffset (-1..1) เป็นตำแหน่งจริงของ Hook รอบจุดล็อกหน้า Player ──
        // clamp รัศมีแกว่งไม่ให้เกินระยะที่เหลือจาก anchor ถึง Player (* 0.9 เผื่อ margin) — ไม่งั้นพอ
        // ดึงเข้ามาหลายรอบจน anchor อยู่ใกล้ Player กว่า worldRadius แล้ว Hook จะแกว่งทะลุ/เลยตัว Player ไปได้
        private void UpdateHookWorldPosition()
        {
            if (hook == null || anchorTransform == null) return;

            float distAnchorToPlayer = Vector3.Distance(anchorTransform.position, transform.position);
            float safeRadius = Mathf.Min(worldRadius, distAnchorToPlayer * 0.9f);

            // ใช้แกน right/forward ของ anchor (yaw = ทิศออกจาก Player) ไม่ใช่แกนโลกตรง ๆ
            // fishOffset.y > 0 = ออกจาก Player ("ข้างหน้า"), fishOffset.x = ซ้าย/ขวา — clamp ไม่ให้ y ติดลบ ป้องกันวิ่งเข้าหา Player เด็ดขาด
            Vector3 pos = anchorTransform.position + (anchorTransform.right * fishOffset.x + anchorTransform.forward * fishOffset.y) * safeRadius;
            pos.y = hook.SampleWaterSurfaceY(pos); // ยังโยกตามคลื่นน้ำจริงต่อ ไม่ลอยนิ่งค้างความสูงเดิม
            hook.transform.position = pos;
        }

        private void UpdateCentering()
        {
            driftTimer -= Time.deltaTime;
            if (driftTimer <= 0f) PickNewDrift();

            fishOffset += driftDir * driftSpeed * Time.deltaTime;

            var mouse = Mouse.current;
            if (mouse != null && fishOffset.sqrMagnitude > 0.0001f)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                if (mouseDelta.magnitude >= counterMouseThreshold)
                {
                    Vector2 towardCenter = -fishOffset.normalized;
                    float alignment = Vector2.Dot(mouseDelta.normalized, towardCenter);
                    if (alignment > 0.3f)
                    {
                        fishOffset -= fishOffset.normalized * pullStrength * alignment * Time.deltaTime;
                    }
                }
            }

            if (fishOffset.y < 0f) fishOffset.y = 0f; // กันไปทาง Player เด็ดขาด เผื่อ mouse-correction ดึงทะลุ 0 ไป

            float magnitude = fishOffset.magnitude;

            if (magnitude >= 1f)
            {
                EndMinigame(false);
                return;
            }

            centeringElapsed += Time.deltaTime;

            float smallRatio = smallCircleRadius / bigCircleRadius;
            if (magnitude > smallRatio)
            {
                holdTimer = 0f;
            }
            else if (centeringElapsed >= minCenteringGrace)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdTimeToTriggerQte)
                {
                    StartQte();
                }
            }
        }

        private void PickNewDrift()
        {
            // สุ่มแค่ครึ่งวง 0-180 องศา -> driftDir.y (ไปข้างหน้า/ออกจาก Player) ไม่ติดลบเลย มีแค่หน้า/ซ้าย/ขวา ไม่มีถอยเข้าหา Player
            float angle = Random.Range(0f, Mathf.PI);
            driftDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            driftTimer = driftDirectionChangeInterval;
        }

        private void StartQte()
        {
            phase = Phase.Qte;
            qteTimer = 0f;
            Debug.Log("[FishingReelMinigame] Hook อยู่กลางครบเวลา -> QTE! กด Space ให้วงเหลืองพอดีวงเล็ก");
        }

        private void UpdateQte()
        {
            qteTimer += Time.deltaTime;
            float t = Mathf.Clamp01(qteTimer / qteShrinkDuration);
            float currentRingRadius = Mathf.Lerp(qteStartRadius, 0f, t);

            var kb = Keyboard.current;
            bool pressed = kb != null && kb.spaceKey.wasPressedThisFrame;

            if (pressed)
            {
                if (Mathf.Abs(currentRingRadius - smallCircleRadius) <= qteTolerance)
                {
                    OnQteSuccess();
                }
                else
                {
                    OnQteFail();
                }
                return;
            }

            if (currentRingRadius < smallCircleRadius - qteTolerance)
            {
                OnQteFail();
            }
        }

        private void OnQteSuccess()
        {
            roundsCompleted++;
            Debug.Log($"[FishingReelMinigame] QTE สำเร็จ! ดึงเข้ามา 1/4 ({roundsCompleted}/{roundsRequired})");

            bool isFinalCatch = roundsCompleted >= roundsRequired;
            // รอบสุดท้ายดึงรวดเดียว 2/4 ที่เหลือ (รอบจริงครั้งที่ 3 + รอบที่ 4 โดยนัย) ให้ Hook มาอยู่ติดตัว Player เป๊ะ
            float pullDistance = isFinalCatch ? quarterPullStep * 2f : quarterPullStep;

            StartPulling(pullDistance, isFinalCatch);
        }

        private void OnQteFail()
        {
            Debug.Log("[FishingReelMinigame] QTE พลาด -> ไล่ดึง Hook เข้ากลางใหม่ (ไม่ลดจำนวนรอบ)");
            fishOffset = Vector2.zero; centeringElapsed = 0f; // เริ่มจากจุดกลางก่อน แล้วค่อยเคลื่อนออกเองด้วย drift ให้ดูเป็นธรรมชาติ
            PickNewDrift();
            holdTimer = 0f;
            phase = Phase.Centering;
        }

        private void EndMinigame(bool success)
        {
            Debug.Log(success ? "[FishingReelMinigame] จับปลาได้!" : "[FishingReelMinigame] ปลาหลุด");

            phase = Phase.Idle;
            if (reelCamera != null) reelCamera.Priority = reelCameraIdlePriority;

            FishingHook endedHook = hook;
            hook = null;
            endedHook?.OnReelMinigameEnded(success);
        }

        private void OnGUI()
        {
            if (!IsActive) return;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            DrawCircleOutline(center, bigCircleRadius, Color.white);
            DrawCircleOutline(center, smallCircleRadius, Color.green);

            Vector2 fishScreenPos = center + new Vector2(fishOffset.x, -fishOffset.y) * bigCircleRadius;
            DrawDot(fishScreenPos, 10f, Color.red);

            if (phase == Phase.Qte)
            {
                float t = Mathf.Clamp01(qteTimer / qteShrinkDuration);
                float currentRingRadius = Mathf.Lerp(qteStartRadius, 0f, t);
                DrawCircleOutline(center, currentRingRadius, Color.yellow);
            }

            string msg;
            switch (phase)
            {
                case Phase.Centering:
                    msg = $"ลากเมาส์สวนทาง Hook เข้ากลาง ({roundsCompleted}/{roundsRequired})";
                    break;
                case Phase.Qte:
                    msg = $"กด Space ให้วงเหลืองพอดีวงเขียว! ({roundsCompleted}/{roundsRequired})";
                    break;
                default:
                    msg = $"กำลังดึงเข้ามา... ({roundsCompleted}/{roundsRequired})";
                    break;
            }
            GUI.Label(new Rect(center.x - 150, center.y + bigCircleRadius + 10, 300, 40), msg);
        }

        // ── วาดวงกลม/จุดด้วย OnGUI แบบง่าย ๆ (เส้นสั้นต่อกันเป็นวง) — placeholder ไปก่อนจนกว่าจะมี UI จริง ──
        private void DrawCircleOutline(Vector2 center, float radius, Color color)
        {
            const int segments = 48;
            Color old = GUI.color;
            GUI.color = color;

            Vector2 prev = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(prev, next, 2f);
                prev = next;
            }

            GUI.color = old;
        }

        private void DrawDot(Vector2 pos, float size, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void DrawLine(Vector2 a, Vector2 b, float thickness)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.001f) return;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
    }
}
