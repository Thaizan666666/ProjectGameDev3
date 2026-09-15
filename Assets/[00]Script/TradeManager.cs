using UnityEngine;
using Yarn.Unity;

public class TradeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static TradeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerWallet walletComponent;
    private IWallet wallet;

    // ── Cache ผลคำนวณล่าสุด ──────────────────────────────────
    private int _lastCalculatedTotal;

    private void Awake()
    {
        Instance = this;
        wallet = walletComponent;

        if (wallet == null)
            Debug.LogWarning($"[TradeManager] {name}: missing PlayerWallet reference");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────────
    // Internal Logic
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// คำนวณมูลค่าปลาในรถเข็น
    /// </summary>
    /// <returns>
    /// 0 = ไม่มีรถเข็น / รถไม่มีปลา
    /// &gt;0 = มูลค่ารวมของปลาที่ขายได้
    /// </returns>
    private int CalculateFishSale()
    {
        var selling = SellingFish.Instance;
        if (selling == null || !selling.IsCartOnDock)
        {
            Debug.Log("[TradeManager] CalculateFishSale: ไม่มีรถเข็นวางอยู่");
            return 0;
        }

        if (!selling.HasFishInCart)
        {
            Debug.Log("[TradeManager] CalculateFishSale: รถเข็นว่างเปล่า ไม่มีปลา");
            return 0;
        }

        int total = selling.TotalMoneyIncome;
        Debug.Log($"[TradeManager] CalculateFishSale: มูลค่าปลา = {total}");
        return total;
    }

    /// <summary>
    /// ยืนยันการขาย — เอาเงินเข้า wallet + ล้างปลาออกจากรถ
    /// </summary>
    private bool ExecuteFishSale()
    {
        var selling = SellingFish.Instance;
        if (selling == null || !selling.IsCartOnDock || !selling.HasFishInCart)
        {
            Debug.LogWarning("[TradeManager] ExecuteFishSale: ไม่สามารถขายได้ (ไม่มีรถ/ไม่มีปลา)");
            return false;
        }

        int total = selling.TotalMoneyIncome;
        wallet.Add(total);
        selling.CurrentCart.ClearFishItems();

        Debug.Log($"[TradeManager] ExecuteFishSale: ขายสำเร็จ +{total} เหรียญ");
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // Yarn API — ใช้จาก Finn_Selling.yarn
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Yarn Command: &lt;&lt;tryCalculateFishSale&gt;&gt;
    /// เรียกก่อน ค่อย query ผ่าน calculateFishSale
    /// </summary>
    [YarnCommand("tryCalculateFishSale")]
    public static void TryCalculateFishSale()
    {
        var inst = Instance;
        if (inst == null) return;
        inst._lastCalculatedTotal = inst.CalculateFishSale();
    }

    /// <summary>
    /// Yarn Function: &lt;&lt;set $total = calculateFishSale()&gt;&gt;
    /// ค่าที่คำนวณจาก tryCalculateFishSale ล่าสุด
    /// </summary>
    [YarnFunction("calculateFishSale")]
    public static int GetCalculateFishSale()
    {
        return Instance != null ? Instance._lastCalculatedTotal : 0;
    }

    /// <summary>
    /// Yarn Command: &lt;&lt;confirmFishSale&gt;&gt;
    /// ยืนยันขาย — เอาเงินเข้า wallet + ล้างปลา
    /// </summary>
    [YarnCommand("confirmFishSale")]
    public static void ConfirmFishSale()
    {
        if (Instance == null) return;
        Instance.ExecuteFishSale();
    }
}