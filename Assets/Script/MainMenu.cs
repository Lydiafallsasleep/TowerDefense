using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    public string startScene;
    public void StartGame()
    {
        SceneManager.LoadScene(startScene);
    }
    private void OnApplicationQuit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
