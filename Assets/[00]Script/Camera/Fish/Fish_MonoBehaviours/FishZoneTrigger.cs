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

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[FishZoneTrigger:{name}] OnTriggerEnter <- {other.name} (tag: \"{other.tag}\", ต้องการ: \"{playerTag}\")");
        TrySetZone(other);
    }

    private void OnTriggerExit(Collider other) => TryClearZone(other);

    private void TrySetZone(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            Debug.LogWarning($"[FishZoneTrigger:{name}] tag ไม่ตรง — \"{other.name}\" มี tag \"{other.tag}\" ไม่ใช่ \"{playerTag}\"");
            return;
        }

        if (zone == null)
        {
            Debug.LogWarning($"[FishZoneTrigger:{name}] ไม่ได้ผูก FishZone (field \"Zone\") ไว้ใน Inspector");
            return;
        }

        var fishing = other.GetComponentInParent<PlayerFishing>();
        if (fishing == null)
        {
            Debug.LogWarning($"[FishZoneTrigger:{name}] \"{other.name}\" (หรือ parent ของมัน) ไม่มี component PlayerFishing");
            return;
        }

        fishing.SetCurrentZone(zone);
        Debug.Log($"[FishZoneTrigger:{name}] ตั้งค่า currentZone สำเร็จ -> {zone.ZoneName}");
    }

    private void TryClearZone(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var fishing = other.GetComponentInParent<PlayerFishing>();
        if (fishing != null) fishing.ClearCurrentZone(zone);
    }
}
