// ─────────────────────────────────────────────────────────────
// FishingHook.cs
// ติดที่ hookPrefab — เช็คว่า Hook ชนอะไรครั้งแรกเป็น Layer "Water" ไหม
// แล้ว report ผลกลับไปให้ FishingCastController ตัดสิน state ต่อ
// รองรับทั้งกรณี Water เป็น solid collider (OnCollisionEnter)
// และกรณีเป็น trigger volume (OnTriggerEnter) เผื่อ scene ตั้งไว้แบบไหนก็ได้
// ถ้าโดน Water จริง จะหยุดตก + ลอย+เคลื่อนที่ตามคลื่นจริงทั้งแนวตั้งและแนวนอน
// (ใช้สูตรเดียวกับ BoatBuoyancy/GerstnerWaves.hlsl ผ่าน WaterManager ให้ตรงกับที่เห็นบนจอ)
// ระหว่างลอยรอปลากิน:
//   - ผู้เล่นขยับตำแหน่ง Hook เองได้ด้วย WASD (คลื่นยังเหวี่ยงจากจุดที่ขยับไปเรื่อย ๆ)
//   - ขยับเข้าไปทับ Layer อื่นที่ไม่ใช่ Water ไม่ได้ (เช่น ชายฝั่ง/หิน) — บล็อกการขยับทิศนั้นไว้
//   - ถ้า Hook ลอยเข้าใกล้ผู้เล่นเกินไป ถือว่ายกเลิกการตกปลาอัตโนมัติ (เหมือนกด R)
//   - รอสุ่มเวลา (biteDelayMin-Max) จำลอง "ปลากินเบ็ด" (ยังไม่มี state ตรวจจับจริง) แล้วเริ่ม
//     FishingReelMinigame ต่อทันที (หยุด logic ลอย/WASD เดิมทั้งหมด)
// ─────────────────────────────────────────────────────────────
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WaterSystem;

namespace FishingNewSystem
{
    [RequireComponent(typeof(Collider))]
    public class FishingHook : MonoBehaviour
    {
        [Tooltip("ระยะสูง/ต่ำจากผิวน้ำ (บวก = ลอยเหนือผิว, ลบ = จมลงไปนิดหน่อย)")]
        [SerializeField] private float floatHeightOffset = -0.1f;
        [Tooltip("WaterManager ในฉาก — ดึงชุดคลื่นมาคำนวณให้ Hook ลอย+เคลื่อนที่ตามคลื่นจริง (สูตรเดียวกับ BoatBuoyancy) ปล่อยว่างได้ถ้าอยากให้ลอยนิ่งที่ผิวน้ำเฉย ๆ")]
        [SerializeField] private WaterManager waterManager;

        [Header("ควบคุมเองตอนลอยรอปลากิน (WASD)")]
        [Tooltip("ความเร็วขยับ Hook เอง (เมตร/วินาที) ตอนลอยอยู่ — คลื่นยังเหวี่ยงต่อจากจุดที่ขยับไปอยู่ดี")]
        [SerializeField] private float moveSpeed = 2f;
        [Tooltip("รัศมีเช็คกันชนตอนขยับ Hook เอง (เมตร) — ถ้าตำแหน่งใหม่ทับ collider ของ Layer อื่นที่ไม่ใช่ Water จะบล็อกไม่ให้ขยับเข้าไป")]
        [SerializeField] private float blockCheckRadius = 0.15f;

        [Header("ยกเลิกอัตโนมัติ")]
        [Tooltip("ถ้า Hook เข้าใกล้ผู้เล่นกว่าระยะนี้ (เมตร) จะยกเลิกการตกปลาทันที เหมือนกด R")]
        [SerializeField] private float minDistanceFromPlayer = 1.5f;

        [Header("Bite Timing (ชั่วคราว — รอ state ตรวจจับปลากินเบ็ดจริง)")]
        [SerializeField] private float biteDelayMin = 2f;
        [SerializeField] private float biteDelayMax = 5f;
        [Tooltip("FishingReelMinigame ในฉาก — ปล่อยว่างได้ auto-find ให้ (prefab asset ชี้ข้าม scene ไว้ล่วงหน้าไม่ได้)")]
        [SerializeField] private FishingReelMinigame reelMinigame;

        private FishingCastController owner;
        private ItemSO equippedHookItem; // เบ็ดที่ใช้โยนครั้งนี้ — ส่งต่อให้ FishingReelMinigame ตอนปลากินเบ็ด
        private bool resolved;
        private bool isFloating;
        private float baseWaterLevel;
        private Vector3 anchorPosition; // จุดที่คลื่นเหวี่ยง Hook ออกเป็นวงรี — ขยับได้เองด้วย WASD ตอนลอยอยู่
        private int nonWaterLayerMask; // bitmask ของ "ทุก Layer ยกเว้น Water และ Layer ของ Hook เอง"

