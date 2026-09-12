using UnityEngine;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// Contract for any player animation layer.
    /// Core (PlayerAnimator) discovers all implementations via GetComponents<IPlayerAnimationLayer>()
    /// and calls UpdateAnimation every frame.
    /// </summary>
    public interface IPlayerAnimationLayer
    {
        void UpdateAnimation(Animator animator, ExampleCharacterController character);
    }
}