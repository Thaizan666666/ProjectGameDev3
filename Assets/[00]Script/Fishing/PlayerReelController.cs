// ─────────────────────────────────────────────────────────────
// PlayerReelController.cs
// อ่านตำแหน่งเมาส์ (นิ่ง ๆ อยู่ฝั่งไหนของจอ) ต้านทิศปลา → ลด stamina ปลา
// (ระหว่างปลายังไม่เหนื่อย) จัดการปุ่ม "ดึง" (reel):
//   ปลาไม่เหนื่อย+ดึงค้างนานเกิน -> เบ็ดขาด
//   ปลาเหนื่อยแล้ว+ดึงค้าง -> สะสม ReelProgress
// Attach: Player GameObject (หรือ GameObject เดียวกับ FishingGameManager)
// ─────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerReelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishController fish;

    [Header("Counter-Direction Input")]
    [Tooltip("ระยะห่างจากกึ่งกลางจอ (พิกเซล) ที่ต้องวางเมาส์เกินไปถึงจะนับว่า 'สวนทางสำเร็จ'")]
    [SerializeField] private float counterThreshold = 50f;

    [Header("Reel Button")]
    [SerializeField] private Key reelKey = Key.Space;
    [Tooltip("ถ้าดึงค้างระหว่างปลายังไม่เหนื่อยเกินเวลานี้ (วินาที) -> เบ็ดขาดทันที")]
    [SerializeField] private float pullWhileActiveBreakTime = 1.5f;
    [Tooltip("ความเร็วสะสม ReelProgress ต่อวินาทีตอนปลาเหนื่อยแล้วและกำลังดึง")]
    [SerializeField] private float reelProgressPerSecond = 20f;
    [SerializeField] private float reelProgressMax = 100f;

    public event Action OnLineBroken;
    public event Action OnFishCaught;

    public float ReelProgress { get; private set; }
    public float ReelProgressMax => reelProgressMax;
    public float ReelProgressPercent => Mathf.Clamp01(ReelProgress / reelProgressMax);

    private float _pullBreakTimer;
    private bool _resolved;

    public void SetFish(FishController newFish)
    {
        fish = newFish;
        ResetState();
    }

    public void ResetState()
    {
        ReelProgress = 0f;
        _pullBreakTimer = 0f;
        _resolved = false;
    }

    private void Update()
    {
        if (fish == null || _resolved) return;

        HandleCounterDirection();
        HandleReelButton();
    }

    // ── สวนทางปลาด้วยตำแหน่งเมาส์ ─────────────────────────────
    private void HandleCounterDirection()
    {
        if (fish.State != FishState.Swimming) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float screenCenterX = Screen.width * 0.5f;
        float offsetFromCenter = mouse.position.ReadValue().x - screenCenterX;

        // ปลาอยู่ฝั่งไหน ผู้เล่นต้องวางเมาส์ไว้ฝั่งตรงข้ามถึงจะนับว่าสวนทางสำเร็จ
        bool counteringCorrectly = fish.RelativeDir switch
        {
            RelativeDirection.Left => offsetFromCenter > counterThreshold,
            RelativeDirection.Right => offsetFromCenter < -counterThreshold,
            _ => false
        };

        float tension = counteringCorrectly ? Mathf.Clamp01(Mathf.Abs(offsetFromCenter) / screenCenterX) : 0f;
        fish.ApplyTension(tension);
    }

    // ── ปุ่มดึง ────────────────────────────────────────────────
    private void HandleReelButton()
    {
        var keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard[reelKey].isPressed;

        if (fish.State == FishState.Tired)
        {
            _pullBreakTimer = 0f;

            if (held)
            {
                AddReelProgress(reelProgressPerSecond * Time.deltaTime);
            }
        }
        else
        {
            if (held)
            {
                _pullBreakTimer += Time.deltaTime;
                if (_pullBreakTimer >= pullWhileActiveBreakTime) TriggerLineBroken();
            }
            else
            {
                _pullBreakTimer = 0f;
            }
        }
    }

    /// <summary>เรียกจาก FishingGameManager เป็นโบนัส/บทลงโทษหลัง QTE ด้วย (delta ติดลบได้)</summary>
    public void AddReelProgress(float delta)
    {
        if (_resolved) return;

        ReelProgress = Mathf.Clamp(ReelProgress + delta, 0f, reelProgressMax);
        if (ReelProgress >= reelProgressMax) TriggerFishCaught();
    }

    private void TriggerLineBroken()
    {
        if (_resolved) return;
        _resolved = true;
        OnLineBroken?.Invoke();
    }

    private void TriggerFishCaught()
    {
        if (_resolved) return;
        _resolved = true;
        OnFishCaught?.Invoke();
    }
}
