using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bot เดินวน (patrol) อิสระในแมพด้วย NavMeshAgent — ไม่ follow ใคร แค่สุ่มจุดใหม่รอบ ๆ
/// origin ไปเรื่อย ๆ ไม่มีที่สิ้นสุด (แต่ละรอบอาจไปทางเดิมหรือทางใหม่ก็ได้ เพราะสุ่มทุกครั้ง)
/// Attach: Bot GameObject ที่มี NavMeshAgent
/// </summary>
/// <summary>จุด waypoint 1 จุดในกราฟ + รายชื่อ index ของ node ที่เดินถึงกันได้ตรง ๆ จากจุดนี้</summary>
[System.Serializable]
public class WaypointNode
{
    public Transform point;
    public int[] neighbors;
}

[RequireComponent(typeof(NavMeshAgent))]
public class BotWander : MonoBehaviour
{
    [Tooltip("รัศมีที่สุ่มจุดเดินรอบจุดเริ่มต้น (เมตร)")]
    [SerializeField] private float wanderRadius = 15f;
    [Tooltip("หยุดพักกี่วินาทีก่อนสุ่มจุดใหม่ (0 = ไม่หยุด เดินต่อทันที)")]
    [SerializeField] private float idleTimeAtPoint = 1f;
    [Tooltip("กราฟ waypoint แบบกำหนด neighbor เองต่อจุด (รองรับโครงสร้างอะไรก็ได้ ไม่ใช่แค่ grid) — ถ้าใส่ไว้จะเดินตามกราฟนี้เท่านั้น ไม่สุ่มในรัศมี wanderRadius อีก (ปล่อยว่างได้ถ้าอยากใช้แบบสุ่มในรัศมีตามเดิม)")]
    [SerializeField] private WaypointNode[] graphNodes;
    [Tooltip("จำกัดให้เดินได้เฉพาะ node index พวกนี้เท่านั้น (เช่น 5,6,10) — ปล่อยว่างได้ถ้าอยากเดินได้ทั้งกราฟตามปกติ")]
    [SerializeField] private int[] allowedNodeIndices;
    private int currentNodeIndex = -1; // index ปัจจุบันใน graphNodes — -1 = ยังไม่เริ่ม
    private int previousNodeIndex = -1; // index ก่อนหน้า — ห้ามเดินถอยกลับจุดนี้ใน 1 รอบถัดไป

    private NavMeshAgent agent;
    private Vector3 origin;
    private float idleTimer;
    private bool isWaiting;
    private bool isManualOverride; // true ตอนถูกสั่ง GoTo ตรง ๆ — ไม่สุ่มจุดเองจนกว่าจะถึงจุดนั้น

    [Header("ทดสอบจาก Inspector")]
    [Tooltip("จุดที่จะสั่งให้เดินไป (world position)")]
    [SerializeField] private Vector3 debugGoToPoint;
    [Tooltip("ติ๊กเพื่อสั่งไปจุดด้านบนทันที (one-shot — ติ๊กแล้วจะ auto ปลดให้เอง)")]
    [SerializeField] private bool debugApplyGoTo;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        origin = transform.position;
    }

    private void Start()
    {
        PickNewDestination();
    }

    /// <summary>สั่ง Bot ให้เดินไปจุดที่กำหนดตรง ๆ (เรียกจาก script อื่นได้) — จะหยุดสุ่มจุดเองจนกว่าจะถึง
    /// แล้วกลับไปเดินวนสุ่มต่อตามปกติ</summary>
    public void GoTo(Vector3 destination)
    {
        isManualOverride = true;
        isWaiting = false;
        agent.SetDestination(destination);
    }

    private void Update()
    {
        if (debugApplyGoTo)
        {
            debugApplyGoTo = false;
            GoTo(debugGoToPoint);
        }

        if (isWaiting)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isWaiting = false;
                PickNewDestination();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isManualOverride = false; // ถึงจุดที่ถูกสั่งแล้ว -> กลับไปเดินวนสุ่มต่อตามปกติ

            if (idleTimeAtPoint > 0f)
            {
                isWaiting = true;
                idleTimer = idleTimeAtPoint;
            }
            else
            {
                PickNewDestination();
            }
        }
    }

    private void PickNewDestination()
    {
        if (graphNodes != null && graphNodes.Length > 0)
        {
            int nextIndex = PickNextGraphIndex();
            if (nextIndex < 0) return;

            Transform target = graphNodes[nextIndex].point;
            if (target != null)
            {
                agent.SetDestination(target.position);
                previousNodeIndex = currentNodeIndex;
                currentNodeIndex = nextIndex;
            }
            return;
        }

        // ไม่มี graph กำหนดไว้ -> สุ่มจุดในวงกลมรัศมี wanderRadius รอบ origin แล้ว snap ลงบน NavMesh จริงที่ใกล้ที่สุด
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>เลือก node ถัดไปจาก neighbors ของ node ปัจจุบัน (ตามที่กำหนดไว้ใน graphNodes[i].neighbors)
    /// ตัด node ก่อนหน้าออกก่อนเสมอ (ห้ามเดินถอยกลับ) ถ้าตัดแล้วไม่เหลือ neighbor เลย ใช้ neighbor ทั้งหมดแทน</summary>
    private bool IsAllowed(int index) =>
        allowedNodeIndices == null || allowedNodeIndices.Length == 0 || System.Array.IndexOf(allowedNodeIndices, index) >= 0;

    private int PickNextGraphIndex()
    {
        // เริ่มจาก node 0 ถ้ายังไม่เคยเดิน ยกเว้นมี allowedNodeIndices กำหนดไว้ -> เริ่มจากตัวแรกใน list นั้นแทน
        int current = currentNodeIndex >= 0
            ? currentNodeIndex
            : (allowedNodeIndices != null && allowedNodeIndices.Length > 0 ? allowedNodeIndices[0] : 0);
        if (current >= graphNodes.Length) current = 0;

        int[] neighbors = graphNodes[current].neighbors;
        if (neighbors == null || neighbors.Length == 0) return -1;

        System.Collections.Generic.List<int> allowedNeighbors = new System.Collections.Generic.List<int>();
        foreach (var n in neighbors) if (IsAllowed(n)) allowedNeighbors.Add(n);
        if (allowedNeighbors.Count == 0) return current; // ไม่มี neighbor ที่อนุญาตเลย -> อยู่ที่เดิม

        System.Collections.Generic.List<int> candidates = new System.Collections.Generic.List<int>(allowedNeighbors);
        candidates.RemoveAll(n => n == previousNodeIndex);
        if (candidates.Count == 0) candidates.AddRange(allowedNeighbors); // ตันจริงภายในขอบเขตที่อนุญาต -> ยอมถอยกลับ

        return candidates[Random.Range(0, candidates.Count)];
    }
}
