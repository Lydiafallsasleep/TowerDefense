using UnityEngine;

/// <summary>
/// 确保TowerPlacementManager在场景中被正确初始化
/// </summary>
public class TowerManagerInitializer : MonoBehaviour
{
    // 在Awake中初始化，确保在其他组件之前执行
    void Awake()
    {
        Debug.Log("TowerManagerInitializer: 开始初始化...");
        
        // 检查是否已存在TowerPlacementManager
        TowerPlacementManager placementManager = FindObjectOfType<TowerPlacementManager>();
        
        if (placementManager == null)
        {
            // 如果不存在，创建一个
            GameObject managerObj = new GameObject("TowerPlacementManager");
            placementManager = managerObj.AddComponent<TowerPlacementManager>();
            
            // 确保不会被销毁
            DontDestroyOnLoad(managerObj);
            
            Debug.Log("TowerManagerInitializer: 已创建TowerPlacementManager");
        }
        else
        {
            Debug.Log("TowerManagerInitializer: 已找到现有TowerPlacementManager");
        }
        
        // 确保TowerManager使用预设放置点
        TowerManager towerManager = FindObjectOfType<TowerManager>();
        if (towerManager != null)
        {
            towerManager.usePresetPlacementPoints = true;
            
            // 如果没有放置点，自动创建一些测试放置点
            if (placementManager.placementPoints == null || placementManager.placementPoints.Count == 0)
            {
                Debug.Log("TowerManagerInitializer: 未找到预设放置点，创建测试放置点");
                CreateTestPlacementPoints(placementManager);
            }
        }
    }
    
    // 创建一些测试放置点
    private void CreateTestPlacementPoints(TowerPlacementManager manager)
    {
        // 创建一个3x3的放置点网格
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3 worldPos = new Vector3(x * 2, y * 2, 0);
                Vector3Int gridPos = new Vector3Int(x, y, 0);
                string pointID = $"Point_{x}_{y}";
                
                manager.CreatePlacementPoint(worldPos, gridPos, pointID);
            }
        }
        
        Debug.Log($"TowerManagerInitializer: 已创建9个测试放置点");
    }
} 