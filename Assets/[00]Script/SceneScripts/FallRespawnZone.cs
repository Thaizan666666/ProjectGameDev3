using UnityEngine;
using KinematicCharacterController.Examples;

// วาง Collider (isTrigger) คลุมพื้นที่ใต้แมพ — player ตกลงมาโดน trigger นี้แล้ว fade ดำ
// แล้วเทเลพอร์ตกลับ Checkpoint ล่าสุด (Checkpoint.Current) หรือ defaultSpawnPoint ถ้ายังไม่เคยแตะ checkpoint
[RequireComponent(typeof(Collider))]
public class FallRespawnZone : MonoBehaviour
{
    [SerializeField] private ExampleCharacterController player;
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (player == null)
            player = other.GetComponentInParent<ExampleCharacterController>();

        Respawn();
    }

    private void Respawn()
    {
        Transform target = Checkpoint.Current != null ? Checkpoint.Current : defaultSpawnPoint;

        if (player == null || target == null)
        {
            Debug.LogWarning($"[FallRespawnZone:{name}] missing player หรือ checkpoint/defaultSpawnPoint");
            return;
        }

        if (FadeBlackScreen.Instance == null)
        {
            Teleport(target);
            return;
        }

        FadeBlackScreen.Instance.FadeIn(() =>
        {
            Teleport(target);
            FadeBlackScreen.Instance.FadeOut();
        });
    }

    private void Teleport(Transform target)
    {
        player.Motor.SetPositionAndRotation(target.position, target.rotation);
        player.Motor.BaseVelocity = Vector3.zero;
    }
}
