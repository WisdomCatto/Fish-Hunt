using System.Collections.Generic;
using UnityEngine;

public class RoomEnemyTrigger : MonoBehaviour
{
    private Transform player;
    private GameObject[] enemyPrefabs;
    private Transform enemyContainer;
    private float enterMargin;
    private float triggerDistance;
    private float spawnHeightOffset;
    private bool verbose;

    private MeshRenderer[] renderers;
    private readonly List<Transform> markers = new List<Transform>();
    private bool triggered;

    public int MarkerCount
    {
        get { return markers.Count; }
    }

    public void Setup(
        Transform player,
        GameObject[] enemyPrefabs,
        Transform enemyContainer,
        float enterMargin,
        float triggerDistance,
        float spawnHeightOffset,
        bool verbose)
    {
        this.player = player;
        this.enemyPrefabs = enemyPrefabs;
        this.enemyContainer = enemyContainer;
        this.enterMargin = enterMargin;
        this.triggerDistance = triggerDistance;
        this.spawnHeightOffset = spawnHeightOffset;
        this.verbose = verbose;

        renderers = GetComponentsInChildren<MeshRenderer>(true);

        markers.Clear();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (IsEnemyMarker(t.name))
                markers.Add(t);
        }
    }

    private void Update()
    {
        if (triggered || player == null || markers.Count == 0)
            return;

        if (PlayerIsInside() || PlayerIsNearMarker())
        {
            triggered = true;
            SpawnEnemies();
        }
    }

    private bool PlayerIsInside()
    {
        Vector3 p = player.position;

        foreach (MeshRenderer r in renderers)
        {
            Bounds b = r.bounds;

            float marginX = Mathf.Min(enterMargin, b.extents.x * 0.5f);
            float marginZ = Mathf.Min(enterMargin, b.extents.z * 0.5f);

            if (p.x > b.min.x + marginX && p.x < b.max.x - marginX &&
                p.z > b.min.z + marginZ && p.z < b.max.z - marginZ)
            {
                return true;
            }
        }

        return false;
    }

    // True if the player is within triggerDistance of any Enemy spawn marker
    private bool PlayerIsNearMarker()
    {
        if (triggerDistance <= 0f)
            return false;

        Vector3 p = player.position;
        float limit = triggerDistance * triggerDistance;

        foreach (Transform m in markers)
        {
            Vector3 d = m.position - p;
            d.y = 0f;

            if (d.sqrMagnitude <= limit)
                return true;
        }

        return false;
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        int spawned = 0;

        foreach (Transform marker in markers)
        {
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null)
                continue;

            Vector3 position = marker.position + Vector3.up * spawnHeightOffset;
            GameObject enemy = Instantiate(
                prefab, position, Quaternion.identity, enemyContainer);

            EnemyAI ai = enemy.GetComponentInChildren<EnemyAI>(true);
            if (ai != null)
                ai.player = player;

            spawned++;
        }

        if (verbose)
        {
            Debug.Log("Player reached " + name + ": spawned " + spawned +
                " enemy(s) at " + markers.Count + " Enemy spawn marker(s).", this);
        }
    }

    private static bool IsEnemyMarker(string markerName)
    {
        string cleaned = markerName.Replace(" ", "").Replace("_", "");
        return cleaned.StartsWith("enemyspawn",
            System.StringComparison.OrdinalIgnoreCase);
    }
}