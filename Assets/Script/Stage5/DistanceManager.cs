using UnityEngine;

public class DistanceManager : MonoBehaviour
{
    [Header("参考点")]
    public Transform centerPoint;
    
    [Header("玩家")]
    public RipplePushEffect playerController;
    
    // 公共属性，其他脚本可以读取
    public float CurrentDistance { get; private set; }
    public Vector3 PlayerPosition { get; private set; }
    public Vector2 PlayerMoveDirection { get; private set; }
    public bool IsPlayerMoving { get; private set; }
    
    public static DistanceManager Instance { get; private set; }
    
    private Vector3 lastPlayerPosition;
    private Vector2 smoothedMoveDirection;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (centerPoint == null)
        {
            GameObject centerObj = new GameObject("CenterPoint");
            centerPoint = centerObj.transform;
            centerPoint.position = Vector3.zero;
        }
        
        if (playerController == null)
            playerController = FindObjectOfType<RipplePushEffect>();
            
        lastPlayerPosition = playerController != null ? playerController.transform.position : Vector3.zero;
    }
    
    void Update()
    {
        if (playerController == null) return;
        
        PlayerPosition = playerController.transform.position;
        CurrentDistance = Vector3.Distance(PlayerPosition, centerPoint.position);
        
        // 计算玩家移动方向和状态
        UpdatePlayerMovement();
    }
    
    void UpdatePlayerMovement()
    {
        Vector3 currentPosition = playerController.transform.position;
        
        // 计算移动方向
        if (Vector3.Distance(currentPosition, lastPlayerPosition) > 0.01f)
        {
            Vector2 rawDirection = (currentPosition - lastPlayerPosition).normalized;
            IsPlayerMoving = true;
            
            // 平滑移动方向（避免抖动）
            smoothedMoveDirection = Vector2.Lerp(smoothedMoveDirection, rawDirection, 5f * Time.deltaTime);
            PlayerMoveDirection = smoothedMoveDirection.normalized;
        }
        else
        {
            IsPlayerMoving = false;
            // 保持最后的方向，但不更新平滑方向
        }
        
        lastPlayerPosition = currentPosition;
    }
    
    // 获取玩家前方的区块坐标
    public Vector2Int GetForwardChunkPosition(float lookAheadDistance = 50f)
    {
        if (!IsPlayerMoving || PlayerMoveDirection == Vector2.zero)
        {
            // 如果玩家静止，返回当前区块
            return GetChunkPosition(PlayerPosition);
        }
        
        // 计算前方位置
        Vector3 forwardPosition = PlayerPosition + (Vector3)PlayerMoveDirection * lookAheadDistance;
        return GetChunkPosition(forwardPosition);
    }
    
    // 获取位置对应的区块坐标
    public Vector2Int GetChunkPosition(Vector3 worldPosition)
    {
        // 这里需要获取OceanTerrainManager的chunkSize
        OceanTerrainManager terrainManager = FindObjectOfType<OceanTerrainManager>();
        int chunkSize = terrainManager != null ? terrainManager.chunkSize : 2000;
        
        int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int chunkY = Mathf.FloorToInt(worldPosition.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // 调试显示
    void OnDrawGizmosSelected()
    {
        if (playerController == null) return;
        
        // 绘制玩家移动方向
        if (IsPlayerMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerController.transform.position, (Vector3)PlayerMoveDirection * 30f);
            
            // 绘制前方区块位置
            Vector2Int forwardChunk = GetForwardChunkPosition();
            OceanTerrainManager terrainManager = FindObjectOfType<OceanTerrainManager>();
            if (terrainManager != null)
            {
                Vector3 chunkCenter = new Vector3(
                    forwardChunk.x * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f,
                    forwardChunk.y * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f,
                    0f
                );
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(chunkCenter, new Vector3(terrainManager.chunkSize, terrainManager.chunkSize, 0));
                Gizmos.DrawLine(playerController.transform.position, chunkCenter);
            }
        }
    }
}