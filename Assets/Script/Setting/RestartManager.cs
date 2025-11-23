using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartManager : MonoBehaviour
{
    void Update()
    {
        // 按R键重新开始当前关卡
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurrentLevel();
        }
    }
    
    public void RestartCurrentLevel()
    {
        // 获取当前场景的索引并重新加载
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
        
        Debug.Log("重新开始当前关卡: " + SceneManager.GetActiveScene().name);
    }
}