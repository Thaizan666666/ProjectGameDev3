using UnityEngine;
using KinematicCharacterController.Examples;
using PlayerNormal.Project_wide;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// Animation layer for player — implements IPlayerAnimationLayer
    /// ผูก Animator parameters เข้ากับ PlayerFishing state ตามที่ Player.controller มีอยู่แล้ว
    /// </summary>
    public class PlayerAnimatorUpdate : MonoBehaviour, IPlayerAnimationLayer
    {
        [SerializeField] private ExampleCharacterController _character;
        [SerializeField] private Animator _animator;

        [Header("Fishing")]
        [Tooltip("PlayerFishing component บน Player GameObject เดียวกัน (auto-find ถ้าว่าง) — ระบบเก่า")]
        [SerializeField] private PlayerFishing _fishing;
        [Tooltip("FishingCastController ของระบบใหม่ (FishingNewSystem) — ใช้แทน _fishing ถ้า Player ตัวนี้ไม่มี PlayerFishing (auto-find ถ้าว่าง)")]
        [SerializeField] private FishingNewSystem.FishingCastController _newFishing;
        [SerializeField] private RideArea _rideArea;

        #region Locomotion hashes
        // Locomotion hashes 
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        #endregion

        #region Fishing hashes
        // Fishing hashes 
        private static readonly int IsFishingHash = Animator.StringToHash("isFishing");
        private static readonly int IsFishBiteHash = Animator.StringToHash("isFishBite");
        private static readonly int IsFishingFinishHash = Animator.StringToHash("isFishingFinish");
        private static readonly int DirFishingHash = Animator.StringToHash("DirFishing");
        private static readonly int SmallFishHash = Animator.StringToHash("SmallFish");
        private static readonly int BigFishHash = Animator.StringToHash("BigFish");
        #endregion

        #region RidingCart hashes
        private static readonly int IsRidingHash = Animator.StringToHash("isRiding");
        #endregion

        #region CarryItem hashes
        private static readonly int IsCarryHash = Animator.StringToHash("isCarry");
        private static readonly int SmallItemHash = Animator.StringToHash("SmallItem?");
        private static readonly int BigItemHash = Animator.StringToHash("BigItem?");
        #endregion
        // track previous frame to detect state transitions (trigger on enter/exit)
        private bool _wasFishing;
        private bool _wasBite;
        private float _dirFishingVelocity; // for SmoothDamp

        private void Awake()
        {
            if (_character == null) _character = GetComponentInParent<ExampleCharacterController>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_fishing == null) _fishing = GetComponentInParent<PlayerFishing>();
            if (_fishing == null && _newFishing == null) _newFishing = GetComponentInParent<FishingNewSystem.FishingCastController>();
        }

        private void OnEnable()
        {
            if (_fishing != null) _fishing.OnFishObtained += HandleFishCaught;
            if (_newFishing != null) _newFishing.OnFishObtained += HandleFishCaught;
        }

        private void OnDisable()
        {
            if (_fishing != null) _fishing.OnFishObtained -= HandleFishCaught;
            if (_newFishing != null) _newFishing.OnFishObtained -= HandleFishCaught;
        }

        /// <summary>เมื่อตกปลาได้ ให้ trigger animation ตาม FishSize</summary>
        private void HandleFishCaught(FishData fish)
        {
            if (_animator == null || fish == null) return;
            
            ItemSize size = fish.linkedItem != null ? fish.linkedItem.itemSize : ItemSize.SmallItem;
            _animator.SetTrigger(fish.fishSize == FishSize.BigFish ? BigFishHash : SmallFishHash);

        }

        public void UpdateAnimation(Animator animator, ExampleCharacterController character)
        {
            UpdateLocomotion(animator, character);
            UpdateFishing(animator, character);
            UpdateRidingCart(animator);
        }

        private void UpdateLocomotion(Animator animator, ExampleCharacterController character)
        {
            bool isMoving = character.Motor.GroundingStatus.IsStableOnGround &&
                           character.Motor.Velocity.sqrMagnitude > 0.01f;
            animator.SetBool(IsMovingHash, isMoving);

            animator.SetBool(IsGroundedHash, character.Motor.GroundingStatus.IsStableOnGround);
        }

        private void UpdateFishing(Animator animator, ExampleCharacterController character)
        {
            bool isWaitingForBite;
            bool isActive;
            Transform fishTransformSource;

            if (_fishing != null)
            {
                isWaitingForBite = _fishing.IsWaitingForBite;
                isActive = _fishing.IsEncounterActive;
                fishTransformSource = _fishing.CurrentFishTransform;
            }
            else if (_newFishing != null)
            {
                isWaitingForBite = _newFishing.IsWaitingForBite;
                isActive = _newFishing.IsEncounterActive;
                fishTransformSource = _newFishing.CurrentFishTransform;
            }
            else
            {
                return;
            }

            // ── isFishing: true ตอนโยนเบ็ดแล้ว (รอปลากินเบ็ด หรือกำลังสู้) ──
            bool isFishing = isWaitingForBite || isActive;
            animator.SetBool(IsFishingHash, isFishing);

            // ── isFishBite: true ตอน encounter กำลังสู้กับปลา ──
            animator.SetBool(IsFishBiteHash, isActive);

            // ── isFishingFinish: trigger เมื่อออกจากโหมดตกปลา ──
            if (_wasFishing && !isFishing)
            {
                // animator.SetTrigger(IsFishingFinishHash);
                animator.SetBool(IsFishingFinishHash, true);
            }

            // ── DirFishing: มุมแนวนอนเทียบกับปลา → 0=fishing_left, 0.5=fishing_up, 1=fishing_right ──
            // คำนวณจาก signed angle ระหว่าง player.forward กับทิศไปหาปลา แล้ว map [-90°,+90°] → [0,1]
            // ปลาอยู่ซ้าย → หันขวาสู้ → angle > 0 → DirFishing > 0.5 ( fishing_right)
            // ปลาอยู่ขวา → หันซ้ายสู้ → angle < 0 → DirFishing < 0.5 ( fishing_left)
            Transform fishTransform = fishTransformSource;
            float targetDir = 0.5f;
            if (fishTransform != null && _character != null)
            {
                Vector3 toFish = fishTransform.position - _character.transform.position;
                toFish.y = 0f;
                if (toFish.sqrMagnitude > 0.001f)
                {
                    float angle = Vector3.SignedAngle(_character.transform.forward, toFish, Vector3.up);
                    targetDir = Mathf.Clamp01(0.5f + angle / 180f);
                }
            }
            float smoothed = Mathf.SmoothDamp(animator.GetFloat(DirFishingHash), targetDir, ref _dirFishingVelocity, 0.15f);
            animator.SetFloat(DirFishingHash, smoothed);

            _wasFishing = isFishing;
            _wasBite = isActive;
            animator.SetBool(IsFishingFinishHash, false);
        }

        private void UpdateRidingCart(Animator animator)
        {
            if(_rideArea == null) return;
            animator.SetBool(IsRidingHash, _rideArea.IsRiding);
        }
    }
}
