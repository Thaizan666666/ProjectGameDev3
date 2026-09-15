using UnityEngine;

public class SellingFish : MonoBehaviour
{
    public static SellingFish Instance { get; private set; }

    [Tooltip("CartStorage ที่อยู่บนแท่นจอกขณะนี้ (auto-set จาก trigger)")]
    public CartStorage CurrentCart { get; private set; }

    /// <summary>มีรถเข็นวางอยู่บนแท่นจอกหรือไม่</summary>
    public bool IsCartOnDock => CurrentCart != null;

    /// <summary>มีปลาอยู่ในรถเข็นหรือไม่ (filter ItemType.FishItem)</summary>
    public bool HasFishInCart => CurrentCart != null && CurrentCart.HasFishItems();

    /// <summary>มูลค่ารวมของปลาที่จะขายได้ (filter ItemType.FishItem)</summary>
    public int TotalMoneyIncome => CurrentCart != null ? CurrentCart.GetFishTotalValue() : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Cart")) return;
        var cart = other.GetComponent<CartStorage>();
        if (cart != null)
        {
            CurrentCart = cart;
            Debug.Log($"[SellingFish] Cart docked: {cart.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Cart")) return;
        var cart = other.GetComponent<CartStorage>();
        if (cart == CurrentCart)
        {
            Debug.Log($"[SellingFish] Cart undocked: {cart.name}");
            CurrentCart = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}