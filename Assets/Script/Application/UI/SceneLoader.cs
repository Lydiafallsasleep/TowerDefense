using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles scene transitions with optional loading screen
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider progressBar;
    public TMPro.TextMeshProUGUI progressText;
    
    [Header("Transition Settings")]
    public float minimumLoadingTime = 0.5f; // 最小加载时间，防止加载太快闪屏
    public bool useLoadingScreen = true;
    public string defaultNextScene = ""; // 默认的下一个场景名称
    
    /// <summary>
    /// 加载下一个场景（基于当前场景的索引+1）
    /// </summary>
    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        
        // 检查是否存在下一个场景
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadSceneByIndex(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] 没有下一个场景，返回到第一个场景");
            LoadSceneByIndex(0); // 回到第一个场景
        }
    }
    
    /// <summary>
    /// 根据索引加载场景
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (useLoadingScreen && loadingScreen != null)
        {
            StartCoroutine(LoadSceneAsyncWithProgress(sceneIndex));
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }
    
    /// <summary>
    /// 根据名称加载场景
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (useLoadingScreen && loadingScreen != null)
        {
            StartCoroutine(LoadSceneAsyncWithProgress(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        LoadSceneByIndex(currentSceneIndex);
    }
    
    /// <summary>
    /// 加载默认的下一个场景（如果已设置）
    /// </summary>
    public void LoadDefaultNextScene()
    {
        if (!string.IsNullOrEmpty(defaultNextScene))
        {
            LoadSceneByName(defaultNextScene);
        }
        else
        {
            LoadNextScene();
        }
    }
    
    /// <summary>
    /// 带进度条的异步加载场景（根据索引）
    /// </summary>
    private IEnumerator LoadSceneAsyncWithProgress(int sceneIndex)
    {
        // 显示加载屏幕
        loadingScreen.SetActive(true);
        
        // 开始加载时间
        float startTime = Time.time;
        
        // 开始异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false; // 暂时不允许场景激活
        
        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            // 进度在0-0.9之间，因为异步加载最多到0.9
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // 更新进度条
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            
            // 更新进度文本
            if (progressText != null)
            {
                progressText.text = $"加载中... {Mathf.Floor(progress * 100)}%";
            }
            
            // 检查是否达到最小加载时间
            float elapsedTime = Time.time - startTime;
            bool timeConditionMet = (elapsedTime >= minimumLoadingTime);
            
            // 如果进度达到100%且满足最小时间条件
            if (asyncLoad.progress >= 0.9f && timeConditionMet)
            {
                asyncLoad.allowSceneActivation = true; // 允许场景激活
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 带进度条的异步加载场景（根据名称）
    /// </summary>
    private IEnumerator LoadSceneAsyncWithProgress(string sceneName)
    {
        // 显示加载屏幕
        loadingScreen.SetActive(true);
        
        // 开始加载时间
        float startTime = Time.time;
        
        // 开始异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // 暂时不允许场景激活
        
        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            // 进度在0-0.9之间，因为异步加载最多到0.9
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // 更新进度条
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            
            // 更新进度文本
            if (progressText != null)
            {
                progressText.text = $"加载中... {Mathf.Floor(progress * 100)}%";
            }
            
            // 检查是否达到最小加载时间
            float elapsedTime = Time.time - startTime;
            bool timeConditionMet = (elapsedTime >= minimumLoadingTime);
            
            // 如果进度达到100%且满足最小时间条件
            if (asyncLoad.progress >= 0.9f && timeConditionMet)
            {
                asyncLoad.allowSceneActivation = true; // 允许场景激活
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneLoader] 退出游戏");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
} 