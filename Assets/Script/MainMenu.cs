using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance;

    private void Awake()
    {
        // ����ģʽ��ȷ��ֻ��һ�� GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �л�����ʱ������
        }
        else
        {
            Destroy(gameObject); // ��ֹ�ظ�����
        }
    }

    // �������˵�
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0); // ��ʹ������ SceneManager.LoadScene(0);
    }

    // ����˵����
    public void LoadInstructions()
    {
        SceneManager.LoadScene(1); // �� SceneManager.LoadScene(1);
    }

    // ������Ϸ����
    public void LoadGame()
    {
        SceneManager.LoadScene(2); // �� SceneManager.LoadScene(2);
    }
public void OnApplicationQuit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
