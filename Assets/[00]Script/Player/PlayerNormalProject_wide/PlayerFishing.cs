using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerNormal.Project_wide
{
    public class PlayerFishing : MonoBehaviour
    {
        public Animator rodAnim;
        InputAction swingRodAction;

        [Header("Fishing Encounter")]
        [Tooltip("ตัวจัดการ encounter ตกปลา (GameObject FishingSystem ในซีน)")]
        [SerializeField] private FishingGameManager fishingGameManager;
        [Tooltip("ระยะห่างหน้าเบ็ดจากผู้เล่นที่จะ spawn ปลา")]
        [SerializeField] private float spawnDistance = 6f;

        [Header("Bite Timing")]
        [Tooltip("เวลารอ (วินาที) หลังโยนเบ็ด ก่อนปลาจะ 'กินเบ็ด' และโผล่มาให้เห็น — ต่ำสุด/สูงสุด สุ่มระหว่างนี้")]
        [SerializeField] private float biteDelayMin = 2f;
        [SerializeField] private float biteDelayMax = 6f;

        private FishZone currentZone;
        private GameObject spawnedFish;
        private Coroutine waitForBiteRoutine;

        /// <summary>ส่งข้อมูลปลาที่จับได้ออกไปให้ระบบอื่น (เช่น Inventory) subscribe ไปเก็บเอง — ไม่ได้เก็บ/จัดการอะไรในนี้</summary>
        public event Action<FishData> OnFishObtained;

        void Awake()
        {
            swingRodAction = InputSystem.actions.FindAction("Interacting/SwingRod");
        }

        void Start()
        {
            rodAnim = GetComponent<Animator>();
        }

        void OnEnable()
        {
            swingRodAction?.Enable();

            if (fishingGameManager != null)
            {
                fishingGameManager.OnFishCaught += HandleFishCaught;
                fishingGameManager.OnLineBroken += HandleEncounterEnded;
            }
        }

        void OnDisable()
        {
            swingRodAction?.Disable();

            if (fishingGameManager != null)
            {
                fishingGameManager.OnFishCaught -= HandleFishCaught;
                fishingGameManager.OnLineBroken -= HandleEncounterEnded;
            }

            if (waitForBiteRoutine != null)
            {
                Debug.LogWarning("[PlayerFishing] OnDisable ถูกเรียกระหว่างที่กำลังรอกินเบ็ดอยู่ — coroutine ถูกตัดจบกลางทาง (component/GameObject นี้โดน disable)");
                StopCoroutine(waitForBiteRoutine);
                waitForBiteRoutine = null;
            }
        }

        // ── ห้ามสะบัดเบ็ดซ้ำระหว่างที่ยังมี encounter ทำงานอยู่ (รอปลากินเบ็ด หรือกำลังสู้กับปลา) ──
        // ไม่มี cooldown เวลาแล้ว พอ encounter ก่อนหน้าจบ (จับได้/เบ็ดขาด) สะบัดเบ็ดใหม่ได้ทันที
        private bool CanSwingRod =>
            waitForBiteRoutine == null &&
            (fishingGameManager == null || fishingGameManager.State != FishingEncounterState.Fighting);

        void Update()
        {
            if (!swingRodAction.WasPressedThisFrame()) return;

            if (!CanSwingRod)
            {
                Debug.LogWarning(
                    $"[PlayerFishing] กด F แต่ CanSwingRod = false — " +
                    $"waitForBiteRoutine {(waitForBiteRoutine == null ? "ว่าง" : "ยังค้างอยู่ (กำลังรอกินเบ็ด)")}, " +
                    $"FishingGameManager.State = {(fishingGameManager != null ? fishingGameManager.State.ToString() : "(ไม่ได้ผูก fishingGameManager)")}"
                );
                return;
            }

            if (rodAnim != null) rodAnim.SetTrigger("Fishing");   // ยังไม่มี Animator ก็ข้ามไปได้ ไม่ให้ระบบตกปลาค้าง
            Debug.Log("Player is swinging");

            StartWaitingForBite();
        }

        // ── Zone tracking (เรียกจาก FishZoneTrigger ตอนผู้เล่นเข้า/ออกโซน) ──
        public void SetCurrentZone(FishZone zone) => currentZone = zone;

        public void ClearCurrentZone(FishZone zone)
        {
            if (currentZone == zone) currentZone = null;
        }

        // ── รอ 'ปลากินเบ็ด' ก่อนค่อย spawn ปลาให้เห็น — ปลาจะไม่โผล่มาจนกว่าจะถึงตอนนี้
        // เพื่อให้ผู้เล่นรู้ตัวว่าต้องเริ่มดูตำแหน่งเมาส์ (ซ้าย/ขวา) ตอนไหน ไม่ใช่ตั้งแต่โยนเบ็ด
        private void StartWaitingForBite()
        {
            if (currentZone == null)
            {
                Debug.LogWarning("[PlayerFishing] สะบัดเบ็ดแล้วแต่ไม่เริ่มรอกินเบ็ด: currentZone เป็น null — ไม่ได้ยืนอยู่ใน FishZone จริง (เช็ค FishZoneTrigger: Is Trigger / Rigidbody / Tag \"Player\")");
                return;
            }

            if (fishingGameManager == null)
            {
                Debug.LogWarning("[PlayerFishing] สะบัดเบ็ดแล้วแต่ไม่เริ่มรอกินเบ็ด: fishingGameManager ไม่ได้ผูกไว้ใน Inspector");
                return;
            }

            if (waitForBiteRoutine != null) StopCoroutine(waitForBiteRoutine);
            waitForBiteRoutine = StartCoroutine(WaitForBiteThenSpawn());
        }

        private System.Collections.IEnumerator WaitForBiteThenSpawn()
        {
            float delay = UnityEngine.Random.Range(biteDelayMin, biteDelayMax);
            Debug.Log($"[PlayerFishing] เริ่มรอปลากินเบ็ด {delay:0.0} วินาที (timeScale ตอนนี้ = {Time.timeScale})");
            yield return new WaitForSeconds(delay);

            Debug.Log("[PlayerFishing] รอครบแล้ว กำลังเริ่ม encounter...");
            waitForBiteRoutine = null;
            TryStartFishingEncounter();
        }

        // ── เริ่ม encounter ตกปลา: สุ่มปลาจากโซนที่ยืนอยู่แล้ว spawn (เรียกตอนปลากินเบ็ดแล้วเท่านั้น) ──
        private void TryStartFishingEncounter()
        {
            if (currentZone == null)
            {
                Debug.LogWarning("[PlayerFishing] ยกเลิกเริ่ม encounter: currentZone เป็น null (เดินออกจากโซนไปแล้วระหว่างรอปลากินเบ็ด?)");
                return;
            }

            if (fishingGameManager == null)
            {
                Debug.LogWarning("[PlayerFishing] ยกเลิกเริ่ม encounter: fishingGameManager ไม่ได้ผูกไว้ใน Inspector");
                return;
            }

            // ถ้ามีปลาที่จองไว้จากตู้โชว์ (FishSlot) อยู่แล้ว ต้องเป็นตัวนั้นเป๊ะๆ ไม่สุ่มใหม่ — ไม่มีค่อย fallback ไปสุ่มสด
            FishData data = currentZone.ConsumePendingFish() ?? currentZone.GetRandomFish();
            if (data == null)
            {
                Debug.LogWarning($"[PlayerFishing] ยกเลิกเริ่ม encounter: {currentZone.ZoneName} สุ่มปลาไม่ได้ (ดู warning จาก FishZone ด้านบน — เช็ค Entries/FishDatabase)");
                return;
            }

            if (data.Prefab == null)
            {
                Debug.LogWarning($"[PlayerFishing] ยกเลิกเริ่ม encounter: {data.fishName} ไม่มี Prefab ผูกไว้ใน FishStats asset");
                return;
            }

            if (spawnedFish != null) Destroy(spawnedFish);

            Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
            spawnedFish = Instantiate(data.Prefab, spawnPos, Quaternion.identity);

            FishController controller = spawnedFish.GetComponent<FishController>();
            if (controller == null) controller = spawnedFish.AddComponent<FishController>();

            controller.SetFishData(data);
            fishingGameManager.StartEncounter(controller);
            Debug.Log($"[PlayerFishing] Encounter started -> {data.fishName} (Tier {data.fishTier}), State now: {fishingGameManager.State}");
        }

        // ── ตกได้แล้ว: caughtData คือปลาตัวที่จับได้จริง (ดึงจาก FishStats SO ผ่าน FishZone/FishDatabase) ──
        private void HandleFishCaught(FishData caughtData)
        {
            if (caughtData != null)
            {
                Debug.Log(
                    $"[PlayerFishing] Caught fish -> " +
                    $"ID: {caughtData.fishID}, " +
                    $"Name: {caughtData.fishName}, " +
                    $"Tier: {caughtData.fishTier}, " +
                    $"Weight: {caughtData.minWeight}-{caughtData.maxWeight}, " +
                    $"Rate: {caughtData.percentRate}%, " +
                    $"Price: {caughtData.Price}, " +
                    $"Icon: {(caughtData.Icon != null ? caughtData.Icon.name : "null")}, " +
                    $"Prefab: {(caughtData.Prefab != null ? caughtData.Prefab.name : "null")}"
                );
                OnFishObtained?.Invoke(caughtData);
            }
            else
            {
                Debug.LogWarning("[PlayerFishing] Fish caught but caughtData is null — CurrentFish.Data ไม่ได้ถูกตั้งค่าไว้ก่อน StartEncounter");
            }
            HandleEncounterEnded();
        }

        private void HandleEncounterEnded()
        {
            if (spawnedFish != null) Destroy(spawnedFish);
            spawnedFish = null;
        }
    }

}
