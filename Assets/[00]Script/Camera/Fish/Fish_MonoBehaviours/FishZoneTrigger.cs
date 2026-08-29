using PlayerNormal.Project_wide;
using UnityEngine;

// Put on a trigger Collider covering a FishZone's fishing spot.
// Marks the zone active on PlayerFishing while the player stands inside.
[RequireComponent(typeof(Collider))]
public class FishZoneTrigger : MonoBehaviour
{
    [SerializeField] private FishZone zone;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => TrySetZone(other);
    private void OnTriggerExit(Collider other) => TryClearZone(other);

    private void TrySetZone(Collider other)
    {
        if (!other.CompareTag(playerTag) || zone == null) return;

        var fishing = other.GetComponentInParent<PlayerFishing>();
        if (fishing == null)
        {
            Debug.LogWarning($"{name}: player has no PlayerFishing component");
            return;
        }

        fishing.SetCurrentZone(zone);
    }

    private void TryClearZone(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var fishing = other.GetComponentInParent<PlayerFishing>();
        if (fishing != null) fishing.ClearCurrentZone(zone);
    }
}
