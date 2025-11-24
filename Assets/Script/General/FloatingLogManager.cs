using UnityEngine;
using System.Collections.Generic;

public class FloatingLogManager : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject logPrefab;
    public int maxLogs = 8;
    public float spawnCheckInterval = 1f;
    
    [Header("固定判定区域")]
    public Vector2 detectionAreaSize = new Vector2(30f, 20f);
    public Vector2 detectionAreaCenter = Vector2.zero;
    
    [Header("浮木生成设置")]
    public float spawnDistance = 10f;
    public float despawnDistance = 25f;
    
    [Header("浮木大小设置")]
    public float baseScale = 0.3f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    
    [Header("外观设置")]
    public Sprite[] availableSprites;
    
    [Header("调试显示")]
    public bool showGizmos = true;
    
    private Transform player;
    private List<FloatingLog> activeLogs = new List<FloatingLog>();
    private float lastCheckTime;
    private Rect detectionRect;
    private bool isPlayerInArea = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastCheckTime = Time.time;
        
        detectionRect = new Rect(
            detectionAreaCenter.x - detectionAreaSize.x / 2,
            detectionAreaCenter.y - detectionAreaSize.y / 2,
            detectionAreaSize.x,
            detectionAreaSize.y
        );
        
        StartCoroutine(InitialSpawn());
    }

    System.Collections.IEnumerator InitialSpawn()
    {
        for (int i = 0; i < Mathf.Min(3, maxLogs); i++)
        {
            if (isPlayerInArea)
            {
                TrySpawnLog();
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    void Update()
    {
        if (player == null) return;
        
        bool wasInArea = isPlayerInArea;
        isPlayerInArea = detectionRect.Contains(player.position);
        
        if (isPlayerInArea)
        {
            if (Time.time - lastCheckTime >= spawnCheckInterval)
            {
                lastCheckTime = Time.time;
                MaintainLogCount();
                CleanupDistantLogs();
            }
        }
        else if (wasInArea)
        {
            ClearAllLogs();
        }
    }

    void MaintainLogCount()
    {
        activeLogs.RemoveAll(log => log == null);
        
        if (activeLogs.Count < maxLogs)
        {
            int logsToSpawn = maxLogs - activeLogs.Count;
            for (int i = 0; i < logsToSpawn; i++)
            {
                TrySpawnLog();
            }
        }
    }

    void TrySpawnLog()
    {
        Vector3 spawnPosition = FindSpawnPositionInSector();
        if (spawnPosition != Vector3.zero)
        {
            SpawnLog(spawnPosition);
        }
        else
        {
            spawnPosition = FindRandomPositionInArea();
            if (spawnPosition != Vector3.zero)
            {
                SpawnLog(spawnPosition);
            }
        }
    }

    Vector3 FindSpawnPositionInSector()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 spawnPos = GetPositionInSector();
            
            if (!detectionRect.Contains(spawnPos))
                continue;
            
            if (IsPositionValid(spawnPos))
            {
                return spawnPos;
            }
        }
        
        return Vector3.zero;
    }

    Vector3 GetPositionInSector()
    {
        Vector3 basePosition = player.position + Vector3.up * spawnDistance;
        float randomAngle = Random.Range(-90f, 90f);
        float randomDistance = Random.Range(-3f, 3f);
        
        Vector3 offset = Quaternion.Euler(0, 0, randomAngle) * Vector3.up * randomDistance;
        return basePosition + offset;
    }

    Vector3 FindRandomPositionInArea()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = GetRandomPositionInArea();
            
            if (IsPositionValid(spawnPos))
            {
                return spawnPos;
            }
        }
        
        return Vector3.zero;
    }

    Vector3 GetRandomPositionInArea()
    {
        float x = Random.Range(detectionRect.xMin, detectionRect.xMax);
        float y = Random.Range(detectionRect.yMin, detectionRect.yMax);
        return new Vector3(x, y, 0);
    }

    bool IsPositionValid(Vector3 position)
    {
        Collider2D obstacle = Physics2D.OverlapCircle(position, 1.5f);
        if (obstacle != null && !obstacle.CompareTag("Player"))
            return false;
            
        foreach (FloatingLog log in activeLogs)
        {
            if (log != null && Vector3.Distance(position, log.transform.position) < 3f)
                return false;
        }
        
        return true;
    }

    void SpawnLog(Vector3 position)
    {
        GameObject logObj = Instantiate(logPrefab, position, Quaternion.identity);
        FloatingLog log = logObj.GetComponent<FloatingLog>();
        
        if (log != null)
        {
            log.baseScale = baseScale;
            log.minScale = minScale;
            log.maxScale = maxScale;
            
            if (availableSprites != null && availableSprites.Length > 0)
            {
                Sprite randomSprite = availableSprites[Random.Range(0, availableSprites.Length)];
                log.SetSprite(randomSprite);
            }
            
            Vector2 baseDirection = Vector2.up;
            Vector2 randomDirection = (baseDirection + Random.insideUnitCircle * 0.2f).normalized;
            log.SetMoveDirection(randomDirection);
            
            activeLogs.Add(log);
        }
    }

    void CleanupDistantLogs()
    {
        for (int i = activeLogs.Count - 1; i >= 0; i--)
        {
            if (activeLogs[i] == null)
            {
                activeLogs.RemoveAt(i);
                continue;
            }
            
            float distanceToPlayer = Vector3.Distance(activeLogs[i].transform.position, player.position);
            if (distanceToPlayer > despawnDistance)
            {
                Destroy(activeLogs[i].gameObject);
                activeLogs.RemoveAt(i);
            }
        }
    }

    void ClearAllLogs()
    {
        foreach (FloatingLog log in activeLogs)
        {
            if (log != null)
                Destroy(log.gameObject);
        }
        activeLogs.Clear();
    }

    [ContextMenu("补充浮木")]
    public void ForceSpawnLogs()
    {
        if (!isPlayerInArea)
        {
            Debug.LogWarning("玩家不在判定区域内，无法生成浮木");
            return;
        }
        
        int logsToSpawn = maxLogs - activeLogs.Count;
        for (int i = 0; i < logsToSpawn; i++)
        {
            TrySpawnLog();
        }
    }

    // 可选的Gizmos显示
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        DrawGizmos(true);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawGizmos(false);
    }

    void DrawGizmos(bool isSelected)
    {
        // 绘制固定判定区域
        Gizmos.color = isPlayerInArea ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(detectionAreaCenter, detectionAreaSize);
        
        // 绘制区域中心
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(detectionAreaCenter, Vector3.one * 0.5f);
        
        if (player != null)
        {
            // 绘制玩家位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, 0.5f);
            
            // 绘制生成距离
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position + Vector3.up * spawnDistance, 1f);
            
            // 绘制销毁距离
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, despawnDistance);
            
            // 绘制180度扇形区域（始终向上）
            DrawSectorGizmo(player.position, Vector3.up, spawnDistance, 90f, new Color(0, 1, 0, 0.3f));
        }
        
        // 显示文本信息
        #if UNITY_EDITOR
        if (isSelected)
        {
            UnityEditor.Handles.Label(detectionAreaCenter, 
                $"固定判定区域\n大小: {detectionAreaSize.x:F1} x {detectionAreaSize.y:F1}\n" +
                $"玩家在区域内: {isPlayerInArea}\n" +
                $"浮木: {activeLogs.Count}/{maxLogs}");
        }
        #endif
    }

    void DrawSectorGizmo(Vector3 center, Vector3 direction, float radius, float angle, Color color)
    {
        Gizmos.color = color;
        int segments = 16;
        float angleStep = angle * 2 / segments;
        
        Vector3 leftBound = center + Quaternion.Euler(0, 0, -angle) * direction * radius;
        Vector3 rightBound = center + Quaternion.Euler(0, 0, angle) * direction * radius;
        
        Vector3 prevPoint = leftBound;
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle + angleStep * i;
            Vector3 currentPoint = center + Quaternion.Euler(0, 0, currentAngle) * direction * radius;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        
        Gizmos.DrawLine(center, leftBound);
        Gizmos.DrawLine(center, rightBound);
        
        // 填充扇形区域（玩家在区域内时显示）
        if (isPlayerInArea)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = new Color(0, 1, 0, 0.1f);
            Vector3[] sectorVertices = new Vector3[segments + 2];
            sectorVertices[0] = center;
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = -angle + angleStep * i;
                sectorVertices[i + 1] = center + Quaternion.Euler(0, 0, currentAngle) * direction * radius;
            }
            UnityEditor.Handles.DrawAAConvexPolygon(sectorVertices);
            #endif
        }
    }
}