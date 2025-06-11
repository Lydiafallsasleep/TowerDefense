using UnityEngine;

/// <summary>
/// 确保塔防御塔预制体在游戏启动时被创建
/// </summary>
public class TowerInitializer : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("TowerInitializer: 检查是否需要初始化塔预制体");
        
        // 尝试加载一个塔预制体，如果不存在则创建
        GameObject testTower = Resources.Load<GameObject>("tower/CannonTower");
        
        if (testTower == null)
        {
            Debug.Log("TowerInitializer: 未找到塔预制体，创建SimpleTowerBuilder");
            
            // 创建SimpleTowerBuilder实例来生成预制体
            GameObject builderObj = new GameObject("SimpleTowerBuilder");
            SimpleTowerBuilder builder = builderObj.AddComponent<SimpleTowerBuilder>();
            builder.saveToResources = true;
            
            // 确保在所有初始化完成后再创建预制体
            builder.Invoke("CreateTowerPrefabs", 0.5f);
        }
        else
        {
            Debug.Log("TowerInitializer: 已找到塔预制体，无需初始化");
        }
    }
} 