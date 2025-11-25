using UnityEngine;
using System.Collections.Generic;

public class BeaconSpawner : MonoBehaviour
{
    [Header("航标设置")]
    public GameObject beaconPrefab;
    public float beaconSpawnInterval = 100f;
    public float maxBeaconDistance = 800f;
    public float triggerDistanceOffset = 50f;

    [Header("颜色设置")]
    public Color[] predefinedColors; // 可选的预定义颜色数组
    public bool useRandomColors = true; // 是否使用随机颜色

    [Header("调试")]
    public bool enableDebug = true;

    private List<GameObject> beacons = new List<GameObject>();
    private HashSet<int> spawnedBeaconDistances = new HashSet<int>();
    private OceanTerrainManager terrainManager;

    void Start()
    {
        terrainManager = FindObjectOfType<OceanTerrainManager>();
        
        // 如果没有预定义颜色，设置一些默认的鲜艳颜色
        if (predefinedColors == null || predefinedColors.Length == 0)
        {
            predefinedColors = new Color[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.magenta,
                Color.cyan,
                new Color(1f, 0.5f, 0f), // 橙色
                new Color(0.5f, 0f, 1f), // 紫色
                new Color(0f, 1f, 0.5f), // 春绿色
                new Color(1f, 0f, 0.5f)  // 玫瑰红
            };
        }
        
        if (enableDebug) Debug.Log("🚢 航标系统启动");
    }

    void Update()
    {
        if (DistanceManager.Instance == null)
        {
            if (enableDebug) Debug.LogError("❌ DistanceManager未找到");
            return;
        }

        if (terrainManager == null)
        {
            terrainManager = FindObjectOfType<OceanTerrainManager>();
            return;
        }

        float currentDistance = DistanceManager.Instance.CurrentDistance;
        
        if (enableDebug && Time.frameCount % 60 == 0) // 每60帧输出一次
        {
            Debug.Log($"📊 当前距离: {currentDistance:F1}米, 已生成航标: {beacons.Count}个");
        }
        
        CheckAndSpawnBeacons(currentDistance);
    }

    void CheckAndSpawnBeacons(float currentDistance)
    {
        if (currentDistance > maxBeaconDistance) return;

        // 简化逻辑：每100米生成一个航标
        for (int targetDistance = 100; targetDistance <= maxBeaconDistance; targetDistance += 100)
        {
            if (!spawnedBeaconDistances.Contains(targetDistance) && 
                currentDistance >= targetDistance - triggerDistanceOffset &&
                currentDistance <= targetDistance + 20f) // 增加上限避免重复生成
            {
                if (enableDebug) Debug.Log($"🎯 生成条件满足: {currentDistance:F1}米 → {targetDistance}米航标");
                SpawnBeaconAtDistance(targetDistance);
                spawnedBeaconDistances.Add(targetDistance);
                break; // 一次只生成一个
            }
        }
    }

