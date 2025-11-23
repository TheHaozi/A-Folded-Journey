using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BGMManager : MonoBehaviour
{
    [System.Serializable]
    public class BGMSceneGroup
    {
        public string groupName;
        public List<string> sceneNames; // 连续场景的名称列表
        public AudioClip bgmClip;       // 该场景组对应的BGM
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    public static BGMManager Instance { get; private set; }

    [Header("BGM Settings")]
    public List<BGMSceneGroup> sceneGroups;
    public float fadeInDuration = 2f;
    public float fadeOutDuration = 2f;

    private AudioSource audioSource;
    private string currentGroupName = "";
    private Coroutine fadeCoroutine;
    private bool isFadingOut = false;

    private void Awake()
    {
        // 单例模式，确保只有一个BGM管理器
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 创建AudioSource组件
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentSceneName = scene.name;
        string targetGroupName = GetSceneGroup(currentSceneName);

        Debug.Log($"场景加载: {currentSceneName}, 目标组: {targetGroupName}, 当前组: {currentGroupName}");

        // 如果当前场景不在任何组中
        if (targetGroupName == null)
        {
            // 离开BGM场景组，淡出BGM
            if (!string.IsNullOrEmpty(currentGroupName) && !isFadingOut)
            {
                FadeOutBGM();
                currentGroupName = "";
            }
        }
        else if (targetGroupName != currentGroupName)
        {
            // 进入新的BGM场景组，播放对应的BGM
            currentGroupName = targetGroupName;
            isFadingOut = false; // 重置淡出状态
            BGMSceneGroup targetGroup = GetSceneGroupByName(targetGroupName);
            if (targetGroup != null)
            {
                PlayBGM(targetGroup);
            }
        }
        // 如果还在同一个组内，保持BGM播放不变
    }

    private string GetSceneGroup(string sceneName)
    {
        foreach (var group in sceneGroups)
        {
            if (group.sceneNames.Contains(sceneName))
            {
                return group.groupName;
            }
        }
        return null;
    }

    private BGMSceneGroup GetSceneGroupByName(string groupName)
    {
        foreach (var group in sceneGroups)
        {
            if (group.groupName == groupName)
            {
                return group;
            }
        }
        return null;
    }

    private void PlayBGM(BGMSceneGroup group)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 如果BGM不同，切换BGM
        if (audioSource.clip != group.bgmClip)
        {
            audioSource.clip = group.bgmClip;
            audioSource.Play();
        }

        fadeCoroutine = StartCoroutine(FadeInBGM(group.volume));
    }

    private void FadeOutBGM()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        isFadingOut = true;
        fadeCoroutine = StartCoroutine(FadeOutBGMCoroutine());
    }

    private IEnumerator FadeInBGM(float targetVolume)
    {
        float currentTime = 0f;
        float startVolume = audioSource.volume;

        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeInDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOutBGMCoroutine()
    {
        float currentTime = 0f;
        float startVolume = audioSource.volume;

        while (currentTime < fadeOutDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeOutDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop(); // 关键修复：淡出完成后停止播放
        isFadingOut = false;
        
        Debug.Log("BGM已完全停止");
    }

    // ========== 新增的公共方法 ==========
    
    /// <summary>
    /// 手动触发BGM淡出（用于特定按钮）
    /// </summary>
    public void TriggerFadeOut()
    {
        if (!isFadingOut && audioSource.isPlaying)
        {
            FadeOutBGM();
            currentGroupName = "";
            Debug.Log("手动触发BGM淡出");
        }
    }

    /// <summary>
    /// 带自定义淡出时间的淡出
    /// </summary>
    public void TriggerFadeOut(float customFadeOutDuration)
    {
        if (!isFadingOut && audioSource.isPlaying)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            isFadingOut = true;
            StartCoroutine(FadeOutWithCustomDuration(customFadeOutDuration));
            currentGroupName = "";
        }
    }

    private IEnumerator FadeOutWithCustomDuration(float duration)
    {
        float currentTime = 0f;
        float startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        isFadingOut = false;
        
        Debug.Log($"BGM已完全停止（自定义淡出时间: {duration}秒）");
    }

    // 公共方法，用于手动控制（可选）
    public void StopBGM()
    {
        FadeOutBGM();
        currentGroupName = "";
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying && audioSource.volume > 0.01f;
    }

    public string GetCurrentBGMGroup()
    {
        return currentGroupName;
    }
}