using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
public class Ripple2DEffect : MonoBehaviour
{
    [Header("场景设置")]
    public Color nightColor = new Color(0.141f, 0.188f, 0.298f); // #24304C
    public Color fireflyColor = new Color(1f, 0.8f, 0.2f); // 萤火虫黄色
    
    [Header("天黑效果")]
    public bool enableNight = true;
    [Range(0.1f, 1f)]
    public float nightIntensity = 0.3f; // 降低强度
    [Range(0f, 1f)]
    public float overlayAlpha = 0.2f; // 降低遮罩透明度
    
    [Header("涟漪效果")]
    public GameObject ripplePrefab;
    public float rippleDuration = 2f;
    
    [Header("萤火虫设置")]
    public GameObject fireflyPrefab;
    public int fireflyCount = 15;
    public Vector2 spawnAreaMin = new Vector2(-10, -10);
    public Vector2 spawnAreaMax = new Vector2(10, 10);
    public float followDistance = 3f;
    public float fireflySpeed = 2f;
    public float fireflyFloatHeight = 0.5f;
    
    private Camera mainCamera;
    private List<Firefly> fireflies = new List<Firefly>();
    private Transform player;
    private SpriteRenderer nightOverlay;
    
    [System.Serializable]
    public class Firefly
    {
        public Transform transform;
        public Light2D light2D;
        public SpriteRenderer spriteRenderer;
        public Vector3 targetPosition;
        public bool isFollowing = false;
        public float changeTargetTime = 0f;
    }
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // 设置2D天黑效果
        SetNightEnvironment();
        
        // 生成萤火虫
        SpawnFireflies();
        
