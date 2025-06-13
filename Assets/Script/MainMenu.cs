using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance;

    private void Awake()
    {
        // 单例模式，确保只有一个 GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else
        {
            Destroy(gameObject); // 防止重复创建
        }
    }

    // 加载主菜单
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0); // 或使用索引 SceneManager.LoadScene(0);
    }

    // 加载说明书
    public void LoadInstructions()
    {
        SceneManager.LoadScene(1); // 或 SceneManager.LoadScene(1);
    }

    // 加载游戏场景
    public void LoadGame()
    {
        SceneManager.LoadScene(2); // 或 SceneManager.LoadScene(2);
    }
public void OnApplicationQuit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
