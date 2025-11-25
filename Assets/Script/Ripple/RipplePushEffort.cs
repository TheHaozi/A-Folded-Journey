using UnityEngine;
using TMPro;
public class RipplePushEffect : MonoBehaviour
{
    [Header("推动设置")]
    public float pushForce = 18.5f;
    public float pushDuration = 8;
    public float maxPushDistance = 20f;
    
    [Header("碰撞检测")]
    public LayerMask obstacleLayers = -1; // 障碍物层
    public float obstacleCheckRadius = 1f; // 障碍物检测半径
    
    [Header("船只旋转设置")]
    public float maxTiltAngle = 25f;          // 最大倾斜角度
    public float tiltSmoothness = 2f;         // 倾斜平滑度
    public float rotationInertia = 0.8f;      // 旋转惯性（0-1）
    public float naturalSway = 2f;            // 自然晃动幅度
    
    [Header("反弹设置")]
    public float bounceForceMultiplier = 0.8f; // 反弹力乘数
    public float minBounceVelocity = 2f;      // 最小反弹速度
    public bool enableBounce = true;          // 是否启用反弹

    [Header("调试/彩蛋设置")]
    public bool enableDebugControls = true;    // 是否启用调试控制
    private bool isKeyboardControlMode = false; // 是否处于键盘控制模式
    
    [Header("传统移动设置")]
    public float moveSpeed = 8f;               // 移动速度
    public float rotationSpeed = 5f;           // 旋转速度
    public float acceleration = 12f;           // 加速
    public float deceleration = 15f;           // 减速

    [Header("彩蛋字幕设置")]
    public bool enableSubtitles = true;           // 是否启用字幕
    public float subtitleDuration = 3f;           // 字幕显示时长
    public TextMeshProUGUI subtitleText;          // TMP字幕文本组件

    //================================================================

    private Vector2 currentMoveVelocity;       // 当前移动速度
    private Vector2 moveInput;                 // 移动输入
    
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

    //=====================================================

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
        
        // 确保字幕文本引用正确
        if (subtitleText == null)
        {
            // 尝试自动查找
            subtitleText = FindObjectOfType<TextMeshProUGUI>();
            if (subtitleText != null)
            {
                Debug.Log("自动找到TMP文本组件");
            }
        }
    }//Start()
    
    void Update()
{
    // 总是检查快捷键，但根据 enableDebugControls 决定行为
    if (CheckDebugShortcut())
    {
        // 如果返回 true 才切换模式（仅在 enableDebugControls 为 true 时）
        if (enableDebugControls)
        {
            ToggleControlMode();
        }
        // 如果 enableDebugControls 为 false，CheckDebugShortcut 内部已经显示了提示
    }
    
    if (isKeyboardControlMode)
    {
        HandleTraditionalInput();
    }
    else
    {
        // 原有的鼠标点击逻辑
        if (Input.GetMouseButtonDown(0))
        {
            CreateRipplePush();
        }
        
        // 原有的物理逻辑
        if (isBeingPushed)
        {
            HandlePushMovement();
        }
        
        if (isBouncing)
        {
            HandleBounceMovement();
        }
        
        HandleBoatRotation();
    }
}
    
    void FixedUpdate()
    {
        if (isKeyboardControlMode)
        {
            HandleTraditionalMovement();
        }
    }//FixedUpdate
    
    void CreateRipplePush()
    {
        Vector2 clickWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPos = transform.position;
        
        float distance = Vector2.Distance(clickWorldPos, playerPos);
        
        // 检查是否在作用范围内
        if (distance > maxPushDistance) return;
        
        // 检查点击位置是否有碰撞箱（障碍物）
        Collider2D obstacle = Physics2D.OverlapCircle(clickWorldPos, obstacleCheckRadius, obstacleLayers);
        if (obstacle != null)
        {
            Debug.Log($"🚫 无法推动：点击位置有障碍物 {obstacle.gameObject.name}");
            return;
        }
        
        // 原有的玩家推动逻辑...
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
        
        PushNearbyFloatingLogs(clickWorldPos, distanceFactor);
    }//CreateRipplePush()
    
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
    }//HandlePushMovement()
    
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
    }//HandleBounceMovement()
    
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
    }//HandleBoatRotation()
    
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
    }//OnCollisionEnter2D()

    void PushNearbyFloatingLogs(Vector2 pushOrigin, float distanceFactor)
    {
        // 检测推动范围内的所有浮木
        float pushRadius = maxPushDistance * 0.8f; // 推动半径（比玩家推动范围稍大）
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(pushOrigin, pushRadius);
        
        foreach (Collider2D collider in nearbyColliders)
        {
            FloatingLog floatingLog = collider.GetComponent<FloatingLog>();
            if (floatingLog != null)
            {
                // 计算浮木相对于推动中心的方向
                Vector2 directionToLog = ((Vector2)floatingLog.transform.position - pushOrigin).normalized;
                
                // 计算浮木与推动中心的距离
                float logDistance = Vector2.Distance(pushOrigin, floatingLog.transform.position);
                float logDistanceFactor = 1f - (logDistance / pushRadius);
                
                // 计算推动力（基于距离和原始推动力）
                float logPushForce = currentForce * logDistanceFactor * 0.6f; // 浮木受力比玩家小
                
                // 应用推动力
                floatingLog.ApplyPushForce(directionToLog, logPushForce);
                
                Debug.Log($"推动浮木，力度: {logPushForce}");
            }
        }
    }//PushNearbyFloatingLogs()
    
    // ========== 传统移动模式方法 ==========
