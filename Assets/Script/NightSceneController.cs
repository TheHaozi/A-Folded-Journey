using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class NightScene2DController : MonoBehaviour
{
    [Header("天黑效果设置")]
    public Color nightColor = new Color(0.141f, 0.188f, 0.298f);
    
    [Header("光照控制")]
    public Light2D globalLight2D;
    public float globalLightIntensityNight = 0.3f;
    
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
    private Light2D nightLight;

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
        
        // 设置天黑效果
        SetNightEnvironment();
        
        // 生成萤火虫
        SpawnFireflies();
        
        // 查找玩家
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void SetNightEnvironment()
    {
        // 设置相机背景色（最可靠的方法）
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = nightColor;
        }
        
        // 方法1：调整现有全局光照
        if (globalLight2D != null)
        {
            globalLight2D.intensity = globalLightIntensityNight;
            // 尝试强制设置颜色
            StartCoroutine(ForceSetLightColor(globalLight2D, nightColor));
        }
        else
        {
            // 方法2：创建专用的夜晚光照
            CreateNightLight();
        }
    }
    
    System.Collections.IEnumerator ForceSetLightColor(Light2D light, Color color)
    {
        // 在几帧内持续设置颜色，覆盖系统默认值
        for (int i = 0; i < 3; i++)
        {
            light.color = color;
            light.intensity = globalLightIntensityNight;
            yield return new WaitForEndOfFrame();
        }
    }
    
    void CreateNightLight()
    {
        if (nightLight != null) return;
        
        GameObject nightLightObj = new GameObject("NightLight2D");
        nightLight = nightLightObj.AddComponent<Light2D>();
        nightLight.lightType = Light2D.LightType.Global;
        nightLight.intensity = globalLightIntensityNight;
        nightLight.color = nightColor;
        
        // 设置光照混合模式为覆盖
        nightLight.blendStyleIndex = 0;
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
        
        // 持续强制设置光照颜色（如果需要）
        if (globalLight2D != null && globalLight2D.color != nightColor)
        {
            globalLight2D.color = nightColor;
        }
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
            
            SetupFireflyAppearance(firefly);
            fireflies.Add(firefly);
        }
    }
    
    void SetupFireflyAppearance(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            firefly.light2D.color = new Color(1f, 0.8f, 0.2f);
            firefly.light2D.intensity = 0.8f;
            firefly.light2D.pointLightOuterRadius = 1.5f;
        }
        
        if (firefly.spriteRenderer != null)
        {
            firefly.spriteRenderer.color = new Color(1f, 0.8f, 0.2f);
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
        
        float floatOffset = Mathf.Sin(Time.time * 3f + firefly.transform.GetInstanceID()) * fireflyFloatHeight * Time.deltaTime;
        firefly.transform.position += Vector3.up * floatOffset;
    }
    
    void UpdateFireflyLight(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            float intensity = 0.4f + Mathf.PerlinNoise(Time.time * 4f + firefly.transform.GetInstanceID(), 0) * 0.6f;
            firefly.light2D.intensity = intensity;
        }
    }
    
    Vector3 GetFollowPosition(Vector3 playerPos)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 1.5f;
        return playerPos + new Vector3(randomOffset.x, randomOffset.y, 0);
    }
}