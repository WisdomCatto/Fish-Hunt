using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint; // Titik pusat area serangan
    public float attackRange = 1.5f; // Jarak jangkauan pedang/pukulan
    public int attackDamage = 20; // Besar *damage* yang diberikan
    public LayerMask enemyLayers; // Filter agar hanya mengenai objek ber-layer 'Enemy'

    void Update()
    {
        // Mengeksekusi serangan saat klik kiri mouse ditekan
        if (Input.GetButtonDown("Fire1"))
        {
            Attack();
        }
    }

    void Attack()
    {
        // 1. Mendeteksi musuh dalam jangkauan bola (sphere) 3D
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        // 2. Memberikan *damage* ke musuh yang terdeteksi
        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log("Berhasil memukul: " + enemy.name);

            // Catatan: Baris di bawah ini akan diaktifkan setelah kita membuat skrip Enemy
            enemy.GetComponent<EnemyStatus>().TakeDamage(attackDamage);
        }
    }

    // Fungsi visual agar Anda bisa melihat seberapa besar area serangan di Unity Editor
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}