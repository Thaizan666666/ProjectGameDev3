using UnityEngine;

namespace Game.Water
{
    public class CoastalWaveScatter : MonoBehaviour
    {
        [SerializeField] private GameObject wavePrefab;
        [SerializeField] private Terrain terrain;
        [SerializeField] private Transform waterCenter;
        [SerializeField] private int sampleDirections = 72;
        [SerializeField] private float maxSearchRadius = 300f;
        [SerializeField] private float waterHeightMargin = 0.2f;
        [SerializeField] private int pointSpacing = 3;

        private void Awake()
        {
            if (terrain == null) terrain = Terrain.activeTerrain;
            if (terrain == null || waterCenter == null || wavePrefab == null) return;

            var waterRenderer = waterCenter.GetComponent<Renderer>();
            float waterY = waterRenderer != null ? waterRenderer.bounds.center.y : waterCenter.position.y;
            Vector3 center = waterCenter.position;

            float angleStep = 360f / sampleDirections;
            int spawned = 0;

            for (int i = 0; i < sampleDirections; i += pointSpacing)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                for (float r = 5f; r <= maxSearchRadius; r += 1f)
                {
                    Vector3 p = center + dir * r;
                    float h = terrain.SampleHeight(p) + terrain.transform.position.y;
                    if (h > waterY + waterHeightMargin)
                    {
                        p.y = waterY;
                        Instantiate(wavePrefab, p, Quaternion.identity, transform);
                        spawned++;
                        break;
                    }
                }
            }

            Debug.Log($"[CoastalWaveScatter] วางเสียงคลื่นตามชายฝั่ง {spawned} จุด");
        }
    }
}
