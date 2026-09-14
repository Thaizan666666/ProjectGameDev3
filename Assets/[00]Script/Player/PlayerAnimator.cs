using UnityEngine;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        public static PlayerAnimator Instance {get; private set;}
        private Animator _animator;
        private IPlayerAnimationLayer[] _layers;

        private void Awake()
        {
            Instance = this;
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