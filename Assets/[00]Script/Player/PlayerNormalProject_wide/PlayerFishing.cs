using System;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController.Examples;
using TableForge.Fish;

namespace PlayerNormal.Project_wide
{
    public class PlayerFishing : MonoBehaviour
    {
        public Animator rodAnim;
        InputAction swingRodAction;

        [Header("Rod Item")]
        [Tooltip("ItemSO ของคันเบ็ด (ลากจาก Project)")]
        [SerializeField] private ItemSO rodItem;

        [Header("Fishing Encounter")]
        [Tooltip("ตัวจัดการ encounter ตกปลา (GameObject FishingSystem ในซีน)")]
        [SerializeField] private FishingGameManager fishingGameManager;
        [Tooltip("ExamplePlayer บน GameObject เดียวกัน — ปิดตอนกด F ตกปลา (ตั้งแต่สะบัดคัน ไม่ใช่รอถึงปลากินเบ็ด) กันเดินระหว่างรอ/สู้ปลา")]
        [SerializeField] private ExamplePlayer examplePlayer;
        [Tooltip("ระยะห่างหน้าเบ็ดจากผู้เล่นที่จะ spawn ปลา")]
        [SerializeField] private float spawnDistance = 6f;

        [Header("Bite Timing")]
        [Tooltip("เวลารอ (วินาที) หลังโยนเบ็ด ก่อนปลาจะ 'กินเบ็ด' และโผล่มาให้เห็น — ต่ำสุด/สูงสุด สุ่มระหว่างนี้")]
        [SerializeField] private float biteDelayMin = 2f;
        [SerializeField] private float biteDelayMax = 6f;

        [Header("Hook Visual (optional)")]
        [Tooltip("ถ้าผูกไว้ จะรอ animation สะบัดคันจบแล้วโยน Hook ออกไปเป็นส่วนโค้งก่อนเริ่มนับเวลารอกินเบ็ด (ดู FishingHookThrow.cs) — ปล่อยว่างได้ถ้ายังไม่อยากใช้")]
        [SerializeField] private FishingHookThrow hookThrow;

        private FishZone currentZone;
        private GameObject spawnedFish;
        private FishController currentFishController;
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
                if (examplePlayer != null) examplePlayer.enabled = true; // กันเดินไม่ได้ค้างตลอดไปถ้า disable กลางคัน
            }

