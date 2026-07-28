using UnityEngine;

public class Fish : MonoBehaviour, IFishDataReceiver
{
    public FishData data;
    public int weight;

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

        RollWeight();

        Debug.Log($"Fish : {data.fishName}\nTier : {data.fishTier}\nWeight : {weight}\nID : {data.fishID}");
    }

    private void RollWeight()
    {
        weight = RandomProvider.Current.Range(data.minWeight, data.maxWeight + 1);
    }
}
