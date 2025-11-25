using System.Collections.Generic;
using UnityEngine;

public class OceanTerrainManager : MonoBehaviour
{
    [Header("地形设置")]
    public Texture2D oceanTexture;
    public int chunkSize = 2000;
    public int keepAliveDistance = 2;
    
    [Header("玩家设置")]
    public RipplePushEffect playerController;
    
    [Header("统计信息")]
    public bool showStatistics = true;
    
    private Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunkPos;
    
    // 简化的统计变量
    private int totalChunksGenerated = 0;
    private int totalChunksDestroyed = 0;
    private int maxChunksAlive = 0;
    private float gameStartTime;
    
    // 性能优化
    private float updateInterval = 0.1f;
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<RipplePushEffect>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("未找到玩家控制脚本！请将RipplePushEffect脚本分配给Player Controller字段。");
            return;
        }
        
        lastPlayerChunkPos = GetChunkPosition(playerController.transform.position);
        gameStartTime = Time.time;
        
        GenerateSurroundingChunks();
        UpdateMaxChunksCount();
        
        Debug.Log("🌊 海洋地形管理器已启动");
    }
    
    void Update()
    {
        // 性能优化：限制地形更新频率
        if (Time.time - lastUpdateTime < updateInterval) return;
        
        Vector2Int currentPlayerChunkPos = GetChunkPosition(playerController.transform.position);
        
        if (currentPlayerChunkPos != lastPlayerChunkPos)
        {
            GenerateSurroundingChunks();
            CleanupFarChunks();
            lastPlayerChunkPos = currentPlayerChunkPos;
            UpdateMaxChunksCount();
            
            lastUpdateTime = Time.time;
        }
    }
    
    private void GenerateSurroundingChunks()
    {
        Vector2Int playerChunkPos = GetChunkPosition(playerController.transform.position);
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int chunkPos = new Vector2Int(playerChunkPos.x + x, playerChunkPos.y + y);
                
                if (!terrainChunks.ContainsKey(chunkPos))
                {
                    CreateChunk(chunkPos);
                    totalChunksGenerated++;
                }
            }
        }
    }
    
    private void CleanupFarChunks()
    {
        Vector2Int playerChunkPos = GetChunkPosition(playerController.transform.position);
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        
        foreach (var chunkPair in terrainChunks)
        {
            Vector2Int chunkPos = chunkPair.Key;
            int distanceX = Mathf.Abs(chunkPos.x - playerChunkPos.x);
            int distanceY = Mathf.Abs(chunkPos.y - playerChunkPos.y);
            
            if (distanceX > keepAliveDistance || distanceY > keepAliveDistance)
            {
                chunksToRemove.Add(chunkPos);
            }
        }
        
        foreach (Vector2Int chunkPos in chunksToRemove)
        {
            if (terrainChunks.TryGetValue(chunkPos, out GameObject chunk))
            {
                Destroy(chunk);
                terrainChunks.Remove(chunkPos);
                totalChunksDestroyed++;
                Debug.Log($"🗑️ 销毁远离的地形块: {chunkPos}");
            }
        }
    }
    
    private void CreateChunk(Vector2Int chunkPos)
    {
        Vector3 worldPosition = new Vector3(chunkPos.x * chunkSize + chunkSize * 0.5f, 
                                          chunkPos.y * chunkSize + chunkSize * 0.5f, 10f);
        
        GameObject chunk = new GameObject($"OceanChunk_{chunkPos.x}_{chunkPos.y}");
        chunk.transform.position = worldPosition;
        chunk.transform.SetParent(transform);
        
        // 添加SpriteRenderer组件
        SpriteRenderer renderer = chunk.AddComponent<SpriteRenderer>();
        
        if (oceanTexture != null)
        {
            Sprite sprite = Sprite.Create(
                oceanTexture,
                new Rect(0, 0, oceanTexture.width, oceanTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            renderer.sprite = sprite;
            
            // 设置排序层，确保地形在玩家后面
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = -1;
        }
        else
        {
            Debug.LogWarning("海洋贴图未分配！请在Inspector中分配海洋贴图。");
            renderer.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        }
        
        // 添加碰撞体
        BoxCollider2D collider = chunk.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(chunkSize, chunkSize);
        collider.isTrigger = true;
        
        terrainChunks.Add(chunkPos, chunk);
        Debug.Log($"🌊 生成地形块: {chunkPos} 位置: {worldPosition}");
    }
    
    private void UpdateMaxChunksCount()
    {
        if (terrainChunks.Count > maxChunksAlive)
        {
            maxChunksAlive = terrainChunks.Count;
        }
    }
    
    // 获取玩家当前控制模式
    private bool GetControlMode()
    {
        System.Reflection.FieldInfo field = typeof(RipplePushEffect).GetField("isKeyboardControlMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (bool)field.GetValue(playerController);
        }
        
        return false;
    }
    
    // 在屏幕上显示统计信息
    void OnGUI()
    {
        if (showStatistics && playerController != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 12;
            
            GUI.Box(new Rect(10, 10, 300, 180), GetStatistics(), style);
        }
    }
    
    // 调试显示
    void OnDrawGizmosSelected()
    {
        if (playerController == null) return;
        
        Vector2Int playerChunk = GetChunkPosition(playerController.transform.position);
        
        // 绘制所有活跃区块
        Gizmos.color = Color.blue;
        foreach (var chunk in terrainChunks.Values)
        {
            if (chunk != null)
            {
                Gizmos.DrawWireCube(chunk.transform.position, new Vector3(chunkSize, chunkSize, 0));
            }
        }
        
        // 绘制玩家所在区块
        Gizmos.color = Color.red;
        Vector3 playerChunkPos = new Vector3(playerChunk.x * chunkSize + chunkSize * 0.5f, 
                                           playerChunk.y * chunkSize + chunkSize * 0.5f, 0);
        Gizmos.DrawWireCube(playerChunkPos, new Vector3(chunkSize, chunkSize, 0));
        
        // 绘制保持存活的距离范围
        Gizmos.color = Color.yellow;
        for (int x = -keepAliveDistance; x <= keepAliveDistance; x++)
        {
            for (int y = -keepAliveDistance; y <= keepAliveDistance; y++)
            {
                Vector3 checkPos = new Vector3((playerChunk.x + x) * chunkSize + chunkSize * 0.5f, 
                                             (playerChunk.y + y) * chunkSize + chunkSize * 0.5f, 0);
                Gizmos.DrawWireCube(checkPos, new Vector3(chunkSize, chunkSize, 0));
            }
        }
        
        // 绘制玩家位置
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(playerController.transform.position, 50f);
    }
    
    // 公开方法：重置统计
    public void ResetStatistics()
    {
        totalChunksGenerated = 0;
        totalChunksDestroyed = 0;
        maxChunksAlive = 0;
        gameStartTime = Time.time;
    }
    
    // 公开方法：强制重新生成所有地形
    public void RegenerateAllTerrain()
    {
        foreach (var chunk in terrainChunks.Values)
        {
            if (chunk != null)
            {
                Destroy(chunk);
            }
        }
        terrainChunks.Clear();
        
        if (playerController != null)
        {
            lastPlayerChunkPos = GetChunkPosition(playerController.transform.position);
            GenerateSurroundingChunks();
        }
        
        Debug.Log("🔄 强制重新生成所有地形");
    }
    
    // 公开方法：获取详细区块信息
    public Dictionary<Vector2Int, GameObject> GetActiveChunks()
    {
        return new Dictionary<Vector2Int, GameObject>(terrainChunks);
    }
    
    // 公开方法：动态调整存活距离
    public void SetKeepAliveDistance(int newDistance)
    {
        keepAliveDistance = Mathf.Max(1, newDistance);
        Debug.Log($"🎯 设置地形存活距离: {keepAliveDistance}格");
    }

    // 在OceanTerrainManager中添加公共方法
public Vector2Int GetChunkPosition(Vector3 worldPosition)
{
    int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
    int chunkY = Mathf.FloorToInt(worldPosition.y / chunkSize);
    return new Vector2Int(chunkX, chunkY);
}

// 添加调试信息到GetStatistics方法
    public string GetStatistics()
    {
        if (playerController == null) return "玩家控制器未找到";
    
        float gameDuration = Time.time - gameStartTime;
        Vector2Int currentChunk = GetChunkPosition(playerController.transform.position);
        Vector3 playerPos = playerController.transform.position;
    
        return $"=== 海洋地形统计 ===\n" +
            $"游戏时间: {gameDuration:F1}秒\n" +
            $"玩家位置: {playerPos:F1}\n" +
            $"当前区块: {currentChunk}\n" +
            $"活跃区块数: {terrainChunks.Count}\n" +
            $"区块大小: {chunkSize}\n" +
            $"总生成: {totalChunksGenerated}\n" +
            $"总销毁: {totalChunksDestroyed}";
    }
}