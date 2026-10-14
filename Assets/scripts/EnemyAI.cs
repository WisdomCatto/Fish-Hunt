using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Pengaturan Musuh")]
    public float moveSpeed = 4f;
    public Transform player; // Boleh dibiarkan kosong di Inspector

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Memaksa mencari objek "Player" di detik pertama
        FindTarget();
    }

    void FindTarget()
    {
        GameObject targetObj = GameObject.Find("Player");
        if (targetObj != null)
        {
            player = targetObj.transform;
        }
        else
        {
            Debug.LogError("MUSUH GAGAL MENEMUKAN PLAYER! Pastikan nama karakter dieja 'Player'.");
        }
    }

    void FixedUpdate()
    {
        // Jika target masih kosong, cari terus menerus
        if (player == null)
        {
            FindTarget();
            return;
        }

        // --- INTI PERGERAKAN ---

        // 1. Kunci posisi Y target agar sejajar dengan posisi musuh (Anti melayang/tenggelam)
        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);

        // 2. Dapatkan arah menuju target
        Vector3 direction = (targetPos - transform.position).normalized;

        // 3. Pindahkan tubuh musuh secara paksa menggunakan dorongan fisika
        Vector3 nextPosition = transform.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);

        // 4. Ubah arah wajah musuh
        transform.LookAt(targetPos);
    }
}