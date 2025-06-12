using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单控制器，管理主菜单界面的功能
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI元素")]
    public Button startGameButton; // 开始游戏按钮
    public Button settingsButton; // 设置按钮
    public Button quitButton; // 退出游戏按钮
    public TextMeshProUGUI highScoreText; // 最高分显示
    public GameObject settingsPanel; // 设置面板
    
    [Header("游戏场景")]
    public string gameSceneName = "GameScene"; // 游戏场景名称
    
    [Header("音效")]
    public AudioClip buttonClickSound; // 按钮点击音效
    public AudioClip backgroundMusic; // 背景音乐
    
    [Header("存档设置")]
    public string highScoreKey = "HighScore"; // 最高分存档键名
    
    private AudioSource audioSource;
    
    void Start()
    {
        // 获取音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 播放背景音乐
        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // 设置按钮点击事件
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettings);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // 隐藏设置面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // 显示最高分
        UpdateHighScoreDisplay();
    }
    
    /// <summary>
    /// 更新最高分显示
    /// </summary>
    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt(highScoreKey, 0);
            highScoreText.text = $"最高分数: {highScore}";
        }
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        PlayButtonSound();
        
        try
        {
            SceneManager.LoadScene(gameSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainMenuController] 无法加载游戏场景: {e.Message}");
            Debug.LogWarning("[MainMenuController] 请确保游戏场景已添加到构建设置中，或者修改场景名称");
            
            // 尝试加载索引为1的场景（通常是游戏场景）
            try
            {
                SceneManager.LoadScene(1);
            }
            catch
            {
                Debug.LogError("[MainMenuController] 无法加载索引为1的场景，请检查构建设置");
            }
        }
    }
    
    /// <summary>
    /// 切换设置面板
    /// </summary>
    public void ToggleSettings()
    {
        PlayButtonSound();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 重置最高分
    /// </summary>
    public void ResetHighScore()
    {
        PlayButtonSound();
        
        PlayerPrefs.SetInt(highScoreKey, 0);
        PlayerPrefs.Save();
        
        UpdateHighScoreDisplay();
        
        Debug.Log("[MainMenuController] 最高分已重置");
    }
    
    /// <summary>
    /// 播放按钮音效
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
} 