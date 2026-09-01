using UnityEngine;

public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance {get; private set;}
    [SerializeField] private PlayerWallet walletComponent;
    private IWallet wallet;

    void Awake()
    {
        Instance = this;
        wallet = walletComponent;

        if (wallet == null)
            Debug.LogWarning($"{name}: missing PlayerWallet reference");
    }

    public bool TryRequestTrading()
    {
        Debug.Log("TryRequestTrading");
        return true;
    }
}
