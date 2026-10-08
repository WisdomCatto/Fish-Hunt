using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;

    // Y positif agar kamera berada di atas karakter, Z negatif agar agak ke belakang
    public Vector3 offset = new Vector3(0, 10f, -10f);
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);

            // Memaksa kamera selalu menunduk menghadap ke arah karakter
            transform.LookAt(target);
        }
    }
}