        public void Init(FishingCastController castController, ItemSO hookItem)
        {
            owner = castController;
            equippedHookItem = hookItem;
            resolved = false;
            isFloating = false;

            // Hook ตอนนี้เป็น Project prefab asset แล้ว ผูก reference ข้าม scene ไว้ล่วงหน้าไม่ได้
            // (prefab asset ชี้ไปที่ object ใน scene ใดที่หนึ่งเจาะจงไม่ได้) เลย auto-find ตอน spawn จริงแทน
            // ยังปล่อยให้ override ผ่าน Inspector ได้เผื่ออยากชี้ WaterManager/FishingReelMinigame ตัวเฉพาะ
            if (waterManager == null) waterManager = FindFirstObjectByType<WaterManager>();
            if (reelMinigame == null) reelMinigame = FindFirstObjectByType<FishingReelMinigame>();

            // ยกเว้นทั้ง Water และ Layer ของ Hook เอง — ไม่งั้น collider ตัวเอง (ขนาด 1x1x1!)
            // จะโดนนับเป็น "สิ่งกีดขวาง" ทุกครั้งที่เช็คตำแหน่งใกล้ ๆ ตัวเอง บล็อกขยับไม่ได้เลยสักก้าว
            int waterLayer = LayerMask.NameToLayer("Water");
            int excludeMask = (waterLayer >= 0 ? (1 << waterLayer) : 0) | (1 << gameObject.layer);
            nonWaterLayerMask = ~excludeMask;
        }

        private void OnCollisionEnter(Collision collision) => HandleHit(collision.gameObject, collision.collider);

        private void OnTriggerEnter(Collider other) => HandleHit(other.gameObject, other);

        private void HandleHit(GameObject hitObject, Collider hitCollider)
        {
            if (resolved) return;
            resolved = true;

            bool isWater = hitObject.layer == LayerMask.NameToLayer("Water");
            Debug.Log($"[FishingHook] ชน \"{hitObject.name}\" (Layer: {LayerMask.LayerToName(hitObject.layer)}) — isWater = {isWater}");

            if (isWater) StartFloating(hitCollider);

            owner?.OnHookLanded(isWater);
        }

        // ── หยุดตก, สลับไปคุมตำแหน่งเองทุกเฟรมแทน physics (isKinematic) กันไม่ให้ physics แย่งคุมตำแหน่ง ──
        private void StartFloating(Collider waterCollider)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // ระดับน้ำนิ่งก่อนบวกคลื่น — ใช้ตำแหน่ง WaterManager ถ้ามี (ตรงกับที่ BoatBuoyancy ใช้)
            // ไม่มีก็ fallback ไปใช้ขอบบนของ collider น้ำที่ชนจริง
            baseWaterLevel = waterManager != null ? waterManager.transform.position.y : waterCollider.bounds.max.y;
            anchorPosition = transform.position;

            isFloating = true;
            StartCoroutine(WaitForBiteThenReel());
        }

        // ── รอสุ่มเวลาจำลองปลากินเบ็ด (ยังไม่มี state ตรวจจับจริง) แล้วเริ่ม reel minigame ──
        private IEnumerator WaitForBiteThenReel()
        {
            float delay = Random.Range(biteDelayMin, biteDelayMax);
            yield return new WaitForSeconds(delay);

            // ถูกยกเลิกไปแล้วก่อนจะกินเบ็ดจริง (กด R / เข้าใกล้ผู้เล่นเกินไป) — ไม่ต้องทำอะไรต่อ
            if (!isFloating) yield break;

            isFloating = false; // หยุด logic ลอยน้ำ/WASD/proximity-cancel เดิมทั้งหมด เข้าสู่ reel minigame แทน
            Debug.Log("[FishingHook] ปลากินเบ็ดแล้ว! -> เริ่ม reel minigame");

            owner?.OnBiteStarted(); // ปิด hookCamera ก่อน ไม่ให้ชิง Priority กับ reelCamera

            if (reelMinigame != null)
            {
                // fishData ยังไม่มี (ยังไม่มี logic เลือกชนิดปลาจริง) ส่งแค่เบ็ดที่ถือไปก่อน
                reelMinigame.ApplyFishLoadout(null, equippedHookItem);
                reelMinigame.StartReeling(this);
            }
            else
            {
                Debug.LogWarning("[FishingHook] ไม่มี FishingReelMinigame ในฉาก — ปลากินเบ็ดแล้วแต่ข้าม minigame ไปเลย");
                owner?.CancelFishing();
            }
        }

        /// <summary>เรียกจาก FishingReelMinigame ตอน minigame จบ (จับได้ หรือปลาหลุด)</summary>
        public void OnReelMinigameEnded(bool success)
        {
            Debug.Log(success ? "[FishingHook] จับปลาสำเร็จ!" : "[FishingHook] ปลาหลุดจาก reel minigame");
            owner?.CancelFishing();
        }

