using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RippleGenerator : MonoBehaviour
{
    [Header("基础设置")]
    public GameObject ripplePrefab;
    public LayerMask obstacleLayers = -1;
    
    [Header("颜色设置")]
    public bool useRandomColors = true;
    public Color fixedColor = new Color(0.2f, 0.5f, 1f, 1f);
    
    [Header("反射设置")]
    public bool enableReflection = true;
    public GameObject reflectionPrefab;
    public LayerMask wallLayers = -1;
    public float reflectionDistance = 5f;
    public float waveSpeed = 2f;
    public int maxReflections = 3;
    
    private Camera mainCamera;

    void Start() => mainCamera = Camera.main;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            CreateRipple();
    }

    void CreateRipple()
    {
        if (ripplePrefab == null || mainCamera == null) return;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (Physics2D.OverlapCircle(mousePos, 0.3f, obstacleLayers))
            return;
        
        GameObject ripple = Instantiate(ripplePrefab, mousePos, Quaternion.identity);
        SetupRippleColor(ripple);
        
        if (enableReflection && reflectionPrefab != null)
        {
            FindWallsAndScheduleReflections(mousePos);
        }
        
        Debug.Log($"✅ 创建涟漪在: {mousePos}");
    }

    void SetupRippleColor(GameObject ripple)
    {
        SimpleRipple rippleScript = ripple.GetComponent<SimpleRipple>();
        if (rippleScript != null)
        {
            rippleScript.useRandomColor = useRandomColors;
            if (!useRandomColors) rippleScript.rippleColor = fixedColor;
        }
    }

    void FindWallsAndScheduleReflections(Vector2 center)
    {
        List<ReflectionPoint> reflectionPoints = new List<ReflectionPoint>();
        
        Vector2[] directions = {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1, 1).normalized, new Vector2(1, -1).normalized,
            new Vector2(-1, 1).normalized, new Vector2(-1, -1).normalized
        };
        
        foreach (Vector2 direction in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(center, direction, reflectionDistance, wallLayers);
            if (hit.collider != null)
            {
                float distance = Vector2.Distance(center, hit.point);
                reflectionPoints.Add(new ReflectionPoint {
                    point = hit.point,
                    distance = distance,
                    direction = direction
                });
            }
        }
        
        List<ReflectionPoint> filteredPoints = FilterDuplicatePoints(reflectionPoints);
        filteredPoints.Sort((a, b) => a.distance.CompareTo(b.distance));
        int reflectionsToCreate = Mathf.Min(filteredPoints.Count, maxReflections);
        
        Debug.Log($"🔍 找到 {reflectionPoints.Count} 个碰撞点，过滤后 {filteredPoints.Count} 个，将创建 {reflectionsToCreate} 个反射波");
        
        for (int i = 0; i < reflectionsToCreate; i++)
        {
            ReflectionPoint reflection = filteredPoints[i];
            float delayTime = reflection.distance / waveSpeed / 2;
            StartCoroutine(CreateDelayedReflection(reflection.point, delayTime, reflection.distance, i + 1));
        }
    }

    List<ReflectionPoint> FilterDuplicatePoints(List<ReflectionPoint> points)
    {
        List<ReflectionPoint> filtered = new List<ReflectionPoint>();
        
        foreach (ReflectionPoint point in points)
        {
            bool isDuplicate = false;
            
            for (int i = filtered.Count - 1; i >= 0; i--)
            {
                if (IsSameWall(filtered[i].point, point.point))
                {
                    if (point.distance < filtered[i].distance)
                    {
                        filtered[i] = point;
                    }
                    isDuplicate = true;
                    break;
                }
            }
            
            if (!isDuplicate)
            {
                filtered.Add(point);
            }
        }
        
        return filtered;
    }

    bool IsSameWall(Vector2 point1, Vector2 point2)
    {
        float tolerance = 0.1f;
        return Mathf.Abs(point1.x - point2.x) < tolerance || Mathf.Abs(point1.y - point2.y) < tolerance;
    }

    IEnumerator CreateDelayedReflection(Vector2 reflectionPoint, float delay, float distance, int reflectionIndex)
    {
        yield return new WaitForSeconds(delay);
        
        if (reflectionPrefab != null)
        {
            GameObject reflection = Instantiate(reflectionPrefab, reflectionPoint, Quaternion.identity);
            SimpleRipple ripple = reflection.GetComponent<SimpleRipple>();
            
            if (ripple != null)
            {
                float distanceFactor = Mathf.Clamp01(1f - (distance / reflectionDistance));
                ripple.startSize = 0.3f * distanceFactor;
                ripple.endSize = 2f * distanceFactor;
                ripple.lifetime = 1f * distanceFactor;
            }
            
            Debug.Log($"🌊 反射波 #{reflectionIndex} 到达! 位置: {reflectionPoint}, 距离: {distance:F1}, 延迟: {delay:F2}秒");
        }
    }

    struct ReflectionPoint
    {
        public Vector2 point;
        public float distance;
        public Vector2 direction;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, reflectionDistance);
    }
}