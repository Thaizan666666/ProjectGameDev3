using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStackSize;
    public int sellPrice;        // ราคาขาย default = 0 (ปลาจะถูก overwrite ด้วย FishData.Price)
    public GameObject itemPrefab;
    public GameObject handItemPrefab;
}
