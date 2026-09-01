using UnityEngine;

public interface IWallet
{
    int Money {get;}
    bool TrySpend(int amount);
    void Add(int amount);
}
