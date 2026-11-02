using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<int> OnMoneyChanged;

    public int WalletPlayer {get; private set;}

    void Awake()
    {
        QualitySettings.vSyncCount = 0; // ต้องปิดก่อน ไม่งั้น VSync ทับ targetFrameRate
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        OnMoneyChanged?.Invoke(WalletPlayer);
    }
}
