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
    
    private Camera mainCamera;
    private bool isBeingPushed = false;
    private float pushTimer = 0f;
    private Vector2 pushDirection;
    private float currentForce;
    
    private float currentRotation;            // 当前旋转角度
    private float targetRotation;             // 目标旋转角度
    private float rotationVelocity;           // 旋转速度（用于平滑）
    
    void Start()
    {
        mainCamera = Camera.main;
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
        
        // 计算推力方向（从点击点指向玩家）
        pushDirection = (playerPos - clickWorldPos).normalized;
        
        // 根据距离计算推力大小（越近推力越大）
        float distanceFactor = 1f - (distance / maxPushDistance);
        currentForce = pushForce * distanceFactor;
        
        // 计算目标旋转角度（基于推力方向）
        // 使用推力方向的垂直向量来决定倾斜方向
        float pushAngle = Mathf.Atan2(pushDirection.y, pushDirection.x) * Mathf.Rad2Deg;
        
        // 目标角度是推力方向的垂直方向（船身与推力方向垂直）
        targetRotation = pushAngle + 90f; // 或者 -90f，取决于船的朝向
        
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
    }
    
    void HandlePushMovement()
    {
        pushTimer += Time.deltaTime;
        
        if (pushTimer >= pushDuration)
        {
            isBeingPushed = false;
            return;
        }
        
        // 计算当前帧的推力（随时间衰减）
        float progress = pushTimer / pushDuration;
        float forceThisFrame = currentForce * (1f - progress) * Time.deltaTime;
        
        // 应用移动
        Vector3 movement = (Vector3)pushDirection * forceThisFrame;
        transform.position += movement;
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
        
        // 如果没有被推动，慢慢减小目标角度（轻微回正，但不完全）
        if (!isBeingPushed && pushTimer > pushDuration * 0.5f)
        {
            targetRotation = Mathf.LerpAngle(targetRotation, naturalSwayRotation, Time.deltaTime * 0.3f);
        }
    }
}