            hookThrow?.CleanupHook();
        }

        // ── ห้ามสะบัดเบ็ดซ้ำระหว่างที่ยังมี encounter ทำงานอยู่ (รอปลากินเบ็ด หรือกำลังสู้กับปลา) ──
        // ไม่มี cooldown เวลาแล้ว พอ encounter ก่อนหน้าจบ (จับได้/เบ็ดขาด) สะบัดเบ็ดใหม่ได้ทันที
        private bool IsHoldingRod =>
        Inventory.instance != null && Inventory.instance.EquippedItem == rodItem;

        private bool CanSwingRod =>
            waitForBiteRoutine == null &&
            (fishingGameManager == null || fishingGameManager.State != FishingEncounterState.Fighting) && IsHoldingRod;

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

        // ── Public read-only state for PlayerAnimatorUpdate ──
        /// <summary>กำลังรอปลากินเบ็ด (โยนเบ็ดแล้ว ยังไม่เริ่ม encounter)</summary>
        public bool IsWaitingForBite => waitForBiteRoutine != null;
        /// <summary>กำลังสู้กับปลา (encounter กำลังทำงาน)</summary>
        public bool IsEncounterActive =>
            fishingGameManager != null && fishingGameManager.State == FishingEncounterState.Fighting;

        /// <summary>Transform ของปลาที่กำลังสู้อยู่ (ใช้คำนวณ DirFishing ใน PlayerAnimatorUpdate) — null ถ้ายังไม่เริ่ม encounter</summary>
        public Transform CurrentFishTransform =>
            fishingGameManager != null && fishingGameManager.CurrentFish != null
                ? fishingGameManager.CurrentFish.transform
                : null;

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

            if (examplePlayer != null) examplePlayer.enabled = false; // กด F ตกปลาแล้ว ห้ามเดินตั้งแต่ตอนนี้เลย ไม่ใช่รอถึงปลากินเบ็ด

            if (waitForBiteRoutine != null) StopCoroutine(waitForBiteRoutine);
            waitForBiteRoutine = StartCoroutine(WaitForBiteThenSpawn());
        }

        private System.Collections.IEnumerator WaitForBiteThenSpawn()
        {
            // ถ้าผูก FishingHookThrow ไว้ ให้รอ animation จบ + โยน Hook + เปิดกล้องจ้อง Hook ก่อน
            // ค่อยเริ่มนับเวลารอกินเบ็ด (ไม่ผูกก็ข้ามไปนับเวลาทันทีเหมือนเดิม)
            if (hookThrow != null)
            {
                Vector3 targetPos = transform.position + transform.forward * spawnDistance;
                yield return StartCoroutine(hookThrow.PlayThrowSequence(targetPos));
            }

            float delay = UnityEngine.Random.Range(biteDelayMin, biteDelayMax);
            Debug.Log($"[PlayerFishing] เริ่มรอปลากินเบ็ด {delay:0.0} วินาที (timeScale ตอนนี้ = {Time.timeScale})");
            yield return new WaitForSeconds(delay);

            Debug.Log("[PlayerFishing] รอครบแล้ว กำลังเริ่ม encounter...");
            // ต้องอ่านตำแหน่ง Hook ไว้ก่อน CleanupHook (มันลบ Hook ทิ้งเลย) — ให้ปลา spawn ตรงจุดที่ Hook อยู่จริง
            Vector3? hookSpawnPos = hookThrow?.HookPosition;
            hookThrow?.CleanupHook();
            waitForBiteRoutine = null;
            TryStartFishingEncounter(hookSpawnPos);
        }

        // ── เริ่ม encounter ตกปลา: สุ่มปลาจากโซนที่ยืนอยู่แล้ว spawn ตรงตำแหน่ง Hook (เรียกตอนปลากินเบ็ดแล้วเท่านั้น) ──
        // overrideSpawnPos = null ถ้าไม่ได้ผูก FishingHookThrow ไว้ -> fallback ไปใช้ spawnDistance หน้าผู้เล่นเหมือนเดิม
        private void TryStartFishingEncounter(Vector3? overrideSpawnPos = null)
        {
            if (currentZone == null)
            {
                Debug.LogWarning("[PlayerFishing] ยกเลิกเริ่ม encounter: currentZone เป็น null (เดินออกจากโซนไปแล้วระหว่างรอปลากินเบ็ด?)");
                if (examplePlayer != null) examplePlayer.enabled = true; // ยกเลิก encounter แล้ว ไม่มี FishingGameManager มาปลดล็อกให้ ต้องปลดเอง
                return;
            }

            if (fishingGameManager == null)
            {
                Debug.LogWarning("[PlayerFishing] ยกเลิกเริ่ม encounter: fishingGameManager ไม่ได้ผูกไว้ใน Inspector");
                if (examplePlayer != null) examplePlayer.enabled = true;
                return;
            }

            // ถ้ามีปลาที่จองไว้จากตู้โชว์ (FishSlot) อยู่แล้ว ต้องเป็นตัวนั้นเป๊ะๆ ไม่สุ่มใหม่ — ไม่มีค่อย fallback ไปสุ่มสด
            FishData data = currentZone.ConsumePendingFish() ?? currentZone.GetRandomFish();
            if (data == null)
            {
                Debug.LogWarning($"[PlayerFishing] ยกเลิกเริ่ม encounter: {currentZone.ZoneName} สุ่มปลาไม่ได้ (ดู warning จาก FishZone ด้านบน — เช็ค Entries/FishDatabase)");
                if (examplePlayer != null) examplePlayer.enabled = true;
                return;
            }

            if (data.Prefab == null)
            {
                Debug.LogWarning($"[PlayerFishing] ยกเลิกเริ่ม encounter: {data.fishName} ไม่มี Prefab ผูกไว้ใน FishStats asset");
                if (examplePlayer != null) examplePlayer.enabled = true;
                return;
            }

            if (spawnedFish != null) Destroy(spawnedFish);

            Vector3 spawnPos = overrideSpawnPos ?? (transform.position + transform.forward * spawnDistance);
            spawnedFish = Instantiate(data.Prefab, spawnPos, Quaternion.identity);

            FishController controller = spawnedFish.GetComponent<FishController>();
            if (controller == null) controller = spawnedFish.AddComponent<FishController>();

            controller.SetFishData(data);
            fishingGameManager.StartEncounter(controller);
            hookThrow?.SetLineEndTarget(controller.transform); // Hook หายไปแล้ว แต่สายยังลากต่อไปหาปลาระหว่างสู้กัน

            currentFishController = controller;
            controller.OnStateChanged += HandleFishStateChanged;
            HandleFishStateChanged(controller.State); // ตั้งความตึงเริ่มต้นทันทีตาม state ปัจจุบัน ไม่ต้องรอ state เปลี่ยนก่อน

            Debug.Log($"[PlayerFishing] Encounter started -> {data.fishName} (Tier {data.fishTier}), State now: {fishingGameManager.State}");
        }

        // ── ปลาว่าย/พุ่ง -> สายตึง (สู้เต็มที่), ปลาเหนื่อย (Tired) -> สายหย่อน (ใกล้ดึงขึ้นฝั่งแล้ว) ──
        private void HandleFishStateChanged(FishState newState)
        {
            hookThrow?.SetLineTension(newState == FishState.Tired ? 0f : 1f);
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

                // ★ เพิ่มปลาลง Inventory ผ่าน ItemSO ที่ผูกไว้ใน FishStats
                if (caughtData.linkedItem != null)
                {
                    Inventory.instance.AddItem(caughtData.linkedItem, 1);
                    Debug.Log($"[PlayerFishing] เพิ่ม {caughtData.fishName} ลง Inventory แล้ว (ItemSO: {caughtData.linkedItem.itemName})");
                }
                else
                {
                    Debug.LogWarning($"[PlayerFishing] {caughtData.fishName} ไม่มี linkedItem — ยังไม่ได้ผูก ItemSO ใน FishStats asset");
                }

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
            hookThrow?.ClearLineEndTarget(); // encounter จบแล้ว (จับได้/เบ็ดขาด) ให้สายหายไปจริง ๆ
            if (examplePlayer != null) examplePlayer.enabled = true; // จบแล้ว (จับได้/เบ็ดขาด) ปลดล็อกให้เดินได้

            if (currentFishController != null)
            {
                currentFishController.OnStateChanged -= HandleFishStateChanged;
                currentFishController = null;
            }
        }
    }

}
