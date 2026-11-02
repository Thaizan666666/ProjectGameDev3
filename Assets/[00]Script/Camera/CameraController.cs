using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    [SerializeField] private CinemachineCamera Cincamera;

    void Awake()
    {
        Instance = this;
    }

    public void SetCameraInput(bool enabled)
    {
        var inputController = Cincamera.GetComponent<CinemachineInputAxisController>();

        if (inputController != null)
        {
            inputController.enabled = enabled;
        }
    }
}
