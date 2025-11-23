using UnityEngine;

public class SimpleRipple : MonoBehaviour
{
    [Header("基础设置")]
    public float startSize = 0.5f;
    public float endSize = 3f;
    public float lifetime = 1.5f;
    
    [Header("颜色设置")]
    public Color rippleColor = new Color(0.2f, 0.5f, 1f, 1f);
    public bool useRandomColor = false;
    public Color[] randomColorPalette = new Color[] {
        new Color(0.2f, 0.5f, 1f, 1f), // 蓝色
        new Color(0.3f, 0.8f, 0.3f, 1f), // 绿色
        new Color(1f, 0.5f, 0.2f, 1f), // 橙色
        new Color(0.8f, 0.3f, 0.8f, 1f), // 紫色
        new Color(1f, 0.9f, 0.3f, 1f)  // 黄色
    };
    
    // 私有变量
    private SpriteRenderer spriteRenderer;
    private float currentLifetime;
    private float currentSize;
    private Color finalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 设置颜色
        if (useRandomColor && randomColorPalette.Length > 0)
        {
            finalColor = randomColorPalette[Random.Range(0, randomColorPalette.Length)];
        }
        else
        {
            finalColor = rippleColor;
        }
        
        spriteRenderer.color = finalColor;
        spriteRenderer.sortingOrder = 1;
        
        transform.localScale = Vector3.one * startSize;
        currentSize = startSize;
        currentLifetime = lifetime;
        
        Debug.Log($"🌀 涟漪创建 - 颜色: {finalColor}");
    }

    void Update()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        UpdateRippleAnimation();
    }

    void UpdateRippleAnimation()
    {
        float progress = 1f - (currentLifetime / lifetime);
        currentSize = Mathf.Lerp(startSize, endSize, progress);
        transform.localScale = Vector3.one * currentSize;
        
        float alpha = Mathf.Lerp(1f, 0f, progress);
        Color color = finalColor;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    // 公共方法：动态改变颜色
    public void ChangeColor(Color newColor)
    {
        finalColor = newColor;
        spriteRenderer.color = new Color(finalColor.r, finalColor.g, finalColor.b, spriteRenderer.color.a);
    }
}