        // 查找玩家
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning("未找到带有Player标签的玩家对象");
        }
    }
    
    void Update()
    {
        // 鼠标点击生成涟漪
        if (Input.GetMouseButtonDown(0))
        {
            CreateRippleAtMousePosition();
        }
        
        // 更新萤火虫行为
        UpdateFireflies();
    }
    
    void SetNightEnvironment()
    {
        if (!enableNight) return;
        
        // 方法1：设置相机背景色（使用较浅的颜色）
        if (mainCamera != null)
        {
            Color backgroundColor = nightColor;
            backgroundColor.r += 0.2f; // 增加红色分量
            backgroundColor.g += 0.2f; // 增加绿色分量
            backgroundColor.b += 0.3f; // 增加蓝色分量
            mainCamera.backgroundColor = backgroundColor;
        }
        
        // 方法2：创建轻微的黑夜遮罩
        CreateNightOverlay();
        
        // 方法3：调整环境光（适度）
        RenderSettings.ambientLight = Color.Lerp(Color.white, nightColor, 0.4f);
        
        Debug.Log("适度的天黑效果已应用！");
    }
    
    void CreateNightOverlay()
    {
        // 如果已经有遮罩，先销毁
        if (nightOverlay != null)
        {
            Destroy(nightOverlay.gameObject);
        }
        
        // 创建轻微的黑夜遮罩
        GameObject overlayObject = new GameObject("NightOverlay");
        overlayObject.transform.SetParent(mainCamera.transform);
        overlayObject.transform.localPosition = new Vector3(0, 0, 10); // 在相机前方但不要太近
        overlayObject.transform.localRotation = Quaternion.identity;
        
        // 添加SpriteRenderer
        SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        
        // 创建简单的白色精灵
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        overlayRenderer.sprite = sprite;
        
        // 设置遮罩颜色和透明度（使用更浅的颜色）
        Color overlayColor = new Color(0.1f, 0.15f, 0.25f, overlayAlpha); // 浅蓝色半透明
        overlayRenderer.color = overlayColor;
        
        // 设置渲染顺序
        overlayRenderer.sortingOrder = 999;
        
        // 确保遮罩足够大覆盖整个屏幕
        UpdateOverlaySize(overlayRenderer);
        
        nightOverlay = overlayRenderer;
    }
    
    void UpdateOverlaySize(SpriteRenderer overlayRenderer)
    {
        if (mainCamera == null || overlayRenderer == null) return;
        
        float cameraHeight = mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        overlayRenderer.transform.localScale = new Vector3(cameraWidth, cameraHeight, 1);
    }
    
    void CreateRippleAtMousePosition()
    {
        if (ripplePrefab == null) return;
        
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0;
        
        GameObject ripple = Instantiate(ripplePrefab, worldPosition, Quaternion.identity);
        Destroy(ripple, rippleDuration);
    }
    
    void SpawnFireflies()
    {
        if (fireflyPrefab == null) return;
        
        for (int i = 0; i < fireflyCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject fireflyObj = Instantiate(fireflyPrefab, spawnPos, Quaternion.identity);
            fireflyObj.transform.SetParent(transform);
            
            Firefly firefly = new Firefly
            {
                transform = fireflyObj.transform,
                light2D = fireflyObj.GetComponentInChildren<Light2D>(),
                spriteRenderer = fireflyObj.GetComponent<SpriteRenderer>(),
                targetPosition = GetRandomPositionInArea()
            };
            
            // 设置萤火虫效果
            SetupFireflyAppearance(firefly);
            
            fireflies.Add(firefly);
        }
    }
    
    void SetupFireflyAppearance(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            firefly.light2D.color = fireflyColor;
            firefly.light2D.intensity = 0.5f;
            firefly.light2D.pointLightOuterRadius = 2f;
        }
        
        if (firefly.spriteRenderer != null)
        {
            firefly.spriteRenderer.color = fireflyColor;
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector3(x, y, 0);
    }
    
    Vector3 GetRandomPositionInArea()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector3(x, y, 0);
    }
    
    void UpdateFireflies()
    {
        foreach (Firefly firefly in fireflies)
        {
            if (firefly.transform == null) continue;
            
            UpdateFireflyBehavior(firefly);
            UpdateFireflyMovement(firefly);
            UpdateFireflyLight(firefly);
        }
    }
    
    void UpdateFireflyBehavior(Firefly firefly)
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(firefly.transform.position, player.position);
            
            if (distanceToPlayer <= followDistance)
            {
                firefly.isFollowing = true;
                firefly.targetPosition = GetFollowPosition(player.position);
            }
            else
            {
                firefly.isFollowing = false;
            }
        }
        
        if (!firefly.isFollowing)
        {
            firefly.changeTargetTime -= Time.deltaTime;
            if (firefly.changeTargetTime <= 0f)
            {
                firefly.targetPosition = GetRandomPositionInArea();
                firefly.changeTargetTime = Random.Range(2f, 5f);
            }
        }
    }
    
    void UpdateFireflyMovement(Firefly firefly)
    {
        Vector3 direction = (firefly.targetPosition - firefly.transform.position).normalized;
        firefly.transform.position += direction * fireflySpeed * Time.deltaTime;
        
        // 轻微的浮动效果
        float floatOffset = Mathf.Sin(Time.time * 3f + firefly.transform.GetInstanceID()) * fireflyFloatHeight * Time.deltaTime;
        firefly.transform.position += Vector3.up * floatOffset;
    }
    
    void UpdateFireflyLight(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            float intensity = 0.3f + Mathf.PerlinNoise(Time.time * 4f + firefly.transform.GetInstanceID(), 0) * 0.4f;
            firefly.light2D.intensity = intensity;
        }
        else if (firefly.spriteRenderer != null)
        {
            Color color = firefly.spriteRenderer.color;
            color.a = 0.4f + Mathf.PerlinNoise(Time.time * 4f + firefly.transform.GetInstanceID(), 0) * 0.6f;
            firefly.spriteRenderer.color = color;
        }
    }
    
    Vector3 GetFollowPosition(Vector3 playerPos)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 1.5f;
        return playerPos + new Vector3(randomOffset.x, randomOffset.y, 0);
    }
    
    // 调试方法
    [ContextMenu("应用适度天黑效果")]
    public void ApplyNightEffect()
    {
        SetNightEnvironment();
        Debug.Log("适度天黑效果已应用");
    }
    
    [ContextMenu("移除天黑效果")]
    public void RemoveNightEffect()
    {
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.clear;
        }
        
        if (nightOverlay != null)
        {
            DestroyImmediate(nightOverlay.gameObject);
            nightOverlay = null;
        }
        
        Debug.Log("天黑效果已移除");
    }
    
    [ContextMenu("调整天黑强度 - 更亮")]
    public void MakeBrighter()
    {
        nightIntensity = Mathf.Clamp(nightIntensity - 0.1f, 0.1f, 1f);
        overlayAlpha = Mathf.Clamp(overlayAlpha - 0.1f, 0f, 0.3f);
        SetNightEnvironment();
        Debug.Log($"天黑强度调整: 强度={nightIntensity}, 透明度={overlayAlpha}");
    }
    
    [ContextMenu("调整天黑强度 - 更暗")]
    public void MakeDarker()
    {
        nightIntensity = Mathf.Clamp(nightIntensity + 0.1f, 0.1f, 1f);
        overlayAlpha = Mathf.Clamp(overlayAlpha + 0.1f, 0f, 0.3f);
        SetNightEnvironment();
        Debug.Log($"天黑强度调整: 强度={nightIntensity}, 透明度={overlayAlpha}");
    }
    
    void OnDrawGizmosSelected()
    {
        // 萤火虫生成区域
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) * 0.5f,
            (spawnAreaMin.y + spawnAreaMax.y) * 0.5f,
            0
        );
        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            spawnAreaMax.y - spawnAreaMin.y,
            0.1f
        );
        Gizmos.DrawWireCube(center, size);
        
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, followDistance);
        }
    }
}