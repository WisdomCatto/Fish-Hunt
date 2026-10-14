using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public int attackDamage = 20;
    public LayerMask enemyLayers;
    public bool showAttackSphere = true;
    public float sphereDuration = 0.2f;
    public bool debugLog = true;

    private Vector3 AttackOrigin
    {
        get { return attackPoint != null ? attackPoint.position : transform.position; }
    }

    private void Reset()
    {
        enemyLayers = LayerMask.GetMask("Enemy");
    }

    private void Awake()
    {
        if (enemyLayers.value == 0)
            enemyLayers = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        // Left mouse button (Fire1)
        if (Input.GetButtonDown("Fire1"))
            Attack();
    }

    private void Attack()
    {
        Vector3 origin = AttackOrigin;

        Collider[] hitColliders =
            Physics.OverlapSphere(origin, attackRange, enemyLayers);

        if (showAttackSphere)
            ShowAttackSphere(origin);

        if (hitColliders.Length == 0)
        {
            if (debugLog)
                ExplainMiss(origin);

            return;
        }

        HashSet<EnemyStatus> alreadyHit = new HashSet<EnemyStatus>();

        foreach (Collider hit in hitColliders)
        {
            EnemyStatus status = FindStatus(hit);

            if (status == null)
            {
                Debug.LogWarning(hit.name + " is on an enemy layer but has no " +
                    "EnemyStatus component.", hit);
                continue;
            }

            if (!alreadyHit.Add(status))
                continue;

            if (debugLog)
                Debug.Log("Hit: " + hit.name + " for " + attackDamage + " damage.");

            status.TakeDamage(attackDamage);
        }
    }

    private EnemyStatus FindStatus(Collider hit)
    {
        EnemyStatus status = hit.GetComponentInParent<EnemyStatus>();

        if (status == null)
            status = hit.GetComponentInChildren<EnemyStatus>();

        return status;
    }

    private void ExplainMiss(Vector3 origin)
    {
        bool enemyInRange = false;

        foreach (Collider c in Physics.OverlapSphere(origin, attackRange))
        {
            if (FindStatus(c) == null)
                continue;

            enemyInRange = true;

            Debug.LogWarning("'" + c.name + "' is in range, but it is on layer '" +
                LayerMask.LayerToName(c.gameObject.layer) + "', which is not " +
                "included in Enemy Layers on PlayerCombat. Add that layer to " +
                "Enemy Layers.", c);
        }

        if (!enemyInRange)
        {
            Debug.Log("Attack hit nothing: no enemy with EnemyStatus within " +
                attackRange + " units of " + origin + ".");
        }
    }

    // Spawns a see-through red sphere the size of the attack area.
    private void ShowAttackSphere(Vector3 center)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Attack Visual";

        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
            Destroy(sphereCollider);

        sphere.transform.position = center;
        sphere.transform.localScale = Vector3.one * attackRange * 2f;

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        sphereRenderer.material = CreateRedMaterial();
        sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
        sphereRenderer.receiveShadows = false;

        Destroy(sphere, sphereDuration);
    }

    private Material CreateRedMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        Color red = new Color(1f, 0f, 0f, 0.35f);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", red);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", red);

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        return mat;
    }

    // Shows the attack area in the Scene view when the player is selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackOrigin, attackRange);
    }
}