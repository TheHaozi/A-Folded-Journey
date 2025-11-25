using UnityEngine;

public class FloatingLog : MonoBehaviour
{
    [Header("基础设置")]
    public float baseSpeed = 1f;
    public float speedVariation = 0.5f;
    
    [Header("大小设置")]
    public float baseScale = 0.3f; // 基础大小
    public float minScale = 0.8f;  // 最小缩放倍数
    public float maxScale = 1.2f;  // 最大缩放倍数
    
    [Header("自然漂浮效果")]
    public float waveFrequency = 0.8f;
    public float waveAmplitude = 0.15f;
    public float horizontalSway = 0.1f;
    
    [Header("旋转效果")]
    public float rotationSway = 8f;
    public float rotationSmoothness = 3f;

    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    
    // 运动变量
    private Vector2 currentVelocity;
    private float currentSpeed;
    private Vector2 currentDirection;
    
    // 自然运动变量
    private float randomOffset;
    private Vector3 startPosition;
    private float wavePhase;
    private float swayPhase;
    private float rotationPhase;
    
    // 旋转相关
    private float targetRotation;
    private float currentRotation;
    private float rotationVelocity;

    void Start()
    {
        InitializeLog();
    }

    void InitializeLog()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        randomOffset = Random.Range(0f, 360f);
        startPosition = transform.position;
        
        wavePhase = Random.Range(0f, Mathf.PI * 2);
        swayPhase = Random.Range(0f, Mathf.PI * 2);
        rotationPhase = Random.Range(0f, Mathf.PI * 2);
        
        currentRotation = transform.rotation.eulerAngles.z;
        targetRotation = currentRotation;
        
        // 应用随机大小
        ApplyRandomScale();
    }

    void ApplyRandomScale()
    {
        // 在基础大小的基础上随机微调
        float randomScaleMultiplier = Random.Range(minScale, maxScale);
        float finalScale = baseScale * randomScaleMultiplier;
        transform.localScale = Vector3.one * finalScale;
        
        // 更新碰撞体大小
        UpdateColliderSize();
    }

    void UpdateColliderSize()
    {
        if (boxCollider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            boxCollider.size = spriteBounds.size;
            boxCollider.offset = spriteBounds.center;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleNaturalFloating();
        HandleSmoothRotation();
    }

    void HandleMovement()
    {
        Vector3 movement = (Vector3)currentVelocity * Time.deltaTime;
        transform.position += movement;
        
        currentVelocity *= 0.998f;
        currentSpeed = currentVelocity.magnitude;
    }

    void HandleNaturalFloating()
    {
        float time = Time.time + randomOffset;
        
        float verticalWave = Mathf.Sin(time * waveFrequency + wavePhase) * waveAmplitude;
        float horizontalWave = Mathf.Sin(time * waveFrequency * 0.7f + swayPhase) * horizontalSway;
        
        transform.position += new Vector3(horizontalWave, verticalWave, 0) * Time.deltaTime;
        
        if (currentVelocity.magnitude > 0.1f)
        {
            float baseRotation = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg + 90f;
            float naturalRotation = Mathf.Sin(time * 0.5f + rotationPhase) * rotationSway;
            targetRotation = baseRotation + naturalRotation;
        }
    }

    void HandleSmoothRotation()
    {
        currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, 1f / rotationSmoothness);
        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    public void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
            UpdateColliderSize(); // 更换贴图时更新碰撞体
        }
    }

    public void SetMoveDirection(Vector2 direction)
    {
        currentDirection = direction.normalized;
        currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        currentVelocity = currentDirection * currentSpeed;
    }

    public void ApplyPushForce(Vector2 pushDirection, float pushForce)
    {
        currentVelocity = pushDirection * pushForce;
        currentDirection = currentVelocity.normalized;
        currentSpeed = currentVelocity.magnitude;
        rotationPhase = Random.Range(0f, Mathf.PI * 2);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            currentVelocity = Vector2.Reflect(currentVelocity, normal) * 0.9f;
            currentDirection = currentVelocity.normalized;
            currentSpeed = currentVelocity.magnitude;
        }
    }
}