using UnityEngine;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// Animation layer for player locomotion — implements IPlayerAnimationLayer
    /// </summary>
    public class PlayerAnimatorUpdate : MonoBehaviour, IPlayerAnimationLayer
    {
        [SerializeField] private ExampleCharacterController _character;
        [SerializeField] private Animator _animator;

        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");

        private void Awake()
        {
            if (_character == null) _character = GetComponentInParent<ExampleCharacterController>();
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        public void UpdateAnimation(Animator animator, ExampleCharacterController character)
        {
            UpdateLocomotion(animator, character);

            // Debug.Log($"sqrMagnitude is {character.Motor.Velocity.sqrMagnitude}");
        }

        private void UpdateLocomotion(Animator animator, ExampleCharacterController character)
        {
            bool isMoving = character.Motor.GroundingStatus.IsStableOnGround &&
                           character.Motor.Velocity.sqrMagnitude > 0.01f;
            animator.SetBool(IsMovingHash, isMoving);

            animator.SetBool(IsGroundedHash, character.Motor.GroundingStatus.IsStableOnGround);
        }

        // ── Future extension methods (not implemented) ──

        // public void UpdateFishing(Animator animator, ExampleCharacterController character) { }
        // public void UpdateSwimming(Animator animator, ExampleCharacterController character) { }
    }
}