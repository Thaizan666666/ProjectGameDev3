// ─────────────────────────────────────────────────────────────
// FishingGameManager.cs
// Orchestrator เชื่อม FishController + PlayerReelController +
// FishingQTEManager + FishingCameraRig เข้าด้วยกัน
// จัดการ event จับได้/เบ็ดขาด และ relay event ให้ UI ผูกได้ตรง ๆ
// Attach: GameObject เดียว (เช่น "FishingGameManager") ในฉาก
// ─────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using KinematicCharacterController.Examples;

public enum FishingEncounterState
{
    Idle,
    Fighting,
    Resolved
}

public class FishingGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerReelController reelController;
    [SerializeField] private FishingQTEManager qteManager;
    [SerializeField] private FishingCameraRig cameraRig;

    [Header("QTE Reward / Penalty")]
    [Tooltip("Stamina ปลาที่หายเพิ่มเมื่อ QTE สำเร็จ")]
    [SerializeField] private float qteSuccessStaminaBonus = 15f;
    [Tooltip("ReelProgress ที่เพิ่มเมื่อ QTE สำเร็จ (มีผลจริงตอนปลาเหนื่อยแล้วเท่านั้น)")]
    [SerializeField] private float qteSuccessProgressBonus = 10f;
    [Tooltip("Stamina ปลาที่ฟื้นเมื่อ QTE ล้มเหลว")]
    [SerializeField] private float qteFailStaminaRecover = 20f;
    [Tooltip("ReelProgress ที่หายเมื่อ QTE ล้มเหลว")]
    [SerializeField] private float qteFailProgressPenalty = 15f;

    // ── Events สำหรับ UI ──────────────────────────────────────
    public event Action OnLineBroken;
    /// <summary>ยิงพร้อม FishData ของปลาที่จับได้ (จาก FishStats SO) — null ได้ถ้าเป็นปลาทดสอบที่ไม่ได้ผ่าน spawn flow จริง</summary>
    public event Action<FishData> OnFishCaught;
    public event Action<QtePromptInfo[]> OnQteStarted;
    public event Action<QtePromptInfo> OnPromptChanged;
    public event Action<bool> OnQteResult;

    public FishingEncounterState State { get; private set; } = FishingEncounterState.Idle;
    public FishController CurrentFish { get; private set; }

    private ExamplePlayer _playerControl;

    private void Awake()
    {
        if (player != null) _playerControl = player.GetComponent<ExamplePlayer>();
    }

    private void OnEnable()
    {
        if (reelController != null)
        {
            reelController.OnLineBroken += HandleLineBroken;
            reelController.OnFishCaught += HandleFishCaught;
        }

        if (qteManager != null)
        {
            qteManager.OnQteStarted += HandleQteStarted;
            qteManager.OnPromptChanged += HandlePromptChanged;
            qteManager.OnQteResult += HandleQteResult;
        }
    }

    private void OnDisable()
    {
        if (reelController != null)
        {
            reelController.OnLineBroken -= HandleLineBroken;
            reelController.OnFishCaught -= HandleFishCaught;
        }

        if (qteManager != null)
        {
            qteManager.OnQteStarted -= HandleQteStarted;
            qteManager.OnPromptChanged -= HandlePromptChanged;
            qteManager.OnQteResult -= HandleQteResult;
        }
    }

    private void Update()
    {
        if (State != FishingEncounterState.Fighting || CurrentFish == null) return;

        if (CurrentFish.IsDashing && CurrentFish.WantsQteThisDash && qteManager != null && !qteManager.IsActive)
        {
            qteManager.StartQte(CurrentFish.Tier);
            CurrentFish.ConsumeQteRequest();
        }
    }

    // ── Encounter lifecycle ───────────────────────────────────
    public void StartEncounter(FishController newFish)
    {
        CurrentFish = newFish;
        CurrentFish.Init(player);

        if (reelController != null) reelController.SetFish(CurrentFish);
        if (cameraRig != null)
        {
            cameraRig.SetFish(CurrentFish);
            cameraRig.SetActive(true);
        }

        if (_playerControl != null) _playerControl.SetControlEnabled(false);
        SetCursorForFishing(true);

        State = FishingEncounterState.Fighting;
    }

    public void ResetEncounter(FishController newFish)
    {
        EndEncounter();
        StartEncounter(newFish);
    }

    private void EndEncounter()
    {
        if (cameraRig != null) cameraRig.SetActive(false);
        if (_playerControl != null) _playerControl.SetControlEnabled(true);
        SetCursorForFishing(false);
        CurrentFish = null;
        State = FishingEncounterState.Idle;
    }

    /// <summary>
    /// ตอนตกปลาต้องปลดล็อกเคอร์เซอร์ให้เห็น/ขยับได้จริง เพราะ PlayerReelController
    /// อ่านตำแหน่งเมาส์ตรง ๆ (Mouse.current.position) — ถ้า Cursor ยังล็อกกลางจอ
    /// (ExamplePlayer.Start() ล็อกไว้ตอนเริ่มเกม) ค่าตำแหน่งจะค้างที่กลางจอตลอด ไม่ขยับตาม
    /// </summary>
    private void SetCursorForFishing(bool isFishing)
    {
        Cursor.lockState = isFishing ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isFishing;
    }

    // ── Handlers ──────────────────────────────────────────────
    private void HandleQteStarted(QtePromptInfo[] infos) => OnQteStarted?.Invoke(infos);

    private void HandlePromptChanged(QtePromptInfo info) => OnPromptChanged?.Invoke(info);

    private void HandleLineBroken()
    {
        State = FishingEncounterState.Resolved;
        StopEncounterSystems();
        CurrentFish = null;
        OnLineBroken?.Invoke();
    }

    private void HandleFishCaught()
    {
        FishData caughtData = CurrentFish != null ? CurrentFish.Data : null;

        State = FishingEncounterState.Resolved;
        StopEncounterSystems();
        CurrentFish = null;
        OnFishCaught?.Invoke(caughtData);
    }

    /// <summary>เรียกทุกครั้งที่ encounter จบ (จับได้/เบ็ดขาด) — ปิดทุกระบบย่อยที่ยังทำงานค้างอยู่ กันไม่ให้ QTE/กล้อง/คอนโทรลทำงานต่อหลังตกปลาเสร็จแล้ว</summary>
    private void StopEncounterSystems()
    {
        if (qteManager != null && qteManager.IsActive) qteManager.CancelQte();
        if (cameraRig != null) cameraRig.SetActive(false);
        if (_playerControl != null) _playerControl.SetControlEnabled(true);
        SetCursorForFishing(false);
    }

    private void HandleQteResult(bool success)
    {
        if (CurrentFish != null)
        {
            CurrentFish.NotifyQteResolved();
            CurrentFish.ModifyStamina(success ? -qteSuccessStaminaBonus : qteFailStaminaRecover);
        }

        if (reelController != null)
        {
            reelController.AddReelProgress(success ? qteSuccessProgressBonus : -qteFailProgressPenalty);
        }

        OnQteResult?.Invoke(success);
    }
}
