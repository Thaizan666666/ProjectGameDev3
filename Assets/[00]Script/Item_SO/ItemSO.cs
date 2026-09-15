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
}
