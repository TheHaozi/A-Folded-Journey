using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("漂浮物理")]
    public float waterDrag = 0.5f;        // 水的阻力
    public float waterAngularDrag = 0.5f; // 水的角阻力
    public float buoyancy = 0.2f;         // 浮力
    
    private Rigidbody2D rb;
    private Vector2 originalDrag;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalDrag = new Vector2(rb.drag, rb.angularDrag);
        
        // 设置水的物理属性
        rb.drag = waterDrag;
        rb.angularDrag = waterAngularDrag;
        
        // 添加轻微的随机旋转，更像漂浮物
        rb.AddTorque(Random.Range(-10f, 10f));
    }
    
    void FixedUpdate()
    {
        // 模拟浮力 - 轻微的向上力
        rb.AddForce(Vector2.up * buoyancy);
        
        // 限制最大速度，避免无限加速
        if (rb.velocity.magnitude > 10f)
        {
            rb.velocity = rb.velocity.normalized * 10f;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 碰撞时添加随机旋转，增加物理感
        rb.AddTorque(Random.Range(-5f, 5f));
    }
}