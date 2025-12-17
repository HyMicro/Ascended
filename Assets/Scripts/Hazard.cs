using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 15f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Hitung arah terpental (menjauh dari duri)
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                
                // Tambahkan sedikit bias ke atas biar pentalannya kelihatan
                knockbackDir += Vector2.up * 0.5f;

                // Reset velocity dulu biar pentalannya konsisten
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                
                Debug.Log("Ouch! Knockback!");
            }
        }
    }
}