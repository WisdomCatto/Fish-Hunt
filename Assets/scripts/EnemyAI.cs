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

        // Otomatis mencari objek bernama "Player" jika kolom target kosong
        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            // Menghitung arah menuju pemain
            Vector3 direction = (player.position - transform.position).normalized;

            // Bergerak perlahan ke arah pemain
            Vector3 movePosition = transform.position + direction * moveSpeed * Time.fixedDeltaTime;
            movePosition.y = transform.position.y; // Mengunci sumbu Y agar tidak melayang

            rb.MovePosition(movePosition);

            // Mengubah arah hadap musuh agar selalu melihat pemain
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }
    }
}