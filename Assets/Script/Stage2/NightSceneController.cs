using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class NightScene2DController : MonoBehaviour
{
    [Header("天黑效果设置")]
    public Color nightColor = new Color(0.141f, 0.188f, 0.298f);
    
    [Header("全局光照控制")]
    public Light2D globalLight2D;
    public float globalLightIntensityNight = 0.3f;
    
    [Header("萤火虫设置")]
    public GameObject fireflyPrefab;
    public int fireflyCount = 15;
    public Vector2 spawnAreaMin = new Vector2(-10, -10);
    public Vector2 spawnAreaMax = new Vector2(10, 10);
    public float followDistance = 4f;                    // 稍微增加跟随距离
    public float normalSpeed = 2f;                       // 降低正常速度
    public float followSpeed = 6f;                       // 降低跟随速度
    public float catchUpSpeed = 18f;                     // 降低最大速度
    public float fireflyFloatHeight = 0.5f;
    
    [Header("围绕玩家设置")]
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 2f;
    public float randomMovementRange = 0.5f;
    public float directionChangeInterval = 1f;
    
    [Header("萤火虫亮度设置")]
    public float baseLightIntensity = 1.5f;
    public float minIntensity = 0.6f;
    public float maxIntensity = 1.8f;
    public float orbitingLightBoost = 1.3f;
    public Color fireflyColor = new Color(1f, 0.9f, 0.6f);
    public float lightRadius = 2.5f;
    public float falloffIntensity = 0.8f;
    
    [Header("萤火虫大小设置")]
    public float minSize = 0.3f;
    public float maxSize = 0.8f;
    public bool randomizeSize = true;
    public float sizeChangeSpeed = 0.5f;
    public float sizePulseAmount = 0.2f;
    public float sizePulseFrequency = 2f;
    
    [Header("中心点设置")]
    public Color centerDotColor = new Color(1f, 1f, 1f, 1f);
    public float centerDotSize = 0.1f;
    public bool enableCenterDot = true;
    
    [Header("相对坐标移动设置")]
    public bool useRelativeMovement = true;
    public float relativeOrbitSpeed = 2f;
    public float movementSmoothTime = 0.3f;
    public float centerArrivalRadius = 0.2f;

    [Header("智能跟随设置")]
    public float predictionTime = 0.8f;                  // 预测玩家未来位置的时间
    public float smoothAcceleration = 3f;                // 平滑加速度
    public float minSmoothTime = 0.15f;                  // 最小平滑时间
    public float maxSmoothTime = 0.35f;                  // 最大平滑时间

    private Camera mainCamera;
    private List<Firefly> fireflies = new List<Firefly>();
    private Transform player;
    
    [System.Serializable]
    public class Firefly
    {
        public Transform transform;
        public Light2D light2D;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer centerDotRenderer;
        public Vector3 targetPosition;
        public bool isFollowing = false;
        public bool isOrbiting = false;
        public bool hasArrivedAtCenter = false;
        public float changeTargetTime = 0f;
        public float currentSpeed;
        public float orbitAngle;
        public Vector3 orbitOffset;
        public float directionChangeTimer;
        public Vector3 randomDirection;
        public float baseSize;
        public float targetSize;
        public float currentSize;
        public float sizeLerpTime;
        
        // 相对坐标相关
        public Vector3 relativePosition;
        public Vector3 relativeTarget;
        public Vector3 relativeVelocity;
    }
    
    void Start()
    {
        mainCamera = Camera.main;
        
        if (globalLight2D == null)
        {
            globalLight2D = FindObjectOfType<Light2D>();
        }
        
        SetNightEnvironment();
        SpawnFireflies();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void SpawnFireflies()
    {
        if (fireflyPrefab == null) return;
        
        for (int i = 0; i < fireflyCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject fireflyObj = Instantiate(fireflyPrefab, spawnPos, Quaternion.identity);
            fireflyObj.transform.SetParent(transform);
            
            float baseSize = randomizeSize ? Random.Range(minSize, maxSize) : (minSize + maxSize) * 0.5f;
            
            Firefly firefly = new Firefly
            {
                transform = fireflyObj.transform,
                light2D = fireflyObj.GetComponentInChildren<Light2D>(),
                spriteRenderer = fireflyObj.GetComponent<SpriteRenderer>(),
                targetPosition = GetRandomPositionInArea(),
                currentSpeed = normalSpeed,
                orbitAngle = Random.Range(0f, 360f),
                directionChangeTimer = Random.Range(0f, directionChangeInterval),
                randomDirection = Random.insideUnitCircle.normalized,
                baseSize = baseSize,
                targetSize = baseSize,
                currentSize = baseSize,
                sizeLerpTime = 0f,
                relativePosition = Vector3.zero,
                relativeTarget = Vector3.zero,
                relativeVelocity = Vector3.zero,
                hasArrivedAtCenter = false
            };
            
            if (enableCenterDot)
            {
                CreateCenterDot(firefly);
            }
            
            ApplyFireflySize(firefly);
            SetupFireflyAppearance(firefly);
            fireflies.Add(firefly);
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
                
                if (!firefly.hasArrivedAtCenter)
                {
                    // 第一阶段：使用预测位置飞向玩家
                    Vector3 playerFuturePosition = GetPlayerFuturePosition(predictionTime);
                    firefly.targetPosition = playerFuturePosition;
                    firefly.currentSpeed = Mathf.Lerp(firefly.currentSpeed, followSpeed, smoothAcceleration * Time.deltaTime);
                    
                    // 检查是否到达玩家中心
                    if (distanceToPlayer <= centerArrivalRadius)
                    {
                        firefly.hasArrivedAtCenter = true;
                        firefly.relativePosition = Vector3.zero;
                        firefly.transform.position = player.position;
                    }
                }
                else
                {
                    // 第二阶段：已到达中心，开始相对坐标移动
                    if (distanceToPlayer <= orbitRadius * 1.2f)
                    {
                        firefly.isOrbiting = true;
                        firefly.currentSpeed = orbitSpeed;
                        firefly.targetSize = firefly.baseSize * 1.2f;
                        
                        if (useRelativeMovement)
                        {
                            UpdateRelativeOrbitTarget(firefly);
                        }
                    }
                    else
                    {
                        firefly.isOrbiting = false;
                        
                        if (useRelativeMovement)
                        {
                            // 设置一个靠近玩家的相对目标
                            Vector2 randomOffset = Random.insideUnitCircle * orbitRadius * 0.8f;
                            firefly.relativeTarget = new Vector3(randomOffset.x, randomOffset.y, 0);
                        }
                        else
                        {
                            Vector3 playerVelocity = GetPlayerVelocity();
                            firefly.targetPosition = player.position + playerVelocity * 0.3f;
                        }
                        
                        float speed = CalculateFollowSpeed(distanceToPlayer, GetPlayerVelocity().magnitude);
                        firefly.currentSpeed = Mathf.Lerp(firefly.currentSpeed, speed, smoothAcceleration * Time.deltaTime);
                        firefly.targetSize = firefly.baseSize;
                    }
                }
            }
            else
            {
                // 离开跟随范围，重置状态
                firefly.isFollowing = false;
                firefly.isOrbiting = false;
                firefly.hasArrivedAtCenter = false;
                firefly.currentSpeed = normalSpeed;
                firefly.targetSize = firefly.baseSize;
            }
        }
        
        if (!firefly.isFollowing)
        {
            firefly.changeTargetTime -= Time.deltaTime;
            if (firefly.changeTargetTime <= 0f)
            {
                firefly.targetPosition = GetRandomPositionInArea();
                firefly.changeTargetTime = Random.Range(2f, 5f);
                
                if (Random.value < 0.3f)
                {
                    firefly.targetSize = Random.Range(minSize, maxSize);
                    firefly.baseSize = firefly.targetSize;
                }
            }
        }
    }
    
    Vector3 GetPlayerFuturePosition(float predictionTime)
    {
        Vector3 playerVelocity = GetPlayerVelocity();
        return player.position + playerVelocity * predictionTime;
    }
    
    void UpdateFireflyMovement(Firefly firefly)
    {
        if (useRelativeMovement && firefly.isFollowing && firefly.hasArrivedAtCenter && player != null)
        {
            // 第二阶段：使用相对坐标移动
            UpdateRelativeMovement(firefly);
        }
        else if (firefly.isFollowing && !firefly.hasArrivedAtCenter && player != null)
        {
            // 第一阶段：飞向玩家中心
            UpdateCenterMovement(firefly);
        }
        else if (firefly.isOrbiting && player != null)
        {
            UpdateOrbitingMovement(firefly);
        }
        else if (firefly.isFollowing && player != null)
        {
            UpdateDirectMovement(firefly);
        }
        else
        {
            UpdateFreeMovement(firefly);
        }
    }
    
    void UpdateCenterMovement(Firefly firefly)
    {
        // 第一阶段：飞向预测的玩家位置
        firefly.transform.position = Vector3.MoveTowards(
            firefly.transform.position,
            firefly.targetPosition,
            firefly.currentSpeed * Time.deltaTime
        );
    }
    
    void UpdateRelativeMovement(Firefly firefly)
    {
        // 根据距离动态调整平滑时间
        float distanceToPlayer = Vector2.Distance(firefly.transform.position, player.position);
        float dynamicSmoothTime = Mathf.Lerp(minSmoothTime, maxSmoothTime, 
            Mathf.Clamp01(distanceToPlayer / followDistance));
        
        float maxSpeed = firefly.isOrbiting ? orbitSpeed : firefly.currentSpeed;
        
        // 使用SmoothDamp实现平滑移动
        firefly.relativePosition = Vector3.SmoothDamp(
            firefly.relativePosition,
            firefly.relativeTarget,
            ref firefly.relativeVelocity,
            dynamicSmoothTime,
            maxSpeed
        );
        
        // 应用相对位置到世界坐标
        firefly.transform.position = player.position + firefly.relativePosition;
        
        // 添加浮动效果（只有在不紧急移动时）
        if (firefly.relativeVelocity.magnitude < 2f)
        {
            float floatOffset = Mathf.Sin(Time.time * 4f + firefly.transform.GetInstanceID()) * 0.1f * Time.deltaTime;
            firefly.transform.position += Vector3.up * floatOffset;
        }
    }
    
    // [其余方法保持不变...]
    
    void UpdateRelativeOrbitTarget(Firefly firefly)
    {
        // 更新围绕角度
        firefly.orbitAngle += relativeOrbitSpeed * Time.deltaTime;
        
        // 基础圆形轨道（相对坐标）
        float baseX = Mathf.Cos(firefly.orbitAngle) * orbitRadius;
        float baseY = Mathf.Sin(firefly.orbitAngle) * orbitRadius;
        Vector3 baseOrbit = new Vector3(baseX, baseY, 0);
        
        // 更新随机移动
        firefly.directionChangeTimer -= Time.deltaTime;
        if (firefly.directionChangeTimer <= 0f)
        {
            firefly.randomDirection = Random.insideUnitCircle.normalized;
            firefly.directionChangeTimer = directionChangeInterval;
        }
        
        // 添加随机偏移
        Vector3 randomOffset = firefly.randomDirection * randomMovementRange;
        
        // 设置相对目标位置
        firefly.relativeTarget = baseOrbit + randomOffset;
    }
     void UpdateOrbitingMovement(Firefly firefly)
    {
        firefly.orbitAngle += orbitSpeed * Time.deltaTime;
        
        float baseX = Mathf.Cos(firefly.orbitAngle) * orbitRadius;
        float baseY = Mathf.Sin(firefly.orbitAngle) * orbitRadius;
        Vector3 baseOrbitPosition = new Vector3(baseX, baseY, 0);
        
        firefly.directionChangeTimer -= Time.deltaTime;
        if (firefly.directionChangeTimer <= 0f)
        {
            firefly.randomDirection = Random.insideUnitCircle.normalized;
            firefly.directionChangeTimer = directionChangeInterval;
        }
        
        Vector3 randomOffset = firefly.randomDirection * randomMovementRange;
        Vector3 targetOrbitPosition = player.position + baseOrbitPosition + randomOffset;
        
        firefly.transform.position = Vector3.MoveTowards(
            firefly.transform.position,
            targetOrbitPosition,
            orbitSpeed * Time.deltaTime
        );
        
        float floatOffset = Mathf.Sin(Time.time * 4f + firefly.transform.GetInstanceID()) * 0.1f * Time.deltaTime;
        firefly.transform.position += Vector3.up * floatOffset;
    }
    
    void UpdateDirectMovement(Firefly firefly)
    {
        Vector3 direction = (firefly.targetPosition - firefly.transform.position).normalized;
        firefly.transform.position = Vector3.MoveTowards(
            firefly.transform.position,
            firefly.targetPosition,
            firefly.currentSpeed * Time.deltaTime
        );
    }
    
    void UpdateFreeMovement(Firefly firefly)
    {
        Vector3 direction = (firefly.targetPosition - firefly.transform.position).normalized;
        firefly.transform.position += direction * firefly.currentSpeed * Time.deltaTime;
        
        float floatOffset = Mathf.Sin(Time.time * 3f + firefly.transform.GetInstanceID()) * fireflyFloatHeight * Time.deltaTime;
        firefly.transform.position += Vector3.up * floatOffset;
    }
    void SetNightEnvironment()
    {
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = nightColor;
        }
        
        if (globalLight2D != null)
        {
            globalLight2D.intensity = globalLightIntensityNight;
            globalLight2D.color = Color.Lerp(Color.white, nightColor, 0.3f);
        }
        
        RenderSettings.ambientLight = nightColor * 0.6f;
    }
    
    void Update()
    {
        UpdateFireflies();
    }
    
    
    
    void CreateCenterDot(Firefly firefly)
    {
        // 创建中心点子对象
        GameObject centerDotObj = new GameObject("CenterDot");
        centerDotObj.transform.SetParent(firefly.transform);
        centerDotObj.transform.localPosition = Vector3.zero;
        centerDotObj.transform.localRotation = Quaternion.identity;
        
        // 添加SpriteRenderer
        SpriteRenderer dotRenderer = centerDotObj.AddComponent<SpriteRenderer>();
        
        // 创建白色圆形纹理
        Texture2D dotTexture = CreateDotTexture(32, centerDotColor);
        Sprite dotSprite = Sprite.Create(dotTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32);
        dotRenderer.sprite = dotSprite;
        
        // 设置渲染顺序（在萤火虫主体之上）
        dotRenderer.sortingOrder = 1;
        
        firefly.centerDotRenderer = dotRenderer;
    }
    
    Texture2D CreateDotTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    // 圆形内部使用指定颜色，不透明度100%
                    pixels[y * size + x] = color;
                }
                else
                {
                    // 圆形外部透明
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    void SetupFireflyAppearance(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            firefly.light2D.color = fireflyColor;
            firefly.light2D.intensity = baseLightIntensity;
            firefly.light2D.pointLightOuterRadius = lightRadius;
            firefly.light2D.falloffIntensity = falloffIntensity;
        }
        
        if (firefly.spriteRenderer != null)
        {
            firefly.spriteRenderer.color = new Color(fireflyColor.r, fireflyColor.g, fireflyColor.b, 0.8f);
        }
        
        // 设置中心点外观
        if (firefly.centerDotRenderer != null)
        {
            firefly.centerDotRenderer.color = centerDotColor;
            firefly.centerDotRenderer.transform.localScale = Vector3.one * centerDotSize;
        }
    }
    
    void ApplyFireflySize(Firefly firefly)
    {
        if (firefly.transform != null)
        {
            firefly.transform.localScale = Vector3.one * firefly.currentSize;
        }
        
        // 根据大小调整光照范围
        if (firefly.light2D != null)
        {
            firefly.light2D.pointLightOuterRadius = lightRadius * (firefly.currentSize / ((minSize + maxSize) * 0.5f));
        }
        
        // 中心点大小保持不变（相对于萤火虫主体）
        if (firefly.centerDotRenderer != null)
        {
            // 中心点的大小是相对于萤火虫主体的，所以不需要额外调整
            // 但我们可以让中心点的大小相对于萤火虫大小保持比例
            float relativeDotSize = centerDotSize / firefly.currentSize;
            firefly.centerDotRenderer.transform.localScale = Vector3.one * relativeDotSize;
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
            UpdateFireflySize(firefly);
        }
    }
    
    
   
    
    void UpdateFireflySize(Firefly firefly)
    {
        firefly.sizeLerpTime += Time.deltaTime * sizeChangeSpeed;
        float smoothSize = Mathf.Lerp(firefly.currentSize, firefly.targetSize, firefly.sizeLerpTime);
        
        float pulse = Mathf.Sin(Time.time * sizePulseFrequency + firefly.transform.GetInstanceID()) * sizePulseAmount * firefly.baseSize;
        
        firefly.currentSize = smoothSize + pulse;
        firefly.currentSize = Mathf.Max(firefly.currentSize, minSize * 0.5f);
        
        ApplyFireflySize(firefly);
        
        if (Mathf.Abs(firefly.currentSize - firefly.targetSize) < 0.01f)
        {
            firefly.sizeLerpTime = 0f;
        }
    }
    
    
    
    

    float CalculateFollowSpeed(float distanceToPlayer, float playerSpeed)
    {
        float baseSpeed = followSpeed;
        float distanceFactor = Mathf.Clamp(distanceToPlayer / followDistance, 1f, 3f);
        float playerSpeedFactor = Mathf.Clamp(playerSpeed / 3f, 1f, 3f);
        float finalSpeed = baseSpeed * distanceFactor * playerSpeedFactor;
        return Mathf.Min(finalSpeed, catchUpSpeed);
    }


    Vector3 GetPlayerVelocity()
    {
        Rigidbody2D playerRb = player?.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            return playerRb.velocity;
        }
        return Vector3.zero;
    }
    
    void UpdateFireflyLight(Firefly firefly)
    {
        if (firefly.light2D != null)
        {
            float intensity = minIntensity + Mathf.PerlinNoise(Time.time * 4f + firefly.transform.GetInstanceID(), 0) * (maxIntensity - minIntensity);
            
            if (firefly.isOrbiting)
            {
                intensity *= orbitingLightBoost;
            }
            
            firefly.light2D.intensity = intensity;
        }
    }
    
}