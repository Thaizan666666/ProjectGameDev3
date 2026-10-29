using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using Unity.AI.Navigation;

// Editor-only: ต้นไม้ที่ paint บน Terrain ไม่ถูก NavMeshSurface มองเห็นตอน bake เลย (ข้อจำกัดจริงของ Unity)
// สร้าง Collider จริงค้างไว้ในฉาก (ครั้งเดียว ตอน Edit mode) ที่ตำแหน่งต้นไม้แต่ละต้น แล้ว bake NavMesh ทับ
// จำกัดแค่ต้นไม้ที่อยู่ในขอบเขตหมู่บ้าน (polygon 4 จุด) เท่านั้น ไม่ทำทั้งแมพ
public static class TerrainTreeColliderBaker
{
    // ขอบเขตหมู่บ้าน (X, Z) ตามลำดับรอบรูป (quadrilateral)
    private static readonly Vector2[] VillageBounds =
    {
        new Vector2(172f, 360f),
        new Vector2(337f, 353f),
        new Vector2(337f, 163f),
        new Vector2(168.3f, 153.2f),
    };

    [MenuItem("Tools/Terrain/Bake Tree Colliders + NavMesh (Village Only)")]
    public static void BakeTreeCollidersAndNavMesh()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { Debug.LogError("[TerrainTreeColliderBaker] ไม่เจอ active Terrain"); return; }

        TerrainData data = terrain.terrainData;

        Transform oldContainer = terrain.transform.Find("TreeColliders");
        if (oldContainer != null) Object.DestroyImmediate(oldContainer.gameObject);

        GameObject container = new GameObject("TreeColliders");
        container.transform.SetParent(terrain.transform, false);

        float[] protoRadius = MeasurePrototypeRadii(data);
        const float trunkRadiusScale = 0.35f;

        int spawned = 0;
        int skippedOutsideVillage = 0;
        foreach (var instance in data.treeInstances)
        {
            int protoIndex = instance.prototypeIndex;
            if (protoIndex < 0 || protoIndex >= protoRadius.Length) continue;

            float radius = protoRadius[protoIndex];
            if (radius <= 0f) continue; // ไม่มี collider (หญ้า/หิน) -> ข้าม

            Vector3 worldPos = terrain.transform.position + Vector3.Scale(instance.position, data.size);

            if (!IsInsidePolygon(new Vector2(worldPos.x, worldPos.z), VillageBounds))
            {
                skippedOutsideVillage++;
                continue;
            }

            GameObject colliderGO = new GameObject("TreeCollider");
            colliderGO.transform.SetParent(container.transform, false);
            colliderGO.transform.position = worldPos;

            CapsuleCollider capsule = colliderGO.AddComponent<CapsuleCollider>();
            capsule.radius = radius * trunkRadiusScale;
            capsule.height = 10f;

            var modifier = colliderGO.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");

            spawned++;
        }

        Debug.Log($"[TerrainTreeColliderBaker] ในหมู่บ้าน: สร้าง collider {spawned} ต้น (ข้ามนอกเขต {skippedOutsideVillage} ต้น) — เริ่ม bake NavMesh...");

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("[TerrainTreeColliderBaker] Bake NavMesh เสร็จแล้ว (ตัดรูต้นไม้เฉพาะในหมู่บ้านแบบ static)");
        }
        else
        {
            Debug.LogWarning("[TerrainTreeColliderBaker] ไม่เจอ NavMeshSurface ในฉาก");
        }

        EditorUtility.SetDirty(terrain.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
    }

    // ray-casting algorithm มาตรฐานสำหรับเช็คจุดอยู่ใน polygon นูน/เว้าก็ได้
    private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private static float[] MeasurePrototypeRadii(TerrainData data)
    {
        var prototypes = data.treePrototypes;
        float[] radii = new float[prototypes.Length];

        for (int i = 0; i < prototypes.Length; i++)
        {
            GameObject prefab = prototypes[i].prefab;
            if (prefab == null) { radii[i] = 0f; continue; }

            GameObject temp = Object.Instantiate(prefab);
            temp.transform.position = Vector3.zero;
            Collider col = temp.GetComponentInChildren<Collider>();

            radii[i] = col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 0f;

            Object.DestroyImmediate(temp);
        }

        return radii;
    }
}
