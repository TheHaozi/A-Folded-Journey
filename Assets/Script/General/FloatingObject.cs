using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 完全复制 RipplePushEffect 的设置
            rb.gravityScale = 0f;
            rb.drag = 0.5f;           // RipplePushEffect 的阻力
            rb.angularDrag = 2f;      // RipplePushEffect 的角阻力
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null || collision.contacts.Length == 0) return;
        
        // 简单直接反弹
        Vector2 normal = collision.contacts[0].normal;
        Vector2 currentVel = rb.velocity;
        
        Vector2 bounceVel = Vector2.Reflect(currentVel.normalized, normal) * currentVel.magnitude;
        rb.velocity = bounceVel;
        
        Debug.Log($"Bounce! Speed: {bounceVel.magnitude}");
    }
}