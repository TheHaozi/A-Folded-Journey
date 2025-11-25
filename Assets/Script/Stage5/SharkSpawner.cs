using UnityEngine;
using System.Collections.Generic;

public class SharkSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject sharkPrefab;
    public float spawnDistanceFromPlayer = 300f; // 在玩家300米外生成
    public int maxSharks = 6; // 全世界最多6只鲨鱼
    public float spawnCheckInterval = 5f; // 每5秒检查一次生成

    [Header("调试")]
    public bool enableDebugLogs = true;

    private List<GameObject> activeSharks = new List<GameObject>();
    private OceanTerrainManager terrainManager;
    private float spawnTimer = 0f;

    void Start()
    {
        terrainManager = FindObjectOfType<OceanTerrainManager>();
        if (sharkPrefab == null)
        {
            Debug.LogError("❌ 需要分配鲨鱼预制体！");
        }
    }

    void Update()
    {
        if (sharkPrefab == null || DistanceManager.Instance == null || terrainManager == null) return;

        spawnTimer += Time.deltaTime;

        // 定期检查生成条件
        if (spawnTimer >= spawnCheckInterval)
        {
            TrySpawnSharks();
            CleanupSharksInUnloadedChunks();
            spawnTimer = 0f;
        }

        // 调试信息
        if (enableDebugLogs && Time.frameCount % 180 == 0)
        {
            Debug.Log($"🦈 鲨鱼状态: {activeSharks.Count}/{maxSharks} 只活跃");
        }
    }

    void TrySpawnSharks()
    {
        // 如果已经达到最大数量，不生成
        if (activeSharks.Count >= maxSharks) return;

        Vector3 playerPos = DistanceManager.Instance.PlayerPosition;
        Vector2Int playerChunk = terrainManager.GetChunkPosition(playerPos);

        // 获取玩家周围已加载的区块
        var loadedChunks = terrainManager.GetActiveChunks();
        
        int sharksToSpawn = maxSharks - activeSharks.Count;
        int spawnedCount = 0;

        foreach (var chunkPair in loadedChunks)
        {
            if (spawnedCount >= sharksToSpawn) break;

            Vector2Int chunkPos = chunkPair.Key;
            
            // 只在远离玩家的区块生成
            if (IsChunkFarFromPlayer(chunkPos, playerChunk))
            {
                if (TrySpawnSharkInChunk(chunkPos))
                {
                    spawnedCount++;
                }
            }
        }

        if (spawnedCount > 0 && enableDebugLogs)
        {
            Debug.Log($"🎯 在远离玩家的区块生成了 {spawnedCount} 只鲨鱼");
        }
    }

    bool IsChunkFarFromPlayer(Vector2Int chunkPos, Vector2Int playerChunk)
    {
        // 计算区块距离（曼哈顿距离）
        int distance = Mathf.Abs(chunkPos.x - playerChunk.x) + Mathf.Abs(chunkPos.y - playerChunk.y);
        return distance >= 2; // 距离玩家至少2个区块
    }

    bool TrySpawnSharkInChunk(Vector2Int chunkPos)
    {
        // 计算区块中心位置
        Vector3 chunkCenter = new Vector3(
            chunkPos.x * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f,
            chunkPos.y * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f,
            0f
        );

        // 在区块内随机位置生成
        Vector3 spawnPos = GetRandomPositionInChunk(chunkPos);
        
        GameObject shark = Instantiate(sharkPrefab, spawnPos, Quaternion.identity);
        shark.transform.SetParent(transform);

        // 设置鲨鱼信息
        SharkController controller = shark.GetComponent<SharkController>();
        if (controller != null)
        {
            controller.Initialize(chunkPos);
        }
        else
        {
            Debug.LogError($"❌ 鲨鱼预制体缺少 SharkController 组件！");
            Destroy(shark);
            return false;
        }

        activeSharks.Add(shark);
        return true;
    }

    Vector3 GetRandomPositionInChunk(Vector2Int chunkPos)
    {
        float chunkCenterX = chunkPos.x * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f;
        float chunkCenterY = chunkPos.y * terrainManager.chunkSize + terrainManager.chunkSize * 0.5f;
        float halfChunk = terrainManager.chunkSize * 0.5f - 2f; // 留出边界

        float randomX = Random.Range(-halfChunk, halfChunk);
        float randomY = Random.Range(-halfChunk, halfChunk);

        return new Vector3(chunkCenterX + randomX, chunkCenterY + randomY, 0f);
    }

    void CleanupSharksInUnloadedChunks()
    {
        var loadedChunks = terrainManager.GetActiveChunks();
        
        foreach (var shark in activeSharks.ToArray())
        {
            if (shark == null)
            {
                activeSharks.Remove(shark);
                continue;
            }

            SharkController controller = shark.GetComponent<SharkController>();
            if (controller != null && controller.HomeChunk.HasValue)
            {
                Vector2Int homeChunk = controller.HomeChunk.Value;
                if (!loadedChunks.ContainsKey(homeChunk))
                {
                    // 鲨鱼所在的区块已卸载，销毁鲨鱼
                    Destroy(shark);
                    activeSharks.Remove(shark);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🗑️ 区块卸载，销毁鲨鱼: {shark.name}");
                    }
                }
            }
        }
    }

    // 获取当前活跃鲨鱼数量
    public int GetActiveSharkCount()
    {
        return activeSharks.Count;
    }

    [ContextMenu("生成测试鲨鱼")]
    public void SpawnTestSharks()
    {
        if (terrainManager == null) return;

        var loadedChunks = terrainManager.GetActiveChunks();
        int count = 0;
        
        foreach (var chunkPair in loadedChunks)
        {
            if (count >= 2) break;
            if (TrySpawnSharkInChunk(chunkPair.Key))
            {
                count++;
            }
        }
    }

    [ContextMenu("重置所有鲨鱼")]
    public void ResetAllSharks()
    {
        foreach (var shark in activeSharks)
        {
            if (shark != null)
                Destroy(shark);
        }
        activeSharks.Clear();
        Debug.Log("🔄 重置所有鲨鱼");
    }

    [ContextMenu("显示生成器状态")]
    public void ShowSpawnerStatus()
    {
        Debug.Log($"📊 鲨鱼生成器状态:");
        Debug.Log($"   当前鲨鱼数量: {activeSharks.Count}/{maxSharks}");
        Debug.Log($"   已加载区块数: {terrainManager.GetActiveChunks().Count}");
        
        foreach (var shark in activeSharks)
        {
            if (shark != null)
            {
                SharkController controller = shark.GetComponent<SharkController>();
                if (controller != null && controller.HomeChunk.HasValue)
                {
                    Debug.Log($"   🦈 {shark.name} - 所在区块: {controller.HomeChunk.Value}");
                }
            }
        }
    }
}