using UnityEngine;

// 添加到场景中任意物体上，用于测试敌人生成
public class EnemySpawnTester : MonoBehaviour
{
    [Header("测试设置")]
    public int numberOfEnemies = 5;
    public KeyCode spawnKey = KeyCode.Space;
    
    void Update()
    {
        // 按下指定按键生成多个敌人
        if (Input.GetKeyDown(spawnKey))
        {
            if (EnemySpawner.Instance != null)
            {
                Debug.Log($"手动触发生成 {numberOfEnemies} 个敌人");
                EnemySpawner.Instance.SpawnMultipleEnemies(numberOfEnemies);
            }
            else
            {
                Debug.LogError("无法找到EnemySpawner实例！");
            }
        }
    }
} 