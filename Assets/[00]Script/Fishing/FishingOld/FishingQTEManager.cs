// ─────────────────────────────────────────────────────────────
// FishingQTEManager.cs
// สุ่มลำดับปุ่มกด (WASD) ตอนปลา dash — ให้ผู้เล่นกดตามภายในเวลาที่กำหนด
// ใช้ Unity Input System (Keyboard.current) โดยตรง เพราะ minigame
// เป็นการ "แย่ง" input ชั่วคราว ไม่ต้องผูกกับ .inputactions ของเกมหลัก
// Attach: GameObject เดียวกับ FishingGameManager
// ─────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct QtePromptInfo
{
    public Key expectedKey;
    public int index;
    public int total;
    public float timeRemaining;
    public float timePerKey;
}

public class FishingQTEManager : MonoBehaviour
{
    private static readonly Key[] PossibleKeys = { Key.W, Key.A, Key.S, Key.D };

    [Header("QTE Tuning")]
    [Tooltip("จำนวนปุ่มในลำดับ QTE แต่ละรอบ (ปลา Rare)")]
    [SerializeField] private int sequenceLength = 4;
    [Tooltip("จำนวนปุ่มในลำดับ QTE ตอนสู้กับปลา Boss (ยากกว่า Rare)")]
    [SerializeField] private int bossSequenceLength = 7;
    [Tooltip("เวลาที่ให้กดปุ่มแต่ละตัว (วินาที)")]
    [SerializeField] private float timePerKey = 0.8f;

    public event Action<QtePromptInfo[]> OnQteStarted;
    public event Action<QtePromptInfo> OnPromptChanged;
    public event Action<bool> OnQteResult;

    public bool IsActive { get; private set; }

    private Key[] _sequence;
    private int _index;
    private float _timer;

    /// <summary>เริ่ม QTE โดยความยาวลำดับปุ่มขึ้นกับระดับปลา (ปลา Boss จะยาว/เยอะกว่าปกติ)</summary>
    public bool StartQte(FishTier tier)
    {
        if (IsActive) return false;

        int length = tier == FishTier.Boss ? bossSequenceLength : sequenceLength;

        _sequence = new Key[length];
        for (int i = 0; i < length; i++)
            _sequence[i] = PossibleKeys[UnityEngine.Random.Range(0, PossibleKeys.Length)];

        _index = 0;
        _timer = timePerKey;
        IsActive = true;

        var infos = new QtePromptInfo[length];
        for (int i = 0; i < length; i++)
            infos[i] = MakePrompt(i);

        OnQteStarted?.Invoke(infos);
        OnPromptChanged?.Invoke(MakePrompt(_index));
        return true;
    }

    private void Update()
    {
        if (!IsActive) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        _timer -= Time.deltaTime;

        foreach (var key in PossibleKeys)
        {
            if (!keyboard[key].wasPressedThisFrame) continue;

            if (key == _sequence[_index])
            {
                _index++;
                if (_index >= _sequence.Length)
                {
                    EndQte(true);
                    return;
                }

                _timer = timePerKey;
                OnPromptChanged?.Invoke(MakePrompt(_index));
            }
            else
            {
                EndQte(false);
                return;
            }
        }

        if (_timer <= 0f)
        {
            EndQte(false);
            return;
        }

        var current = MakePrompt(_index);
        current.timeRemaining = _timer;
        OnPromptChanged?.Invoke(current);
    }

    private void EndQte(bool success)
    {
        IsActive = false;
        OnQteResult?.Invoke(success);
    }

    /// <summary>ยกเลิก QTE ทันทีโดยไม่ยิง OnQteResult — ใช้ตอน encounter จบไปแล้ว (จับปลาได้/เบ็ดขาด) ระหว่าง QTE ยังค้างอยู่ กัน QTE เก่ายิง result มาใส่ encounter ใหม่</summary>
    public void CancelQte()
    {
        IsActive = false;
    }

    private QtePromptInfo MakePrompt(int index)
    {
        return new QtePromptInfo
        {
            expectedKey = _sequence[index],
            index = index,
            total = _sequence.Length,
            timeRemaining = _timer,
            timePerKey = timePerKey
        };
    }
}
