using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<int> OnMoneyChanged;

    public int WalletPlayer {get; private set;}

    void Update()
    {
        OnMoneyChanged?.Invoke(WalletPlayer);
    }
}
