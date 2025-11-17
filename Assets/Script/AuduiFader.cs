using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAudioManager : MonoBehaviour
{
    public static SceneAudioManager Instance;
    
    public AudioSource audioSource;
    public AudioClip[] backgroundMusics; // 不同场景的背景音乐
    public float fadeDuration = 1.5f;
    
    private void Awake()
    {
        // 单例模式，确保只有一个音频管理器
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0f;
        
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成: {scene.name}");
        
        // 根据场景索引或名称选择音乐
        AudioClip sceneMusic = GetMusicForScene(scene);
        
        if (sceneMusic != null)
        {
            // 淡入新场景的音乐
            StartCoroutine(CrossFadeMusic(sceneMusic));
        }
        else
        {
            // 如果没有设置该场景的音乐，淡出当前音乐
            StartCoroutine(FadeOutMusic());
        }
    }
    
    private AudioClip GetMusicForScene(Scene scene)
    {
        // 方法1：按场景索引获取音乐
        if (scene.buildIndex < backgroundMusics.Length)
        {
            return backgroundMusics[scene.buildIndex];
        }
        
        // 方法2：按场景名称获取音乐（需要在Inspector中手动映射）
        // 这里简单返回第一个音乐，你可以根据需求修改
        
        return backgroundMusics.Length > 0 ? backgroundMusics[0] : null;
    }
    
    // 手动切换场景时调用（在按钮点击事件中）
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(LoadSceneWithAudioFade(sceneName));
    }
    
    public void LoadSceneWithFade(int sceneIndex)
    {
        StartCoroutine(LoadSceneWithAudioFade(sceneIndex));
    }
    
    private IEnumerator LoadSceneWithAudioFade(object sceneIdentifier)
    {
        Debug.Log("开始场景切换流程");
        
        // 第一步：淡出当前音乐
        yield return StartCoroutine(FadeOutMusic());
        
        // 第二步：加载新场景
        if (sceneIdentifier is string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
        else if (sceneIdentifier is int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        
        // 注意：新场景加载后，OnSceneLoaded会自动处理淡入新音乐
    }
    
    private IEnumerator CrossFadeMusic(AudioClip newClip)
    {
        // 如果当前正在播放相同的音乐，不需要切换
        if (audioSource.clip == newClip && audioSource.isPlaying)
        {
            Debug.Log("已经是相同的背景音乐，无需切换");
            yield break;
        }
        
        // 淡出现有音乐
        if (audioSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutMusic());
        }
        
        // 设置新音乐并淡入
        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.Play();
        
        yield return StartCoroutine(FadeInMusic());
    }
    
    private IEnumerator FadeInMusic()
    {
        Debug.Log("开始淡入音乐");
        
        float currentTime = 0f;
        float startVolume = audioSource.volume;
        float targetVolume = 0.8f; // 目标音量80%
        
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeDuration);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
        Debug.Log("音乐淡入完成");
    }
    
    private IEnumerator FadeOutMusic()
    {
        if (!audioSource.isPlaying)
            yield break;
            
        Debug.Log("开始淡出音乐");
        
        float currentTime = 0f;
        float startVolume = audioSource.volume;
        
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
        Debug.Log("音乐淡出完成");
    }
}