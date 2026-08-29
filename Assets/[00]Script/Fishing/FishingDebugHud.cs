// ─────────────────────────────────────────────────────────────
// FishingDebugHud.cs
// HUD ชั่วคราวสำหรับทดสอบ (OnGUI) — โชว์ state/stamina/reel progress/QTE
// ก่อนที่จะมี UI จริงตาม spec ข้อ 2 (progress bar, QTE prompt, popup)
// Attach: GameObject เดียวกับ FishingGameManager (ลบทิ้งได้เมื่อทำ UI จริงแล้ว)
// ─────────────────────────────────────────────────────────────
using UnityEngine;

public class FishingDebugHud : MonoBehaviour
{
    [SerializeField] private FishingGameManager gameManager;
    [SerializeField] private PlayerReelController reelController;

    private QtePromptInfo _lastPrompt;
    private bool _qteActive;
    private string _lastResult = "-";

    private void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.OnPromptChanged += p => { _lastPrompt = p; _qteActive = true; };
        gameManager.OnQteStarted += _ => _qteActive = true;
        gameManager.OnQteResult += success => { _qteActive = false; _lastResult = success ? "QTE SUCCESS" : "QTE FAIL"; };
        gameManager.OnLineBroken += () => _lastResult = "LINE BROKEN";
        gameManager.OnFishCaught += () => _lastResult = "FISH CAUGHT";
    }

    private void OnGUI()
    {
        if (gameManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 340, 250), GUI.skin.box);
        GUILayout.Label($"Encounter State: {gameManager.State}");

        var fish = gameManager.CurrentFish;
        if (fish != null)
        {
            GUILayout.Label($"Fish State: {fish.State}   Dir: {fish.RelativeDir}");
            GUILayout.Label($"Stamina: {fish.Stamina:0.0} / {fish.MaxStamina} ({fish.StaminaPercent:P0})");
        }
        else
        {
            GUILayout.Label("Fish State: (none)");
        }

        if (reelController != null)
        {
            GUILayout.Label($"ReelProgress: {reelController.ReelProgress:0.0} / {reelController.ReelProgressMax} ({reelController.ReelProgressPercent:P0})");
        }

        GUILayout.Label(_qteActive
            ? $"QTE: press [{_lastPrompt.expectedKey}]  {_lastPrompt.index + 1}/{_lastPrompt.total}  t={_lastPrompt.timeRemaining:0.00}"
            : "QTE: -");

        GUILayout.Label($"Last Result: {_lastResult}");

        GUILayout.Space(8);
        GUILayout.Label("Space = ดึง (Reel) ค้าง | W/A/S/D = ตอบ QTE");
        GUILayout.Label("ขยับเมาส์ซ้าย/ขวาสวนทางปลา = ลด stamina");
        GUILayout.EndArea();
    }
}
