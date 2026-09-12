using UnityEngine;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private IPlayerAnimationLayer[] _layers;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _layers = GetComponents<IPlayerAnimationLayer>();
        }

        private void Update()
        {
            foreach (var layer in _layers)
                layer.UpdateAnimation(_animator, GetComponent<ExampleCharacterController>());
        }
    }
}