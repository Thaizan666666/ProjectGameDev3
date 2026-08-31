using PlayerNormal.Project_wide;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance {get; private set;}
    [SerializeField] private PlayerWallet walletComponent;
    private IWallet wallet;

    private void Awake()
    {
        Instance = this;
        wallet = walletComponent;

        if (wallet == null)
            Debug.LogWarning($"{name}: missing PlayerWallet reference");
    }

    public bool TryRequestUpgrade(Building building)
    {
        int cost = building.data.GetUpgradeCost(building.currentLevel + 1);
        if (cost < 0) return false;

        if (!wallet.TrySpend(cost))
        {
            Debug.LogWarning("Not enough money to upgrade this building.");
            return false;
        } 

        building.Upgrade();
        return true;
    }
}
