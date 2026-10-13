using UnityEngine;

/// <summary>
/// Added to each generated room by RoomChainGenerator.
/// The first time the player walks into the room, one enemy is spawned at
/// every "Enemy spawn" marker inside it. Each room only triggers once.
/// </summary>
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

            // Not parented to the room: the map is turned onto the ground, and
            // a parented enemy would be turned with it.
            GameObject enemy = Instantiate(
                prefab, position, Quaternion.identity, enemyContainer);

            // Give the enemy its target straight away.
            // The piranha prefab keeps EnemyAI on a child object, so search
            // the whole prefab, not only the root.
            EnemyAI ai = enemy.GetComponentInChildren<EnemyAI>(true);
            if (ai != null)
                ai.player = player;
        }
    }

    // Matches "Enemy spawn", "EnemySpawn", "enemy_spawn 2", ... (case, spaces
    // and underscores ignored).
    private static bool IsEnemyMarker(string markerName)
    {
        string cleaned = markerName.Replace(" ", "").Replace("_", "");
        return cleaned.StartsWith("enemyspawn",
            System.StringComparison.OrdinalIgnoreCase);
    }
}