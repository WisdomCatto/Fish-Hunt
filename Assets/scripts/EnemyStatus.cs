using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Fungsi ini akan dipanggil oleh senjata/pedang pemain
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " terkena " + damage + " damage! Sisa HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " mati dikalahkan!");
        // Menghilangkan objek musuh dari arena
        Destroy(gameObject);
    }
}