// ─────────────────────────────────────────────────────────────
// FishController.cs
// Stamina + AI ว่ายน้ำ/พุ่ง (dash) ของปลาที่กำลังสู้กับผู้เล่น
// คำนวณ RelativeDirection (Left/Right/Forward) เทียบกับผู้เล่น
// Attach: Fish GameObject (ตัวที่กำลัง fighting อยู่)
// ─────────────────────────────────────────────────────────────
using System;
using UnityEngine;

public enum FishState
{
    Swimming,
    Dashing,
    Tired
}

public enum RelativeDirection
{
    Left,
    Right,
    Forward
}

public class FishController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform ผู้เล่นที่กำลังสู้กับปลาตัวนี้")]
    [SerializeField] private Transform player;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("Stamina <= ค่านี้ ถือว่าปลาเหนื่อย (Tired) ดึงเข้าฝั่งได้")]
    [SerializeField] private float tiredThreshold = 20f;
    [Tooltip("Stamina ที่หายไปต่อ 1 หน่วย tension ต่อวินาที (ผู้เล่นดึงสวนทางถูกต้อง)")]
    [SerializeField] private float staminaDrainPerTensionUnit = 15f;
    [Tooltip("Stamina ที่ฟื้นต่อวินาทีเมื่อไม่มีใครดึงสวนทางสำเร็จ")]
    [SerializeField] private float staminaRegenPerSecond = 4f;

    [Header("Swim AI")]
    [SerializeField] private float swimSpeed = 1.5f;
    [Tooltip("รัศมีที่ปลาว่ายวนรอบผู้เล่นตอนยังไม่ dash")]
    [SerializeField] private float swimRadius = 4f;
    [SerializeField] private float swimHeight = 0.5f;
    [SerializeField] private float directionChangeInterval = 1.5f;

    [Header("Dash")]
    [Tooltip("โอกาส dash ต่อวินาที (0-1) ตอนปลายังไม่เหนื่อยและพ้น cooldown แล้ว")]
    [SerializeField] private float dashChancePerSecond = 0.1f;
    [SerializeField] private float dashDuration = 1.2f;
    [SerializeField] private float dashSpeedMultiplier = 3f;
    [Tooltip("หลัง QTE จบ ต้องรอเท่านี้วินาทีก่อนปลาจะ dash ได้อีกครั้ง")]
    [SerializeField] private float cooldownAfterQte = 2f;
    [Tooltip("สัดส่วนแกน z (หน้า/หลัง) เทียบแกน x (ซ้าย/ขวา) ที่ต้องเด่นเกินนี้ (0-1) ระหว่าง dash ถึงจะตัดสินเป็น Forward")]
    [SerializeField, Range(0f, 1f)] private float forwardDominance = 0.6f;

    public event Action<FishState> OnStateChanged;

    public FishState State { get; private set; } = FishState.Swimming;
    public RelativeDirection RelativeDir { get; private set; } = RelativeDirection.Left;
    public float Stamina { get; private set; }
    public float MaxStamina => maxStamina;
    public float StaminaPercent => Mathf.Clamp01(Stamina / maxStamina);
    public bool IsTired => State == FishState.Tired;
    public bool IsDashing => State == FishState.Dashing;

    private int _lastTensionFrame = -1;
    private float _qteCooldownTimer;
    private float _dashTimer;
    private float _directionTimer;
    private Vector3 _swimTarget;

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
        Stamina = maxStamina;
        State = FishState.Swimming;
        _qteCooldownTimer = cooldownAfterQte;
        _dashTimer = 0f;
        _directionTimer = 0f;
        PickNewSwimTarget();
        UpdateRelativeDirection();
    }

    private void Awake()
    {
        if (Stamina <= 0f) Stamina = maxStamina;
    }

    private void Update()
    {
        if (player == null) return;

        if (_qteCooldownTimer > 0f) _qteCooldownTimer -= Time.deltaTime;

        switch (State)
        {
            case FishState.Swimming:
                SwimAround();
                RegenStaminaIfIdle();
                TryStartDash();
                break;

            case FishState.Dashing:
                DashTowardsAway();
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f) EndDash();
                break;

            case FishState.Tired:
                SwimAround();
                break;
        }

        UpdateRelativeDirection();
    }

    // ── Swim AI ───────────────────────────────────────────────
    private void SwimAround()
    {
        _directionTimer -= Time.deltaTime;
        if (_directionTimer <= 0f) PickNewSwimTarget();

        transform.position = Vector3.MoveTowards(transform.position, _swimTarget, swimSpeed * Time.deltaTime);
    }

    private void PickNewSwimTarget()
    {
        Vector2 circle = UnityEngine.Random.insideUnitCircle * swimRadius;
        Vector3 basePos = player != null ? player.position : transform.position;
        _swimTarget = basePos + new Vector3(circle.x, swimHeight, circle.y);
        _directionTimer = directionChangeInterval;
    }

    // ── Dash ──────────────────────────────────────────────────
    private void TryStartDash()
    {
        if (_qteCooldownTimer > 0f) return;
        if (UnityEngine.Random.value > dashChancePerSecond * Time.deltaTime) return;

        State = FishState.Dashing;
        _dashTimer = dashDuration;
        OnStateChanged?.Invoke(State);
    }

    private void DashTowardsAway()
    {
        if (player == null) return;
        Vector3 away = (transform.position - player.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = transform.forward;
        transform.position += away.normalized * swimSpeed * dashSpeedMultiplier * Time.deltaTime;
    }

    private void EndDash()
    {
        State = Stamina <= tiredThreshold ? FishState.Tired : FishState.Swimming;
        OnStateChanged?.Invoke(State);
    }

    // ── Direction relative to player ─────────────────────────
    private void UpdateRelativeDirection()
    {
        if (player == null) return;

        Vector3 toFish = transform.position - player.position;
        if (toFish.sqrMagnitude < 0.0001f) return;

        Vector3 local = player.InverseTransformDirection(toFish.normalized);

        if (State == FishState.Dashing)
        {
            float denom = Mathf.Abs(local.x) + Mathf.Abs(local.z) + 0.0001f;
            float forwardRatio = Mathf.Abs(local.z) / denom;
            if (forwardRatio > forwardDominance)
            {
                RelativeDir = RelativeDirection.Forward;
                return;
            }
        }

        RelativeDir = local.x < 0f ? RelativeDirection.Left : RelativeDirection.Right;
    }

    // ── Stamina ───────────────────────────────────────────────
    /// <summary>เรียกทุกเฟรมจาก PlayerReelController ตอนผู้เล่นดึงสวนทางถูกทิศ</summary>
    public void ApplyTension(float tension01)
    {
        tension01 = Mathf.Clamp01(tension01);
        _lastTensionFrame = Time.frameCount;
        ModifyStamina(-tension01 * staminaDrainPerTensionUnit * Time.deltaTime);
    }

    public void ModifyStamina(float delta)
    {
        Stamina = Mathf.Clamp(Stamina + delta, 0f, maxStamina);

        if (State != FishState.Dashing)
        {
            var newState = Stamina <= tiredThreshold ? FishState.Tired : FishState.Swimming;
            if (newState != State)
            {
                State = newState;
                OnStateChanged?.Invoke(State);
            }
        }
    }

    private void RegenStaminaIfIdle()
    {
        if (_lastTensionFrame == Time.frameCount) return;
        ModifyStamina(staminaRegenPerSecond * Time.deltaTime);
    }

    /// <summary>เรียกโดย FishingGameManager หลัง QTE จบ (สำเร็จหรือล้มเหลว) เพื่อกันปลา dash ซ้อน</summary>
    public void NotifyQteResolved()
    {
        _qteCooldownTimer = cooldownAfterQte;
    }
}
