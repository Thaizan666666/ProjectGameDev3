using UnityEngine;

// Put on a Collider GameObject (trigger or solid). Marks slotIndex active on player contact.
[RequireComponent(typeof(Collider))]
public class FishSlotTrigger : MonoBehaviour
{
    [SerializeField] private ActiveFishSlot activeFishSlot;
    [SerializeField] private int slotIndex;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => TrySetActive(other);
    private void OnCollisionEnter(Collision collision) => TrySetActive(collision.collider);

    private void OnTriggerExit(Collider other) => TryClearActive(other);
    private void OnCollisionExit(Collision collision) => TryClearActive(collision.collider);

    private void TrySetActive(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (activeFishSlot == null)
        {
            Debug.LogWarning($"{name}: missing ActiveFishSlot");
            return;
        }

        activeFishSlot.SetActiveSlot(slotIndex);
    }

    private void TryClearActive(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (activeFishSlot == null) return;

        activeFishSlot.ClearActiveSlot(slotIndex);
    }
}
