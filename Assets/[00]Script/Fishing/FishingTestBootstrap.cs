// ─────────────────────────────────────────────────────────────
// FishingTestBootstrap.cs
// ตัวช่วยทดสอบชั่วคราว: เริ่ม encounter กับปลาทดสอบทันทีตอน Play
// (เกมจริงจะเริ่ม StartEncounter จากระบบโยนเบ็ด/ปลากินเหยื่อแทน)
// Attach: GameObject เดียวกับ FishingGameManager (ลบทิ้งได้เมื่อต่อระบบโยนเบ็ดจริงแล้ว)
// ─────────────────────────────────────────────────────────────
using UnityEngine;

public class FishingTestBootstrap : MonoBehaviour
{
    [SerializeField] private FishingGameManager gameManager;
    [SerializeField] private FishController testFish;
    [Tooltip("กด R ระหว่าง Play เพื่อเริ่ม encounter ใหม่กับปลาตัวเดิม (ทดสอบ ResetEncounter)")]
    [SerializeField] private bool allowResetKey = true;

    private void Start()
    {
        if (gameManager != null && testFish != null)
            gameManager.StartEncounter(testFish);
    }

    private void Update()
    {
        if (!allowResetKey) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame && gameManager != null && testFish != null)
            gameManager.ResetEncounter(testFish);
    }
}
