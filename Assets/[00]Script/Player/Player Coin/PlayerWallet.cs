using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour, IWallet
{
    public static PlayerWallet Instance;
    public event Action<int> OnMoneyChanged;
    [SerializeField] private int money;
    public int Money => money;

    void Awake()
    {
        OnMoneyChanged?.Invoke(money);
    }

    public bool TrySpend(int amount)
    {
        if(amount < 0 || money < amount) return false;

        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    public void Add(int amount)
    {
        money += Mathf.Max(amount, 0);
        OnMoneyChanged?.Invoke(money);
    }

}
