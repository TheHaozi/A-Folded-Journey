using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    
    [Header("跟随设置")]
    public float smoothSpeed = 0.1f;
    public Vector3 offset = new Vector3(0, 0, -10);
    public float maxCameraSpeed = 5f;
    
    [Header("边界设置")]
    public bool useBounds = true;
    public Rect cameraBounds = new Rect(-10, -10, 20, 20);
    
    private Vector3 currentVelocity;
    private Rigidbody2D targetRb;
    private Camera cam;
    
    void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
        cam = GetComponent<Camera>();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.position + offset;
        
        // 如果目标有物理运动，预测位置
        if (targetRb != null && targetRb.velocity.magnitude > 0.1f)
        {
            // 轻微的位置预测，让相机更平滑
            targetPosition += (Vector3)targetRb.velocity * 0.1f;
        }
        
        // 应用边界限制
        if (useBounds && cam != null)
        {
            targetPosition = GetBoundedPosition(targetPosition);
        }
        
        // 限制最大相机速度
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);
        
        // 如果使用边界，确保平滑后的位置也在边界内
        if (useBounds && cam != null)
        {
            smoothedPosition = GetBoundedPosition(smoothedPosition);
        }
        
        // 限制相机移动速度，避免快速晃动
        if (Vector3.Distance(transform.position, smoothedPosition) > maxCameraSpeed * Time.deltaTime)
        {
            smoothedPosition = transform.position + (smoothedPosition - transform.position).normalized * maxCameraSpeed * Time.deltaTime;
        }
        
        transform.position = smoothedPosition;
    }
    
    /// <summary>
    /// 获取在边界内的相机位置
    /// </summary>
    private Vector3 GetBoundedPosition(Vector3 desiredPosition)
    {
        if (cam == null) return desiredPosition;
        
        // 计算相机视口大小
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;
        
        // 计算相机在边界内的限制位置
        float minX = cameraBounds.xMin + width / 2f;
        float maxX = cameraBounds.xMax - width / 2f;
        float minY = cameraBounds.yMin + height / 2f;
        float maxY = cameraBounds.yMax - height / 2f;
        
        // 限制位置在边界内
        float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(desiredPosition.y, minY, maxY);
        
        return new Vector3(clampedX, clampedY, desiredPosition.z);
    }
    
    /// <summary>
    /// 在Scene视图中绘制边界框
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(cameraBounds.center.x, cameraBounds.center.y, 0);
        Vector3 size = new Vector3(cameraBounds.width, cameraBounds.height, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
    
    /// <summary>
    /// 设置新的边界
    /// </summary>
    public void SetBounds(Rect newBounds)
    {
        cameraBounds = newBounds;
    }
    
    /// <summary>
    /// 启用/禁用边界限制
    /// </summary>
    public void SetBoundsEnabled(bool enabled)
    {
        useBounds = enabled;
    }
}