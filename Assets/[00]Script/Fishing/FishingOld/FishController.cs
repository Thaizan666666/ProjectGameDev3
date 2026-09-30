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
    [Header("Identity")]
    [Tooltip("ระดับปลา (มีผลกับว่า QTE จะเกิดไหม/ยากแค่ไหน — ดู FishingQTEManager)")]
    [SerializeField] private FishTier tier = FishTier.Common;

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
    [Tooltip("รัศมีที่ปลาว่ายวนรอบผู้เล่นตอนเริ่ม encounter (ยังไม่ได้ดึงเลย) — หดเข้าหาผู้เล่นตาม ReelProgress ตอนกดดึงค้างเท่านั้น ถ้าปลายังดูใกล้เกินไปตอนเริ่ม ให้เพิ่มค่านี้")]
    [SerializeField] private float swimRadius = 8f;
    [SerializeField] private float swimHeight = 0.5f;
    [SerializeField] private float directionChangeInterval = 1.5f;
    [Tooltip("ตอนถูกดึง (ReelProgress > 0) ความเร็วว่ายจะเพิ่มขึ้นสูงสุดกี่เท่า ให้รู้สึกเหมือนโดนลากเข้ามาจริง ๆ")]
    [SerializeField] private float maxReelSpeedMultiplier = 1.5f;
    [Tooltip("ระยะห่างจากผู้เล่นต่ำสุดที่ปลาเข้ามาได้ (กันปลาชนตัวผู้เล่นตอนถูกดึงเข้ามาสุด)")]
    [SerializeField] private float minDistanceFromPlayer = 2f;

    [Header("Dash")]
    [Tooltip("โอกาส dash ต่อวินาที (0-1) ตอนปลายังไม่เหนื่อยและพ้น cooldown แล้ว")]
    [SerializeField] private float dashChancePerSecond = 0.1f;
    [SerializeField] private float dashDuration = 1.2f;
    [SerializeField] private float dashSpeedMultiplier = 3f;
    [Tooltip("หลัง QTE จบ ต้องรอเท่านี้วินาทีก่อนปลาจะ dash ได้อีกครั้ง")]
    [SerializeField] private float cooldownAfterQte = 2f;
    [Tooltip("สัดส่วนแกน z (หน้า/หลัง) เทียบแกน x (ซ้าย/ขวา) ที่ต้องเด่นเกินนี้ (0-1) ระหว่าง dash ถึงจะตัดสินเป็น Forward")]
    [SerializeField, Range(0f, 1f)] private float forwardDominance = 0.6f;

    [Header("QTE")]
    [Tooltip("เปิด/ปิดการเกิด QTE สำหรับปลาตัวนี้ — ใช้เป็น override ปิด QTE เฉพาะตัวได้ (เช่น debug/ทดสอบ) แต่ทุกกรณี QTE จะเกิดได้เฉพาะปลา Tier Boss เท่านั้น (เช็คคู่กับ tier ด้านล่าง)")]
    [SerializeField] private bool qteEnabled = true;
    [Tooltip("ความถี่ที่ dash แต่ละครั้งจะกลายเป็น QTE จริง (0 = ไม่เกิดเลย, 1 = ทุก dash ที่ผ่านเงื่อนไข cooldown เป็น QTE เสมอ)")]
    [SerializeField, Range(0f, 1f)] private float qteChancePerDash = 1f;

    public event Action<FishState> OnStateChanged;

    public FishTier Tier => tier;
    public FishState State { get; private set; } = FishState.Swimming;
    public RelativeDirection RelativeDir { get; private set; } = RelativeDirection.Left;
    public float Stamina { get; private set; }
    public float MaxStamina => maxStamina;
    public float StaminaPercent => Mathf.Clamp01(Stamina / maxStamina);
    public bool IsTired => State == FishState.Tired;
    public bool IsDashing => State == FishState.Dashing;
    /// <summary>สุ่มตัดสินไว้ตอน dash เริ่ม — dash รอบนี้จะกลายเป็น QTE จริงไหม (ดู FishingGameManager)</summary>
    public bool WantsQteThisDash { get; private set; }
    /// <summary>ข้อมูลปลาตัวนี้ (ชื่อ/ราคา/ไอคอน ฯลฯ) ที่ดึงมาจาก FishStats SO ผ่าน FishZone/FishDatabase ตอน spawn จริง — null ถ้าเป็นปลาทดสอบที่ไม่ได้ผ่าน spawn flow</summary>
    public FishData Data { get; private set; }

    private int _lastActiveFrame = -1;
    private float _qteCooldownTimer;
    private float _dashTimer;
    private float _directionTimer;
    private Vector2 _swimTargetUnit;
    private Vector3 _swimTargetPos;
    private float _pullProgress01;
    /// <summary>ค่า _pullProgress01 สูงสุดที่เคยทำได้ (ไม่มีวันลดลงเอง) — ใช้จำกัดว่าปลาว่ายออกห่างผู้เล่นได้ไกลสุดแค่ไหนตอนไม่ได้ถูกดึงอยู่ กันไม่ให้ว่ายกลับไปไกลเท่าตอนเริ่ม encounter ทั้งที่เคยถูกดึงเข้ามาแล้ว</summary>
    private float _maxPullProgressReached;

    /// <summary>เรียกจาก spawn flow (เช่น PlayerFishing) ก่อน StartEncounter เพื่อบอกระดับปลาที่ spawn จริง</summary>
    public void SetTier(FishTier newTier) => tier = newTier;

    /// <summary>เรียกจาก spawn flow ก่อน StartEncounter เพื่อผูกข้อมูลปลาจริง (จาก FishStats SO ผ่าน FishZone) เข้ากับตัวนี้ — เซ็ต Tier ให้ตรงกับข้อมูลนี้ไปด้วยในตัว</summary>
    public void SetFishData(FishData data)
    {
        Data = data;
        if (data != null) tier = data.fishTier;
    }

    /// <summary>เรียกจาก PlayerReelController ทุกครั้งที่ ReelProgress เปลี่ยน — ยิ่งดึงเข้ามากรัศมีว่ายของปลาจะยิ่งหดเข้าหาผู้เล่น จนถึงตัวผู้เล่นพอดีตอนดึงสำเร็จ (progress = 1)
    /// มีผลจริงเฉพาะตอนปลา Tired เท่านั้น — ตอน Swimming ปลายัง dash ได้ ซึ่งอาจเกิด QTE แล้วมี success/fail bonus ยิง ReelProgress มาด้วย
    /// (ดู FishingGameManager.HandleQteResult) ถ้าปล่อยให้มีผลตอน Swimming ด้วย ปลาจะหด/ขยับกระทันหันทั้งที่ผู้เล่นยังไม่ได้กดดึงเลย
    /// แล้วพอเข้าสู่ Tired จริงๆ ค่อยกระโดดกลับไปอีกค่า (ดูเหมือน "ถอย" หลอกๆ) — เลยกันไว้ ไม่ให้ตำแหน่งปลาขยับตาม QTE ตอนยังไม่เหนื่อย</summary>
    public void SetPullProgress(float progress01)
    {
        if (State != FishState.Tired) return;

        _pullProgress01 = Mathf.Clamp01(progress01);
        if (_pullProgress01 > _maxPullProgressReached) _maxPullProgressReached = _pullProgress01;
    }

    /// <summary>เรียกจาก FishingGameManager หลังใช้ WantsQteThisDash ไปเริ่ม QTE แล้ว กันไม่ให้เริ่มซ้ำใน dash รอบเดียวกัน</summary>
    public void ConsumeQteRequest() => WantsQteThisDash = false;

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
        Stamina = maxStamina;
        State = FishState.Swimming;
        _qteCooldownTimer = cooldownAfterQte;
        _dashTimer = 0f;
        _directionTimer = 0f;
        _pullProgress01 = 0f;
        _maxPullProgressReached = 0f;
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
                TryStartDash();
                break;

            case FishState.Dashing:
                DashMove();
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f) EndDash();
                break;

            case FishState.Tired:
                // ปลาเหนื่อยแล้ว หยุด "ว่ายเล่น" แบบสุ่มทันที เคลื่อนที่ได้แค่ทางเดียวคือถูกดึงเข้าหาผู้เล่นตาม ReelProgress เท่านั้น
                PullTowardsPlayer();
                break;
        }

        UpdateRelativeDirection();
    }

    // เช็ค idle/regen ใน LateUpdate เสมอ — รับประกันว่า PlayerReelController.Update() (ที่ mark ว่า "กำลัง active" ผ่าน ApplyTension)
    // รันไปแล้วก่อนหน้าในเฟรมเดียวกันแน่ ๆ ไม่ว่า Script Execution Order จะเป็นยังไง กัน false-idle ที่ทำให้ stamina ฟื้นทั้งที่กำลังสวนทางถูกอยู่จริง
    private void LateUpdate()
    {
        if (player == null) return;

        switch (State)
        {
            case FishState.Swimming:
                RegenStaminaIfIdle(); // ฟื้นเฉพาะตอนไม่ได้สวนทางถูกอยู่
                break;
            case FishState.Tired:
                // ฟื้นตลอดเวลาไม่ว่าจะกด Spacebar ดึงค้างอยู่หรือไม่ก็ตาม — ปลามีโอกาสหายเหนื่อยและกลับไปสู้ใหม่ได้เสมอ ไม่ใช่แค่ตอนผู้เล่นปล่อยมือ
                ModifyStamina(staminaRegenPerSecond * Time.deltaTime);
                break;
        }
    }

    // ── Swim AI ───────────────────────────────────────────────
    private void SwimAround()
    {
        _directionTimer -= Time.deltaTime;
        if (_directionTimer <= 0f) PickNewSwimTarget();

        // _swimTargetPos คือจุดนิ่งที่คำนวณไว้ครั้งเดียวตอน PickNewSwimTarget (ไม่คำนวณสดทุกเฟรมแล้ว) —
        // พอปลาไปถึงจุดนั้นจะหยุดนิ่งจริงๆ จนกว่า directionTimer จะครบแล้วเลือกจุดใหม่ ไม่ใช่ขยับกระตุกตามผู้เล่นหมุนตัวทุกเฟรมแบบ noise
        float speed = swimSpeed * Mathf.Lerp(1f, maxReelSpeedMultiplier, _pullProgress01);
        transform.position = Vector3.MoveTowards(transform.position, _swimTargetPos, speed * Time.deltaTime);
    }

    /// <summary>ตอนเหนื่อย (Tired) ปลาไม่ว่ายเล่นเองแล้ว เคลื่อนที่ได้ทางเดียวคือถูกดึงเข้าหาผู้เล่นตาม ReelProgress สดๆ ตรงนี้ — คงมุมเดิมที่อยู่ไว้ (ไม่สุ่มมุมใหม่) แค่ปรับระยะห่างเข้า-ออกตาม CurrentSwimRadius</summary>
    private void PullTowardsPlayer()
    {
        Vector3 basePos = player.position;
        Vector3 dirFromPlayer = transform.position - basePos;
        dirFromPlayer.y = 0f;
        dirFromPlayer = dirFromPlayer.sqrMagnitude > 0.0001f ? dirFromPlayer.normalized : HorizontalForward();

        Vector3 pullTarget = basePos + dirFromPlayer * CurrentSwimRadius() + Vector3.up * swimHeight;
        transform.position = Vector3.MoveTowards(transform.position, pullTarget, swimSpeed * Time.deltaTime);
    }

    private Vector3 HorizontalForward()
    {
        Vector3 fwd = player != null ? player.forward : transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
    }

    /// <summary>รัศมีที่ปลาถูกดึงเข้ามาตอนนี้จริงๆ (ใช้ตอน Tired เท่านั้น) — หดลงตาม _pullProgress01 สดๆ (0 = รัศมีเต็ม, 1 = อยู่ที่ตัวผู้เล่นพอดี) ขึ้น-ลงได้ตาม ReelProgress จริง (เช่น QTE fail ทำให้ถอยออกชั่วคราว) แต่ไม่มีวันต่ำกว่า minDistanceFromPlayer กันปลาชนผู้เล่น</summary>
    private float CurrentSwimRadius() => Mathf.Lerp(swimRadius, minDistanceFromPlayer, _pullProgress01);

    /// <summary>ระยะไกลสุดที่ปลาว่ายเล่นได้ตอน Swimming/Dashing — หดถาวรตาม _maxPullProgressReached (high-water mark) ไม่ใช่ _pullProgress01 สดๆ ปลาจะไม่มีวันว่ายกลับออกไปไกลกว่าระยะที่เคยถูกดึงเข้ามาได้แล้ว</summary>
    private float MaxWanderRadius() => Mathf.Lerp(swimRadius, minDistanceFromPlayer, _maxPullProgressReached);

    /// <summary>มุมคงที่แค่ 3 ค่า เทียบกับ player.forward — แบ่งกรวย 150° หน้าผู้เล่นออกเป็น 3 โซนเท่าๆกัน (โซนละ 50°) แล้วใช้จุดกึ่งกลางแต่ละโซนเป็นทิศซ้าย/หน้า/ขวา ไม่มีเฉียง ให้ผู้เล่นอ่านทิศทางปลาได้ง่ายเหมือน Sea of Thieves</summary>
    private static readonly float[] SwimAngles = { -50f, 0f, 50f };

    private void PickNewSwimTarget()
    {
        float angle = SwimAngles[UnityEngine.Random.Range(0, SwimAngles.Length)] * Mathf.Deg2Rad;
        _swimTargetUnit = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)); // x = ขวา/ซ้าย, y = หน้า (ไม่ติดลบ)
        _directionTimer = directionChangeInterval;

        // สแนปช็อตจุดเป้าหมายเป็นตำแหน่งโลกนิ่งๆ ครั้งเดียวตอนนี้เลย (ไม่คำนวณสดทุกเฟรมอีกต่อไป)
        // กันปลาขยับกระตุกซ้าย-ขวานิดๆ ตลอดเวลาตามผู้เล่นหมุนตัว/ขยับเล็กน้อยทุกเฟรม (เหมือน noise) — ให้ไปถึงจุดแล้วนิ่งจริงจนกว่าจะครบ directionChangeInterval
        Vector3 basePos = player != null ? player.position : transform.position;
        Vector3 forward = HorizontalForward();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x); // ตั้งฉากกับ forward บนระนาบ XZ
        Vector3 offset = (right * _swimTargetUnit.x + forward * _swimTargetUnit.y) * MaxWanderRadius();
        _swimTargetPos = basePos + offset + Vector3.up * swimHeight;
    }

    // ── Dash ──────────────────────────────────────────────────
    private void TryStartDash()
    {
        if (_qteCooldownTimer > 0f) return;
        if (UnityEngine.Random.value > dashChancePerSecond * Time.deltaTime) return;

        State = FishState.Dashing;
        _dashTimer = dashDuration;
        PickNewSwimTarget(); // dash พุ่งไปทิศใหม่ในครึ่งวงหน้าผู้เล่น (ซ้าย/ขวา/หน้า) เร็วขึ้น — ไม่ถอยออกห่างผู้เล่นเด็ดขาด ถอยได้แค่ตอนถูกดึงแล้ว QTE พลาดเท่านั้น (ดู PullTowardsPlayer)

        // QTE เกิดได้เฉพาะปลา Tier Boss เท่านั้น (tier มาจาก FishData ที่สุ่มได้จากโซนตอนเริ่ม encounter ผ่าน SetFishData)
        WantsQteThisDash = qteEnabled && tier == FishTier.Boss && UnityEngine.Random.value <= qteChancePerDash;
        OnStateChanged?.Invoke(State);
    }

    /// <summary>Dash คือการพุ่งไปทิศใหม่ (ซ้าย/ขวา/หน้า) เร็วกว่าว่ายปกติ แต่ยังอยู่ในรัศมี MaxWanderRadius เดิม — ไม่มีการถอยออกห่างผู้เล่นเพิ่มเติมเหมือนของเก่า (Sea of Thieves ปลาไม่ถอยตอนว่าย ถอยได้แค่ตอนถูกดึงแล้วหลุดมือ)</summary>
    private void DashMove()
    {
        float speed = swimSpeed * dashSpeedMultiplier * Mathf.Lerp(1f, maxReelSpeedMultiplier, _pullProgress01);
        transform.position = Vector3.MoveTowards(transform.position, _swimTargetPos, speed * Time.deltaTime);
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

        // จัดหมวดตำแหน่งปลาเทียบผู้เล่นเป็น 1 ใน 3 ทิศ (ซ้าย/ขวา/หน้า) จากตำแหน่งจริงเสมอ ไม่ว่า state ไหน
        // (ก่อนหน้านี้เช็คแค่ตอน Dashing เท่านั้น ทำให้ Forward/กล้อง OverHead แทบไม่มีโอกาสโผล่เลยตอน Swimming/Tired)
        // ถ้าแกน z เด่นเกิน forwardDominance ถือว่าอยู่ข้างหน้า (ต้องดึงเมาส์ลง) ไม่งั้นจัดเป็นซ้าย/ขวาตามปกติ
        float denom = Mathf.Abs(local.x) + Mathf.Abs(local.z) + 0.0001f;
        float forwardRatio = Mathf.Abs(local.z) / denom;
        if (forwardRatio > forwardDominance)
        {
            RelativeDir = RelativeDirection.Forward;
            return;
        }

        RelativeDir = local.x < 0f ? RelativeDirection.Left : RelativeDirection.Right;
    }

    // ── Stamina ───────────────────────────────────────────────
    /// <summary>เรียกทุกเฟรมจาก PlayerReelController ตอนผู้เล่นดึงสวนทางถูกทิศ</summary>
    public void ApplyTension(float tension01)
    {
        tension01 = Mathf.Clamp01(tension01);
        if (tension01 > 0f) _lastActiveFrame = Time.frameCount;
        ModifyStamina(-tension01 * staminaDrainPerTensionUnit * Time.deltaTime);
    }

    public void ModifyStamina(float delta)
    {
        Stamina = Mathf.Clamp(Stamina + delta, 0f, maxStamina);

        if (State == FishState.Dashing) return;

        // เกณฑ์เข้า/ออก Tired ไม่เท่ากันโดยตั้งใจ: หลุด Tired ได้ต่อเมื่อ stamina ฟื้นเต็ม (100%) เท่านั้น
        // ไม่ใช่แค่ข้าม tiredThreshold นิดเดียวแล้วกลับไปว่ายใหม่ทันที
        if (State == FishState.Tired)
        {
            if (Stamina >= maxStamina)
            {
                State = FishState.Swimming;
                OnStateChanged?.Invoke(State);
            }
        }
        else if (Stamina <= tiredThreshold)
        {
            State = FishState.Tired;
            OnStateChanged?.Invoke(State);
        }
    }

    private void RegenStaminaIfIdle()
    {
        if (_lastActiveFrame == Time.frameCount) return;
        ModifyStamina(staminaRegenPerSecond * Time.deltaTime);
    }

    /// <summary>เรียกโดย FishingGameManager หลัง QTE จบ (สำเร็จหรือล้มเหลว) เพื่อกันปลา dash ซ้อน</summary>
    public void NotifyQteResolved()
    {
        _qteCooldownTimer = cooldownAfterQte;
    }
}
