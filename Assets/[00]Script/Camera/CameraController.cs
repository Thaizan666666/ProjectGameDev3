using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera Cincamera;

    public void SetCameraInput(bool enabled)
    {
        var inputController = Cincamera.GetComponent<CinemachineInputAxisController>();

        if (inputController != null)
        {
            inputController.enabled = enabled;
        }
    }
}