        /// <summary>เรียกจาก FishingReelMinigame — สูตรเดียวกับตอนลอยรอปลากิน (baseWaterLevel + คลื่นจริง + floatHeightOffset)
        /// ให้ Hook ยังโยกตามคลื่นน้ำต่อระหว่าง state ปลากินเบ็ด/reel minigame ไม่ใช่ลอยนิ่งค้างที่ความสูงเดิม</summary>
        public float SampleWaterSurfaceY(Vector3 worldPos)
        {
            float y = baseWaterLevel;
            if (waterManager != null && waterManager.Waves != null && waterManager.Waves.Length > 0)
            {
                GerstnerWaveMath.SampleWaves(worldPos, Time.time, waterManager.Waves, out Vector3 waveOffset, out _);
                y += waveOffset.y;
            }
            return y + floatHeightOffset;
        }

        // ใช้ LateUpdate ให้ทำงานหลัง Update อื่น ๆ ในเฟรมเดียวกัน กันตำแหน่งเด้งตามหลัง 1 เฟรม
        private void LateUpdate()
        {
            if (!isFloating) return;

            HandlePlayerMove();

            float waterHeight = baseWaterLevel;
            Vector3 horizontalDrift = Vector3.zero;

            if (waterManager != null && waterManager.Waves != null && waterManager.Waves.Length > 0)
            {
                // สุ่มตัวอย่างคลื่นที่ "จุดยึด" (anchorPosition — ขยับได้เองด้วย WASD) ไม่ใช่ตำแหน่งปัจจุบันที่ขยับอยู่ กัน feedback loop
                // ให้ได้ผลเป็นวงรีแกว่งไปมารอบจุดนั้น เหมือนเศษไม้ลอยน้ำ ไม่ใช่ลอยหนีไปเรื่อย ๆ เอง
                GerstnerWaveMath.SampleWaves(anchorPosition, Time.time, waterManager.Waves, out Vector3 waveOffset, out _);
                waterHeight += waveOffset.y;
                horizontalDrift = new Vector3(waveOffset.x, 0f, waveOffset.z);
            }

            Vector3 pos = anchorPosition + horizontalDrift;
            pos.y = waterHeight + floatHeightOffset;
            transform.position = pos;

            CheckTooCloseToPlayer(pos);
        }

        // ── อ่าน WASD ตรง ๆ จาก Keyboard (ไม่ผูก action asset เพราะเป็น minigame ชั่วคราว เหมือน FishingQTEManager ของระบบเก่า)
        // ขยับ anchorPosition เอง ไม่ใช่ transform.position ตรง ๆ เพราะ LateUpdate ด้านบนคำนวณคลื่นจาก anchor ทุกเฟรมอยู่แล้ว
        private void HandlePlayerMove()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // ไม่มี W ตั้งใจ — ห้ามเคลื่อน Hook ออกห่างจาก Player เพิ่มตอนลอยรอปลากิน (เหมือนยืดระยะคัดเรื่อย ๆ) มีแค่ถอยเข้า (S) / สไลด์ซ้ายขวา (A/D)
            Vector2 input = Vector2.zero;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude <= 0.0001f) return;

            input.Normalize();

            // เคลื่อนที่สัมพันธ์กับทิศที่ Player หันหน้า (เดียวกับทิศที่กล้องใช้คำนวณ) ไม่ใช่แกนโลกตรงๆ
            Vector3 moveForward = owner != null ? Vector3.ProjectOnPlane(owner.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 moveRight = owner != null ? Vector3.ProjectOnPlane(owner.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 delta = (moveRight * input.x + moveForward * input.y) * moveSpeed * Time.deltaTime;

            // เช็คที่ตำแหน่งจริงปัจจุบัน (ไม่ใช่ anchor) บวกด้วย delta ที่จะขยับ — ถ้าทับ collider ของ Layer
            // อื่นที่ไม่ใช่ Water (เช่น ชายฝั่ง/หิน) ห้ามขยับเข้าไปทิศนั้น กันเบ็ดลอยทะลุขึ้นฝั่งไปเลย
            if (Physics.CheckSphere(transform.position + delta, blockCheckRadius, nonWaterLayerMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            anchorPosition += delta;
        }

        // ── Hook ลอยเข้าใกล้ผู้เล่นเกินไป -> ยกเลิกการตกปลาทันที (เหมือนกด R) ──
        private void CheckTooCloseToPlayer(Vector3 currentPos)
        {
            if (owner == null) return;

            float dist = Vector3.Distance(currentPos, owner.transform.position);
            if (dist < minDistanceFromPlayer)
            {
                Debug.Log($"[FishingHook] Hook เข้าใกล้ผู้เล่นเกินไป ({dist:0.00}m < {minDistanceFromPlayer}m) -> ยกเลิกการตกปลา");
                isFloating = false;
                owner.CancelFishing();
            }
        }
    }
}
