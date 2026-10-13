using UnityEngine;

public class RoomEnemyTrigger : MonoBehaviour
{
    private Transform player;
    private GameObject[] enemyPrefabs;
    private Transform enemyContainer;
    private float enterMargin;
    private float spawnHeightOffset;

    private MeshRenderer[] renderers;
    private bool triggered;

    public void Setup(
        Transform player,
        GameObject[] enemyPrefabs,
        Transform enemyContainer,
        float enterMargin,
        float spawnHeightOffset)
    {
        this.player = player;
        this.enemyPrefabs = enemyPrefabs;
        this.enemyContainer = enemyContainer;
        this.enterMargin = enterMargin;
        this.spawnHeightOffset = spawnHeightOffset;

        renderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    private void Update()
    {
        if (triggered || player == null)
            return;

        if (PlayerIsInside())
        {
            triggered = true;
            SpawnEnemies();
        }
    }

    // The player counts as inside once they are past the room's walls by
    // enterMargin on the ground (X/Z). Height is ignored.
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

    private void SpawnEnemies()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!IsEnemyMarker(t.name))
                continue;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null)
                continue;

            Vector3 position = t.position + Vector3.up * spawnHeightOffset;

            GameObject enemy = Instantiate(
                prefab, position, Quaternion.identity, enemyContainer);

            EnemyAI ai = enemy.GetComponentInChildren<EnemyAI>(true);
            if (ai != null)
                ai.player = player;
        }
    }

    private static bool IsEnemyMarker(string markerName)
    {
        string cleaned = markerName.Replace(" ", "").Replace("_", "");
        return cleaned.StartsWith("enemyspawn",
            System.StringComparison.OrdinalIgnoreCase);
    }
}