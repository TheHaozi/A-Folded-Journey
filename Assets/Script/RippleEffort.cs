using UnityEngine;

public class RipplePushEffect : MonoBehaviour
{
    [Header("推动设置")]
    public float pushForce = 18.5f;           // 推力大小
    public float pushDuration = 8;            // 推力持续时间
    public float maxPushDistance = 20f;       // 最大作用距离
    
    [Header("船只旋转设置")]
    public float maxTiltAngle = 25f;          // 最大倾斜角度
    public float tiltSmoothness = 2f;         // 倾斜平滑度
    public float rotationInertia = 0.8f;      // 旋转惯性（0-1）
    public float naturalSway = 2f;            // 自然晃动幅度
    
    [Header("反弹设置")]
    public float bounceForceMultiplier = 0.8f; // 反弹力乘数
    public float minBounceVelocity = 2f;      // 最小反弹速度
    public bool enableBounce = true;          // 是否启用反弹
    
    private Camera mainCamera;
    private bool isBeingPushed = false;
    private float pushTimer = 0f;
    private Vector2 pushDirection;
    private float currentForce;
    
    private float currentRotation;            // 当前旋转角度
    private float targetRotation;             // 目标旋转角度
    private float rotationVelocity;           // 旋转速度（用于平滑）
    
    // 反弹相关变量
    private Vector2 bounceVelocity;           // 反弹速度
    private bool isBouncing = false;          // 是否正在反弹
    private float bounceDecay = 0.95f;        // 反弹衰减
    
    // 移动相关变量
    private Vector2 currentVelocity;          // 当前速度
    private Rigidbody2D rb;                   // 物理组件

    void Start()
    {
        mainCamera = Camera.main;
        
        // 获取或添加 Rigidbody2D 组件
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 设置物理属性
        rb.gravityScale = 0f;                 // 无重力
        rb.drag = 0.5f;                       // 线性阻尼
        rb.angularDrag = 2f;                  // 角阻尼
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 连续碰撞检测
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreateRipplePush();
        }
        
        // 处理推动移动
        if (isBeingPushed)
        {
            HandlePushMovement();
        }
        
        // 处理反弹移动
        if (isBouncing)
        {
            HandleBounceMovement();
        }
        
        // 持续处理旋转
        HandleBoatRotation();
    }
    
    void CreateRipplePush()
    {
        Vector2 clickWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPos = transform.position;
        
        float distance = Vector2.Distance(clickWorldPos, playerPos);
        
        // 检查是否在作用范围内
        if (distance > maxPushDistance) return;
        
        // 停止任何现有的反弹
        isBouncing = false;
        bounceVelocity = Vector2.zero;
        
        // 计算推力方向（从点击点指向玩家）
        pushDirection = (playerPos - clickWorldPos).normalized;
        
        // 根据距离计算推力大小（越近推力越大）
        float distanceFactor = 1f - (distance / maxPushDistance);
        currentForce = pushForce * distanceFactor;
        
        // 计算目标旋转角度（基于推力方向）
        float pushAngle = Mathf.Atan2(pushDirection.y, pushDirection.x) * Mathf.Rad2Deg;
        targetRotation = pushAngle + 90f;
        
        // 根据距离调整倾斜幅度
        float tiltMultiplier = distanceFactor * rotationInertia;
        targetRotation = Mathf.LerpAngle(currentRotation, targetRotation, tiltMultiplier);
        
        // 限制最大倾斜角度
        float rotationDelta = Mathf.DeltaAngle(currentRotation, targetRotation);
        rotationDelta = Mathf.Clamp(rotationDelta, -maxTiltAngle, maxTiltAngle);
        targetRotation = currentRotation + rotationDelta;
        
        // 开始推动
        isBeingPushed = true;
        pushTimer = 0f;
        
        // 设置初始速度
        currentVelocity = pushDirection * currentForce;
    }
    
    void HandlePushMovement()
    {
        pushTimer += Time.deltaTime;
        
        if (pushTimer >= pushDuration)
        {
            isBeingPushed = false;
            // 推动结束后，如果有速度，开始自然移动
            if (currentVelocity.magnitude > minBounceVelocity)
            {
                isBouncing = true;
            }
            return;
        }
        
        // 计算当前帧的推力（随时间衰减）
        float progress = pushTimer / pushDuration;
        float forceThisFrame = currentForce * (1f - progress) * Time.deltaTime;
        
        // 更新速度
        currentVelocity = pushDirection * (currentForce * (1f - progress));
        
        // 应用移动
        Vector3 movement = (Vector3)pushDirection * forceThisFrame;
        transform.position += movement;
    }
    
    void HandleBounceMovement()
    {
        if (bounceVelocity.magnitude < minBounceVelocity)
        {
            isBouncing = false;
            bounceVelocity = Vector2.zero;
            return;
        }
        
        // 应用反弹移动
        Vector3 movement = (Vector3)bounceVelocity * Time.deltaTime;
        transform.position += movement;
        
        // 衰减速度
        bounceVelocity *= bounceDecay;
        
        // 更新旋转以匹配移动方向
        if (bounceVelocity.magnitude > 0.1f)
        {
            float bounceAngle = Mathf.Atan2(bounceVelocity.y, bounceVelocity.x) * Mathf.Rad2Deg;
            targetRotation = bounceAngle + 90f;
        }
    }
    
    void HandleBoatRotation()
    {
        // 添加自然的水面晃动
        float naturalSwayRotation = Mathf.Sin(Time.time * 0.8f) * naturalSway;
        
        // 平滑旋转到目标角度
        currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation + naturalSwayRotation, 
            ref rotationVelocity, 1f / tiltSmoothness);
        
        // 应用旋转
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);
        
        // 如果没有被推动且没有反弹，慢慢减小目标角度
        if (!isBeingPushed && !isBouncing && pushTimer > pushDuration * 0.5f)
        {
            targetRotation = Mathf.LerpAngle(targetRotation, naturalSwayRotation, Time.deltaTime * 0.3f);
        }
    }
    
    // 碰撞检测 - 添加反弹效果
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableBounce) return;
        
        // 安全检查
        if (collision == null || collision.contacts.Length == 0) return;
        
        // 获取碰撞法线
        Vector2 normal = collision.contacts[0].normal;
        
        // 计算入射速度（使用当前速度或反弹速度）
        Vector2 incomingVelocity = isBouncing ? bounceVelocity : currentVelocity;
        
        // 如果速度太小，不反弹
        if (incomingVelocity.magnitude < minBounceVelocity) return;
        
        // 计算反射方向
        Vector2 reflection = Vector2.Reflect(incomingVelocity.normalized, normal);
        
        // 计算反弹速度（带衰减）
        float incomingSpeed = incomingVelocity.magnitude;
        bounceVelocity = reflection * (incomingSpeed * bounceForceMultiplier);
        
        // 确保反弹速度不低于最小值
        if (bounceVelocity.magnitude < minBounceVelocity)
        {
            bounceVelocity = bounceVelocity.normalized * minBounceVelocity;
        }
        
        // 设置状态
        isBeingPushed = false;
        isBouncing = true;
        
        // 根据反弹方向更新目标旋转
        float bounceAngle = Mathf.Atan2(bounceVelocity.y, bounceVelocity.x) * Mathf.Rad2Deg;
        targetRotation = bounceAngle + 90f;
        
        Debug.Log($"Bounce! Incoming: {incomingVelocity.magnitude}, Bounce: {bounceVelocity.magnitude}");
    }
}