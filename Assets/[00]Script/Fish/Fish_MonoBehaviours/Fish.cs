using UnityEngine;

public class Fish : MonoBehaviour, IFishDataReceiver
{
    public FishData data;
    public int weight;
    private GameObject newPrefab;

    private void Start()
    {
        if (data != null) RollWeight();
    }

    public void SetData(FishData newData)
    {
        data = newData;

        if (data == null)
        {
            Debug.LogWarning($"{name}: null data");
            return;
        }

        newPrefab = data.Prefab;
        RollWeight();
        ReplaceChildByIndex(0);

        Debug.Log($"Fish : {data.fishName}\nTier : {data.fishTier}\nWeight : {weight}\nID : {data.fishID}");
    }

    public void ReplaceChildByIndex(int index)
{
    if (index < 0 || index >= transform.childCount)
    {
        Debug.LogWarning("Index incorrect");
        return;
    }

    if (newPrefab == null)
    {
        Debug.LogWarning($"{name}: newPrefab is null");
        return;
    }

    Transform child = transform.GetChild(index);
    Vector3 pos = child.position;
        Quaternion rot = child.rotation;
        Transform parent = child.parent;
        int siblingIndex = child.GetSiblingIndex();

        Destroy(child.gameObject);
        GameObject newChild = Instantiate(newPrefab, pos, rot, parent);
        newChild.transform.SetSiblingIndex(siblingIndex);
}

    private void RollWeight()
    {
        weight = RandomProvider.Current.Range(data.minWeight, data.maxWeight + 1);
    }
}
