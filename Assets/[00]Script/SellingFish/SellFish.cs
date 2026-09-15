using UnityEngine;

public class SellingFish : MonoBehaviour
{
    public static SellingFish Instance {get; private set;}


    void Start()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        bool isCart = other.GetComponent<CartStorage>();

        Debug.Log($"isCart is {isCart}");
    }
}
