using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 movement;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Mengunci rotasi agar karakter tidak menggelinding saat menabrak dinding
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // Mengambil input (W, A, S, D)
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.z = Input.GetAxisRaw("Vertical"); // Sumbu Z untuk maju/mundur di 3D
        movement = movement.normalized;
    }

    void FixedUpdate()
    {
        // Menggerakkan karakter di ruang 3D
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}