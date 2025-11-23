using UnityEngine;
using UnityEngine.UI;

public class BGMFadeOutButton : MonoBehaviour
{
    [Header("BGM淡出设置")]
    [Tooltip("触发淡出的按钮")]
    public Button targetButton;
    
    [Tooltip("淡出时间（秒），使用0则使用默认时间")]
    public float fadeOutDuration = 0f;
    
    [Tooltip("是否在淡出后禁用按钮")]
    public bool disableButtonAfterClick = true;
    
    [Header("特定场景设置")]
    [Tooltip("只在特定场景生效，如果为空则在所有场景生效")]
    public string specificSceneName = "";

    private void Start()
    {
        // 如果没有指定按钮，尝试获取当前对象的Button组件
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
        
        if (targetButton != null)
        {
            targetButton.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError("BGMFadeOutButton: 没有找到按钮组件！", this);
        }
    }

    private void OnButtonClicked()
    {
        // 检查是否在特定场景中（如果设置了特定场景）
        if (!string.IsNullOrEmpty(specificSceneName))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != specificSceneName)
            {
                Debug.LogWarning($"BGMFadeOutButton: 当前场景 {currentScene} 不是目标场景 {specificSceneName}，不触发淡出");
                return;
            }
        }
        
        // 触发BGM淡出
        if (BGMManager.Instance != null)
        {
            if (fadeOutDuration > 0f)
            {
                BGMManager.Instance.TriggerFadeOut(fadeOutDuration);
            }
            else
            {
                BGMManager.Instance.TriggerFadeOut();
            }
            
            Debug.Log($"按钮 {gameObject.name} 触发了BGM淡出");
            
            // 可选：禁用按钮防止重复点击
            if (disableButtonAfterClick && targetButton != null)
            {
                targetButton.interactable = false;
            }
        }
        else
        {
            Debug.LogError("BGMFadeOutButton: 找不到BGMManager实例！");
        }
    }

    // 公共方法，供其他脚本调用
    public void TriggerFadeOutManually()
    {
        OnButtonClicked();
    }
}