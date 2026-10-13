using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public Transform player; // Target yang akan dikejar
    public float moveSpeed = 3f; // Kecepatan gerak musuh
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Agar tidak menggelinding

        CariPlayer();
    }

    void FixedUpdate()
    {
        // Jika player belum ditemukan (misal karena telat spawn), coba cari terus sampai dapat
        if (player == null)
        {
            CariPlayer();
            return; // Hentikan gerakan sampai player benar-benar ada
        }

        // 1. Menyamakan sumbu Y target dengan posisi musuh (Kunci Anti-Melayang)
        // Ini memastikan musuh tidak mencoba terbang ke atas atau menembus lantai
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);

        // 2. Menghitung arah HANYA pada sumbu X dan Z
        Vector3 direction = (targetPosition - transform.position).normalized;

        // 3. Bergerak menggunakan dorongan fisika perlahan
        Vector3 movePosition = transform.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(movePosition);

        // 4. Mengubah arah hadap musuh agar terus melihat pemain
        transform.LookAt(targetPosition);
    }

    // Fungsi khusus untuk mencari player dengan aman tanpa memicu error
    void CariPlayer()
    {
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
}