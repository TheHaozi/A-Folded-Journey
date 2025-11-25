using UnityEngine;
using System.Collections.Generic;

public class BeaconSpawner : MonoBehaviour
{
    [Header("航标设置")]
    public GameObject beaconPrefab;
    public float beaconSpawnInterval = 100f;
    public float maxBeaconDistance = 800f;
    public float triggerDistanceOffset = 50f;

    [Header("调试")]
    public bool enableDebug = true;

    private List<GameObject> beacons = new List<GameObject>();
    private HashSet<int> spawnedBeaconDistances = new HashSet<int>();
    private OceanTerrainManager terrainManager;

    void Start()
    {
        terrainManager = FindObjectOfType<OceanTerrainManager>();
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

        if (enableDebug)
        {
            Debug.Log($"📍 成功生成 {distance} 米航标");
            Debug.Log($"   位置: {position}");
        }

        beacons.Add(beacon);
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
}