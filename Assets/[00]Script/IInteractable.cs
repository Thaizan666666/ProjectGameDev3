using UnityEngine;

namespace PlayerNormal.Project_wide
{
    public interface IInteractable
    {
        void OnActive(GameObject gameObject);
        void OnDisactive();
    }
    
}
