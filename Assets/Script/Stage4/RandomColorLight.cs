using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
public class SuperBrightLight2D : MonoBehaviour
{
    [Header("基础设置")]
    public Light2D targetLight2D;
    public float activationDistance = 5f;
    
    [Header("超级亮度设置")]
    [Tooltip("普通强度调整")]
    [Range(0.1f, 10f)]
    public float lightIntensity = 5f;
    
    [Tooltip("如果普通强度不够，使用这个超级强度")]
    [Range(1f, 100f)]
    public float superIntensity = 50f;
    
    [Tooltip("是否使用超级强度模式")]
    public bool useSuperIntensity = true;
    
    [Header("灯光大小和范围")]
    [Range(0.1f, 20f)]
    public float lightRadius = 5f;
    
    [Header("灯光颜色")]
    public Color lightColor = Color.white;
    
    [Header("多层灯光解决方案")]
    public bool useMultipleLights = true;
    public int additionalLightsCount = 2;
    
    [Header("呼吸效果")]
    public bool useBreathingEffect = true;
    [Range(0.1f, 5f)]
    public float minIntensity = 1f;
    [Range(1f, 10f)]
    public float maxIntensity = 3f;
    [Range(0.1f, 3f)]
    public float breathingSpeed = 1f;

    private bool isPlayerNear = false;
    private Transform playerTransform;
    private Light2D[] additionalLights;

    void Start()
    {
        InitializeMainLight();
        CreateAdditionalLights();
        FindPlayer();
    }

    void InitializeMainLight()
    {
        if (targetLight2D == null)
        {
            targetLight2D = GetComponent<Light2D>();
        }
        
        if (targetLight2D == null)
        {
            Debug.LogError("未找到Light2D组件！");
            return;
        }

        // 配置主灯光
        targetLight2D.color = lightColor;
        targetLight2D.intensity = 0f;
        targetLight2D.enabled = true;
        
        // 设置点光源半径
        if (targetLight2D.lightType == Light2D.LightType.Point)
        {
            targetLight2D.pointLightOuterRadius = lightRadius;
        }
        
        Debug.Log("主灯光初始化完成");
    }

    void CreateAdditionalLights()
    {
        if (!useMultipleLights) return;
        
        additionalLights = new Light2D[additionalLightsCount];
        
        for (int i = 0; i < additionalLightsCount; i++)
        {
            GameObject lightObj = new GameObject($"AdditionalLight_{i}");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            
            Light2D newLight = lightObj.AddComponent<Light2D>();
            newLight.lightType = targetLight2D.lightType;
            newLight.color = lightColor;
            
            // 稍微不同的颜色变化，增加层次感
            Color variedColor = lightColor;
            if (i == 1) variedColor *= 1.2f;
            if (i == 2) variedColor *= 0.8f;
            newLight.color = variedColor;
            
            if (newLight.lightType == Light2D.LightType.Point)
            {
                newLight.pointLightOuterRadius = lightRadius * (1f + i * 0.2f);
            }
            
            newLight.intensity = 0f;
            newLight.enabled = true;
            
            additionalLights[i] = newLight;
        }
        
        Debug.Log($"创建了 {additionalLightsCount} 个附加灯光");
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool wasPlayerNear = isPlayerNear;
        isPlayerNear = distance <= activationDistance;

        if (isPlayerNear && !wasPlayerNear)
        {
            OnPlayerEnter();
        }
        else if (!isPlayerNear && wasPlayerNear)
        {
            OnPlayerExit();
        }

        if (isPlayerNear)
        {
            UpdateLightEffect();
        }
    }

    void OnPlayerEnter()
    {
        Debug.Log("激活超级灯光！");
        float intensity = useSuperIntensity ? superIntensity : lightIntensity;
        
        targetLight2D.intensity = intensity;
        
        // 激活所有附加灯光
        if (useMultipleLights && additionalLights != null)
        {
            foreach (var light in additionalLights)
            {
                if (light != null)
                    light.intensity = intensity * 0.6f; // 附加灯光稍暗一些
            }
        }
    }

    void OnPlayerExit()
    {
        targetLight2D.intensity = 0f;
        
        if (useMultipleLights && additionalLights != null)
        {
            foreach (var light in additionalLights)
            {
                if (light != null)
                    light.intensity = 0f;
            }
        }
    }

    void UpdateLightEffect()
    {
        if (!useBreathingEffect) return;

        float pingPong = Mathf.PingPong(Time.time * breathingSpeed, 1f);
        float baseIntensity = useSuperIntensity ? superIntensity : lightIntensity;
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, pingPong) * baseIntensity;

        targetLight2D.intensity = currentIntensity;
        
        // 更新附加灯光的呼吸效果
        if (useMultipleLights && additionalLights != null)
        {
            for (int i = 0; i < additionalLights.Length; i++)
            {
                if (additionalLights[i] != null)
                {
                    // 每个附加灯光有轻微的相位偏移
                    float phaseOffset = i * 0.3f;
                    float individualPingPong = Mathf.PingPong(Time.time * breathingSpeed + phaseOffset, 1f);
                    float individualIntensity = Mathf.Lerp(minIntensity * 0.8f, maxIntensity * 0.8f, individualPingPong) * baseIntensity * 0.6f;
                    additionalLights[i].intensity = individualIntensity;
                }
            }
        }
    }

    // 实时应用设置更改
    void OnValidate()
    {
        if (targetLight2D != null)
        {
            targetLight2D.color = lightColor;
            
            if (targetLight2D.lightType == Light2D.LightType.Point)
            {
                targetLight2D.pointLightOuterRadius = lightRadius;
            }
            
            // 更新附加灯光
            if (useMultipleLights && additionalLights != null)
            {
                for (int i = 0; i < additionalLights.Length; i++)
                {
                    if (additionalLights[i] != null)
                    {
                        additionalLights[i].color = lightColor;
                        if (additionalLights[i].lightType == Light2D.LightType.Point)
                        {
                            additionalLights[i].pointLightOuterRadius = lightRadius * (1f + i * 0.2f);
                        }
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // 触发范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
        
        // 灯光范围
        if (targetLight2D != null && targetLight2D.lightType == Light2D.LightType.Point)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, lightRadius);
        }
    }
}