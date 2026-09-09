using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStackSize;
    public GameObject itemPrefab;
    public GameObject handItemPrefab;
}
