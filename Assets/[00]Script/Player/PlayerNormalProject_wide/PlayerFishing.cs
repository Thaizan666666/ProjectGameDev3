using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerNormal.Project_wide
{
    public class PlayerFishing : MonoBehaviour
    {
        public Animator rodAnim;
        [SerializeField] float maxCooldown = 300.0f;
        public float coolDown = 0;
        public bool isCooldown;
        InputAction swingRodAction;

        [Header("Fishing Encounter")]
        [Tooltip("ตัวจัดการ encounter ตกปลา (GameObject FishingSystem ในซีน)")]
        [SerializeField] private FishingGameManager fishingGameManager;
        [Tooltip("ระยะห่างหน้าเบ็ดจากผู้เล่นที่จะ spawn ปลา")]
        [SerializeField] private float spawnDistance = 3f;

        private FishZone currentZone;
        private GameObject spawnedFish;

        void Awake()
        {
            swingRodAction = InputSystem.actions.FindAction("Interacting/SwingRod");
        }

        void Start()
        {
            rodAnim = GetComponent<Animator>();

            isCooldown = true;
        }

        void OnEnable()
        {
            swingRodAction?.Enable();

            if (fishingGameManager != null)
            {
                fishingGameManager.OnFishCaught += HandleEncounterEnded;
                fishingGameManager.OnLineBroken += HandleEncounterEnded;
            }
        }

        void OnDisable()
        {
            swingRodAction?.Disable();

            if (fishingGameManager != null)
            {
                fishingGameManager.OnFishCaught -= HandleEncounterEnded;
                fishingGameManager.OnLineBroken -= HandleEncounterEnded;
            }
        }

        void Update()
        {
            if (!isCooldown)
            {
                if(coolDown <= 0)
                {
                    coolDown = 0.0f;
                    isCooldown = true;
                }
                else
                {
                    coolDown -= 1.0f;
                }
            }

            if (swingRodAction.WasPressedThisFrame() && coolDown == 0)
            {
                rodAnim.SetTrigger("Fishing");
                Debug.Log("Player is swinging");
                coolDown = maxCooldown;
                isCooldown = false;

                TryStartFishingEncounter();
            }
        }

        // ── Zone tracking (เรียกจาก FishZoneTrigger ตอนผู้เล่นเข้า/ออกโซน) ──
        public void SetCurrentZone(FishZone zone) => currentZone = zone;

        public void ClearCurrentZone(FishZone zone)
        {
            if (currentZone == zone) currentZone = null;
        }

        // ── เริ่ม encounter ตกปลา: สุ่มปลาจากโซนที่ยืนอยู่แล้ว spawn ──
        private void TryStartFishingEncounter()
        {
            if (currentZone == null || fishingGameManager == null) return;

            FishData data = currentZone.GetRandomFish();
            if (data == null || data.Prefab == null) return;

            if (spawnedFish != null) Destroy(spawnedFish);

            Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
            spawnedFish = Instantiate(data.Prefab, spawnPos, Quaternion.identity);

            FishController controller = spawnedFish.GetComponent<FishController>();
            if (controller == null) controller = spawnedFish.AddComponent<FishController>();

            fishingGameManager.StartEncounter(controller);
        }

        private void HandleEncounterEnded()
        {
            if (spawnedFish != null) Destroy(spawnedFish);
            spawnedFish = null;
        }
    }

}
