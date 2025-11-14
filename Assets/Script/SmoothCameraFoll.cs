using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    
    [Header("跟随设置")]
    public float smoothSpeed = 0.1f;
    public Vector3 offset = new Vector3(0, 0, -10);
    public float maxCameraSpeed = 5f;
    
    private Vector3 currentVelocity;
    private Rigidbody2D targetRb;
    
    void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
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
        
        // 限制最大相机速度
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);
        
        // 限制相机移动速度，避免快速晃动
        if (Vector3.Distance(transform.position, smoothedPosition) > maxCameraSpeed * Time.deltaTime)
        {
            smoothedPosition = transform.position + (smoothedPosition - transform.position).normalized * maxCameraSpeed * Time.deltaTime;
        }
        
        transform.position = smoothedPosition;
    }
}
