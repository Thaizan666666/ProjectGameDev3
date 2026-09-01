using UnityEngine;

public class PlayerWallet : MonoBehaviour, IWallet
{
    [SerializeField] private int money;
    public int Money => money;

    public bool TrySpend(int amount)
    {
        if(amount < 0 || money < amount) return false;

        money -= amount;
        return true;
    }

    public void Add(int amount) => money += Mathf.Max(amount, 0);

}
