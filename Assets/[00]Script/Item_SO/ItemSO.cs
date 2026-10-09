using UnityEngine;

public enum ItemSize
{
    ExceptSize,
    SmallItem,
    BigItem
}

public enum ItemType
{
    Undefine,
    EquipmentItem,
    FishItem,
    QuestItem
}

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStackSize;
    public int sellPrice;        // ราคาขาย default = 0 (ปลาจะถูก overwrite ด้วย FishData.Price)
    public GameObject itemPrefab;
    public GameObject handItemPrefab;
    public ItemSize itemSize = ItemSize.SmallItem;
    public ItemType itemType = ItemType.Undefine;

    [Header("Fishing Minigame (ใช้เฉพาะเบ็ด/คันเบ็ด EquipmentItem — item อื่นปล่อยค่า default ไว้ ไม่มีผล)")]
    [Tooltip("ตัวคูณแรงดึง ส่งเข้า FishingReelMinigame.pullStrength — 1 = ปกติ 100%")]
    public float hookPullStrengthMultiplier = 1f;
    [Tooltip("% ลด holdTimeToTriggerQte ของ FishingReelMinigame (0-1, เช่น 0.2 = ลด 20%)")]
    [Range(0f, 0.9f)]
    public float hookHoldTimeReductionPercent = 0f;
}
