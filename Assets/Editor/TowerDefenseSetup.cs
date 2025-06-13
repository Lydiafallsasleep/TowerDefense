using UnityEngine;
using UnityEditor;

/// <summary>
/// 提供快速设置塔防游戏所需对象的菜单选项
/// </summary>
public class TowerDefenseSetup
{
    [MenuItem("Tools/Tower Defense/Setup Game Managers")]
    public static void SetupGameManagers()
    {
        // 检查并创建GameManager
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gameManager, "Create GameManager");
        }
        
        // 检查并创建TowerManager
        GameObject towerManager = GameObject.Find("TowerManager");
        if (towerManager == null)
        {
            towerManager = new GameObject("TowerManager");
            TowerManager tm = towerManager.AddComponent<TowerManager>();
            
            // 设置默认值
            tm.currentGold = 300;
            Undo.RegisterCreatedObjectUndo(towerManager, "Create TowerManager");
        }
        
        // 检查并创建ObstacleManager
        GameObject obstacleManager = GameObject.Find("ObstacleManager");
        if (obstacleManager == null)
        {
            obstacleManager = new GameObject("ObstacleManager");
            ObstacleManager om = obstacleManager.AddComponent<ObstacleManager>();
            om.searchAllLayers = true;
            Undo.RegisterCreatedObjectUndo(obstacleManager, "Create ObstacleManager");
        }
        
        // 检查并创建TowerInitializer
        GameObject towerInitializer = GameObject.Find("TowerInitializer");
        if (towerInitializer == null)
        {
            towerInitializer = new GameObject("TowerInitializer");
            towerInitializer.AddComponent<TowerInitializer>();
            Undo.RegisterCreatedObjectUndo(towerInitializer, "Create TowerInitializer");
        }
        
        Debug.Log("已创建所有必要的管理器对象");
        EditorUtility.DisplayDialog("设置完成", "已创建/检查所有必要的游戏管理器对象", "确定");
    }

    [MenuItem("Tools/Tower Defense/Fix Tower Resources")]
    public static void FixTowerResources()
    {
        // 确保Resources/tower文件夹存在
        if (!System.IO.Directory.Exists("Assets/Resources/tower"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/tower");
            AssetDatabase.Refresh();
        }
        
        // 创建一个临时的SimpleTowerBuilder来生成预制体
        GameObject tempBuilder = new GameObject("TempBuilder");
        SimpleTowerBuilder builder = tempBuilder.AddComponent<SimpleTowerBuilder>();
        builder.saveToResources = true;
        builder.CreateTowerPrefabs();
        
        // 删除临时对象
        Object.DestroyImmediate(tempBuilder);
        
        // 刷新资源数据库
        AssetDatabase.Refresh();
        
        Debug.Log("已尝试修复塔资源，请检查Resources/tower文件夹");
        EditorUtility.DisplayDialog("资源修复", "已尝试创建塔资源，请检查Resources/tower文件夹", "确定");
    }
} 