    void SpawnBeaconAtDistance(int targetDistance)
    {
        Vector3 beaconPosition = CalculateForwardIntersection(targetDistance);
        
        if (beaconPosition != Vector3.zero)
        {
            CreateBeacon(beaconPosition, targetDistance);
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"⚠️ 无法计算 {targetDistance} 米航标位置");
        }
    }

    Vector3 CalculateForwardIntersection(float targetDistance)
    {
        Vector3 playerPos = DistanceManager.Instance.PlayerPosition;
        Vector3 centerPos = DistanceManager.Instance.centerPoint.position;
        Vector2 moveDirection = DistanceManager.Instance.PlayerMoveDirection;

        if (!DistanceManager.Instance.IsPlayerMoving || moveDirection == Vector2.zero)
        {
            Vector3 toCenter = centerPos - playerPos;
            moveDirection = -toCenter.normalized;
            if (enableDebug) Debug.Log($"🔄 使用远离中心方向: {moveDirection}");
        }

        Vector3 intersection = CalculateRayCircleIntersection(playerPos, moveDirection, centerPos, targetDistance);
        
        if (intersection == Vector3.zero) 
        {
            // 备用方法：直接在目标距离圆上生成
            Vector3 toPlayer = (playerPos - centerPos).normalized;
            intersection = centerPos + toPlayer * targetDistance;
            if (enableDebug) Debug.Log($"🔄 使用径向方法计算位置");
        }

        float actualDistance = Vector3.Distance(intersection, centerPos);
        float error = Mathf.Abs(actualDistance - targetDistance);

        if (enableDebug)
        {
            Debug.Log($"🎯 计算{targetDistance}米航标:");
            Debug.Log($"   玩家位置: {playerPos} (距中心: {Vector3.Distance(playerPos, centerPos):F1}米)");
            Debug.Log($"   航标位置: {intersection}");
            Debug.Log($"   实际距离: {actualDistance:F1}米, 误差: {error:F3}米");
        }

        return intersection;
    }

    Vector3 CalculateRayCircleIntersection(Vector3 rayOrigin, Vector2 rayDirection, Vector3 circleCenter, float radius)
    {
        Vector3 localOrigin = rayOrigin - circleCenter;
        Vector3 direction = (Vector3)rayDirection;
        
        float a = Vector3.Dot(direction, direction);
        float b = 2f * Vector3.Dot(localOrigin, direction);
        float c = Vector3.Dot(localOrigin, localOrigin) - radius * radius;
        
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0) return Vector3.zero;
        
        float t = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
        return rayOrigin + direction * t;
    }

    void CreateBeacon(Vector3 position, int distance)
    {
        if (beaconPrefab == null)
        {
            Debug.LogError("❌ 航标预制体未分配！");
            return;
        }

        GameObject beacon = Instantiate(beaconPrefab, position, Quaternion.identity);
        beacon.name = $"Beacon_{distance}m";
        beacon.transform.SetParent(transform);

        // 设置航标颜色
        SetBeaconColor(beacon, distance);

        if (enableDebug)
        {
            Debug.Log($"📍 成功生成 {distance} 米航标");
            Debug.Log($"   位置: {position}");
        }

        beacons.Add(beacon);
    }

    /// <summary>
    /// 为航标设置随机颜色（排除灰色和黑色）
    /// </summary>
    void SetBeaconColor(GameObject beacon, int distance)
    {
        Renderer renderer = beacon.GetComponent<Renderer>();
        if (renderer == null)
        {
            if (enableDebug) Debug.LogWarning($"⚠️ 航标 {distance} 米没有Renderer组件");
            return;
        }

        Color randomColor = GetRandomNonGrayColor();
        renderer.material.color = randomColor;

        if (enableDebug) Debug.Log($"🎨 设置 {distance} 米航标颜色: {randomColor}");
    }

    /// <summary>
    /// 获取非灰色和黑色的随机颜色
    /// </summary>
    Color GetRandomNonGrayColor()
    {
        if (useRandomColors && predefinedColors != null && predefinedColors.Length > 0)
        {
            // 从预定义颜色中随机选择
            return predefinedColors[Random.Range(0, predefinedColors.Length)];
        }
        else
        {
            // 生成随机颜色，排除灰色和黑色
            Color randomColor;
            do
            {
                randomColor = new Color(
                    Random.Range(0.3f, 1f),
                    Random.Range(0.3f, 1f),
                    Random.Range(0.3f, 1f)
                );
            } 
            while (IsGrayColor(randomColor) || IsBlackColor(randomColor));
            
            return randomColor;
        }
    }

    /// <summary>
    /// 判断颜色是否为灰色（RGB分量相近）
    /// </summary>
    bool IsGrayColor(Color color)
    {
        float diff = Mathf.Max(
            Mathf.Abs(color.r - color.g),
            Mathf.Abs(color.g - color.b),
            Mathf.Abs(color.b - color.r)
        );
        return diff < 0.2f; // 如果RGB分量差异小于0.2，认为是灰色
    }

    /// <summary>
    /// 判断颜色是否为黑色（亮度很低）
    /// </summary>
    bool IsBlackColor(Color color)
    {
        float brightness = (color.r + color.g + color.b) / 3f;
        return brightness < 0.2f; // 如果平均亮度小于0.2，认为是黑色
    }

    [ContextMenu("强制生成所有航标")]
    public void ForceSpawnAllBeacons()
    {
        for (int distance = 100; distance <= 800; distance += 100)
        {
            if (!spawnedBeaconDistances.Contains(distance))
            {
                SpawnBeaconAtDistance(distance);
                spawnedBeaconDistances.Add(distance);
            }
        }
    }

    [ContextMenu("显示航标状态")]
    public void ShowBeaconStatus()
    {
        Debug.Log($"📋 航标状态:");
        Debug.Log($"   已生成: {beacons.Count}个");
        Debug.Log($"   玩家距离: {DistanceManager.Instance.CurrentDistance:F1}米");
        
        foreach (var distance in spawnedBeaconDistances)
        {
            Debug.Log($"   ✅ {distance}米航标已生成");
        }
        
        for (int distance = 100; distance <= 800; distance += 100)
        {
            if (!spawnedBeaconDistances.Contains(distance))
            {
                Debug.Log($"   ❌ {distance}米航标未生成");
            }
        }
    }

    [ContextMenu("重新随机所有航标颜色")]
    public void RandomizeAllBeaconColors()
    {
        foreach (GameObject beacon in beacons)
        {
            if (beacon != null)
            {
                Renderer renderer = beacon.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetRandomNonGrayColor();
                }
            }
        }
        if (enableDebug) Debug.Log("🔄 所有航标颜色已重新随机");
    }
}