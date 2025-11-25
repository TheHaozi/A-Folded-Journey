using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlayerDeathManager : MonoBehaviour
{
    [Header("死亡设置")]
    [SerializeField] private int maxCollisions = 3;
    [SerializeField] private float fadeDuration = 2f;
    
    [Header("UI引用")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private string livesFormat = "生命: {0}";
    
    private int collisionCount = 0;
    private bool isDead = false;
    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;
    
    void Start()
    {
        // 创建用于渐出的纹理
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();
        
        // 初始化生命值显示
        UpdateLivesDisplay();
    }
    
    void OnGUI()
    {
        // 使用GUI实现渐出效果，节省资源
        if (isFading && fadeAlpha > 0)
        {
            GUI.color = new Color(0, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || collision.collider.isTrigger)
            return;
            
        ProcessCollision();
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || other.isTrigger)
            return;
            
        ProcessCollision();
    }
    
    private void ProcessCollision()
    {
        collisionCount++;
        UpdateLivesDisplay();
        
        Debug.Log($"碰撞次数: {collisionCount}/{maxCollisions}");
        
        if (collisionCount >= maxCollisions)
        {
            StartCoroutine(DieAndRestart());
        }
    }
    
    private void UpdateLivesDisplay()
    {
        if (livesText != null)
        {
            int remainingLives = Mathf.Max(0, maxCollisions - collisionCount);
            livesText.text = string.Format(livesFormat, remainingLives);
        }
    }
    
    private IEnumerator DieAndRestart()
    {
        isDead = true;
        isFading = true;
        
        Debug.Log("玩家死亡，开始渐出效果");
        
        // 禁用玩家控制 - 通用方法
        DisablePlayerControls();
        
        // 屏幕渐出效果
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeAlpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;
        
        // 重新加载当前场景
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
    
    // 通用方法禁用玩家控制
    private void DisablePlayerControls()
    {
        // 禁用Rigidbody物理运动
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        
        // 禁用可能存在的移动脚本
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // 保留这个脚本和Transform
            if (script != this && script.GetType() != typeof(Transform))
            {
                script.enabled = false;
            }
        }
        
        // 如果有Animator，停止动画
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    
    // 公共方法用于手动重置碰撞计数
    public void ResetCollisionCount()
    {
        collisionCount = 0;
        isDead = false;
        isFading = false;
        fadeAlpha = 0f;
        UpdateLivesDisplay();
        
        // 重新启用组件
        ReenablePlayerControls();
    }
    
    private void ReenablePlayerControls()
    {
        // 重新启用Rigidbody
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // 重新启用所有脚本
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
        }
        
        // 重新启用Animator
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }
    }
    
    // 增加生命值（例如吃到道具）
    public void AddLife(int amount = 1)
    {
        collisionCount = Mathf.Max(0, collisionCount - amount);
        UpdateLivesDisplay();
    }
    
    // 设置生命值
    public void SetLives(int lives)
    {
        collisionCount = Mathf.Max(0, maxCollisions - lives);
        UpdateLivesDisplay();
    }
    
    // 获取当前剩余生命值
    public int GetRemainingLives()
    {
        return Mathf.Max(0, maxCollisions - collisionCount);
    }
    
    // 获取碰撞次数
    public int GetCollisionCount()
    {
        return collisionCount;
    }
    
    void OnDestroy()
    {
        // 清理纹理
        if (fadeTexture != null)
        {
            Destroy(fadeTexture);
        }
    }
}