bool CheckDebugShortcut()
{
    // 检测快捷键是否被按下
    bool shortcutPressed = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                          (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                          Input.GetKey(KeyCode.J) &&
                          Input.GetKeyDown(KeyCode.C);
    
    if (shortcutPressed)
    {
        if (!enableDebugControls)
        {
            ShowSubtitles("You are not arrow to use WASD in this level.");
        }
        return true; // 总是返回 true 表示检测到了快捷键
    }
    
    return false;
}
    
    void ToggleControlMode()
    {
        isKeyboardControlMode = !isKeyboardControlMode;

        if (!enableDebugControls)
        {
            ShowSubtitles("You are not arrow to use WASD in this level.");
            return; // 直接返回，不执行模式切换
        }
    
        
        // 切换模式时完全重置所有物理状态
        if (isKeyboardControlMode)
        {
            // 彻底清除所有推动和反弹状态
            isBeingPushed = false;
            isBouncing = false;
            currentVelocity = Vector2.zero;
            bounceVelocity = Vector2.zero;
            currentMoveVelocity = Vector2.zero;
            moveInput = Vector2.zero;
            
            // 重置旋转相关状态
            rotationVelocity = 0f;
            targetRotation = 0f;
            currentRotation = 0f;
            
            // 设置刚体属性为传统移动模式
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.drag = 0f;           // 传统模式不需要阻尼
            rb.angularDrag = 0f;
            
            // ========== 新增彩蛋字幕 ==========
            Debug.Log($"🎮 控制模式切换: 传统WSAD移动模式");
            ShowSubtitles("You cans using WSAD to move,but it's makes me off-topic!Press the same shortcut to switch back!");
        }
        else
        {
            // 切换回物理模式时恢复刚体设置
            rb.drag = 0.5f;
            rb.angularDrag = 2f;
            
            Debug.Log($"🎮 控制模式切换: 物理点击推动模式");
            ShowSubtitles("Alright,let's act like nothing happened and keep playing.");
        }
    }//ToggleControlMode()
    
    void HandleTraditionalInput()
    {
        // 获取WSAD输入
        moveInput = Vector2.zero;
        
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
        
        // 标准化输入
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
        
        // 处理旋转 - 基于移动方向
        if (moveInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + 90f;
            currentRotation = Mathf.LerpAngle(currentRotation, targetAngle, rotationSpeed * Time.deltaTime);
        }
        
        // 应用旋转
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }//HandleTraditionalInput()
    
    void HandleTraditionalMovement()
    {
        if (moveInput != Vector2.zero)
        {
            // 加速
            currentMoveVelocity = Vector2.Lerp(
                currentMoveVelocity, 
                moveInput * moveSpeed, 
                acceleration * Time.fixedDeltaTime
            );
        }
        else
        {
            // 减速
            currentMoveVelocity = Vector2.Lerp(
                currentMoveVelocity, 
                Vector2.zero, 
                deceleration * Time.fixedDeltaTime
            );
        }
        
        // 应用移动
        Vector2 newPosition = rb.position + currentMoveVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }//HandleTraditionalMovement()
   void ShowSubtitles(string message)
    {
        if (!enableSubtitles) return;
        
        // 在控制台显示
        Debug.Log($"🎯 {message}");
        
        // 使用TMP在屏幕上显示
        if (subtitleText != null)
        {
            subtitleText.text = message;
            subtitleText.color = Color.yellow;
            subtitleText.fontStyle = FontStyles.Bold;
            
            // 取消之前的协程（如果有）并开始新的
            StopAllCoroutines();
            StartCoroutine(HideSubtitleAfterDelay());
        }
        else
        {
            Debug.LogWarning("字幕TMP组件未分配！请在Inspector中分配或创建TMP文本对象。");
        }
    }//ShowSubtitles()

    System.Collections.IEnumerator HideSubtitleAfterDelay()
    {
        yield return new WaitForSeconds(subtitleDuration);
        
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }
    }//HideSubtitleAfterDelay()

    // 在 RipplePushEffect 类中添加：
    public void PlayerDieByShark()
    {
        // 禁用玩家控制
        enabled = false;
        
        // 停止移动
        if (GetComponent<Rigidbody2D>() != null)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        // 可以添加其他死亡效果，如粒子效果等
        Debug.Log("💀 玩家被鲨鱼杀死");
    }//PlayerDieByShark()

    System.Collections.IEnumerator ReloadSceneWithDelay()
    {
    // 等待一帧让音效播放完成
    yield return new WaitForSeconds(1f);
    UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }//ReloadSceneWithDelay()
    
    public void ResetPlayer()
    {
        // 重新启用玩家控制
        enabled = true;
        
        // 重置玩家位置到起点
        transform.position = Vector3.zero;
        
        // 重置其他状态
        // ...
    }
}//class RipplePushEffect