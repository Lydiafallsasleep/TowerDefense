using UnityEngine;
using System.Collections.Generic;

public class EnemyDebugger : MonoBehaviour
{
    [Header("调试选项")]
    public bool spawnOnStart = true;
    public bool showPathGizmos = true;
    public bool forceSpawnButton = false;
    public Color landPathColor = Color.green;
    public Color waterPathColor = Color.blue;
    
    [Header("路径设置")]
    [Tooltip("强制重新生成路径，即使已存在路径")]
    public bool forceRegeneratePaths = false;
    
    void Start()
    {
        if (spawnOnStart)
            Invoke("SpawnTestEnemies", 2f);
    }
    
    void Update()
    {
        // 按下空格键手动生成敌人
        if (Input.GetKeyDown(KeyCode.Space) || forceSpawnButton)
        {
            forceSpawnButton = false;
            SpawnTestEnemies();
        }
    }
    
    public void SpawnTestEnemies()
    {
        Debug.Log("正在生成测试敌人...");
        
        // 检查路径是否需要重新生成
        if (PathManager.Instance != null)
        {
            // 应用调试设置
            PathManager.Instance.forceRegeneratePaths = forceRegeneratePaths;
            
            // 检查路径是否已存在
            bool pathsExist = PathManager.Instance.ArePathsGenerated();
            
            if (!pathsExist || forceRegeneratePaths)
            {
                Debug.Log("路径不存在或强制重新生成");
                PathManager.Instance.GeneratePaths();
            }
            else
            {
                Debug.Log("路径已存在，无需重新生成");
            }
        }
        else
        {
            Debug.LogError("找不到PathManager实例");
        }
        
        // 检查路径是否存在
        LogPathStatus("LandPathParent");
        LogPathStatus("WaterPathParent");
        
        // 尝试生成敌人
        if (EnemySpawner.Instance != null)
        {
            Debug.Log("通过EnemySpawner生成敌人");
            // 生成一个史莱姆
            EnemySpawner.Instance.SpawnEnemy(EnemyMovement.MonsterType.Slime);
            // 生成一个鱼
            EnemySpawner.Instance.SpawnEnemy(EnemyMovement.MonsterType.Fish);
        }
        else
        {
            Debug.Log("EnemySpawner实例不存在，无法生成敌人");
        }
        
        // 检查场景中活跃的敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"场景中有{enemies.Length}个带有Enemy标签的物体");
        
        // 查找所有有EnemyMovement组件的对象
        EnemyMovement[] movements = FindObjectsOfType<EnemyMovement>();
        Debug.Log($"场景中有{movements.Length}个带有EnemyMovement组件的物体");
        
        // 输出每个敌人的位置信息
        foreach (EnemyMovement movement in movements)
        {
            Debug.Log($"敌人: {movement.gameObject.name}, 位置: {movement.transform.position}, 激活状态: {movement.gameObject.activeSelf}");
        }
    }
    
    void LogPathStatus(string pathName)
    {
        GameObject pathObj = GameObject.Find(pathName);
        if (pathObj != null)
        {
            Debug.Log($"找到路径: {pathName}, 子对象数量: {pathObj.transform.childCount}");
            
            // 输出路径点位置
            for (int i = 0; i < pathObj.transform.childCount; i++)
            {
                Transform waypoint = pathObj.transform.GetChild(i);
                Debug.Log($"  - 路径点 {i}: {waypoint.position}");
            }
        }
        else
        {
            Debug.LogError($"找不到路径: {pathName}");
        }
    }
    
    // 可视化路径点
    void OnDrawGizmos()
    {
        if (!showPathGizmos) return;
        
        DrawPath("LandPathParent", landPathColor);
        DrawPath("WaterPathParent", waterPathColor);
    }
    
    void DrawPath(string pathName, Color color)
    {
        GameObject pathObj = GameObject.Find(pathName);
        if (pathObj == null || pathObj.transform.childCount == 0) return;
        
        Gizmos.color = color;
        
        // 绘制路径点
        for (int i = 0; i < pathObj.transform.childCount; i++)
        {
            Transform point = pathObj.transform.GetChild(i);
            Gizmos.DrawSphere(point.position, 0.3f);
            
            // 绘制路径线
            if (i < pathObj.transform.childCount - 1)
            {
                Transform nextPoint = pathObj.transform.GetChild(i + 1);
                Gizmos.DrawLine(point.position, nextPoint.position);
            }
        }
    }
} 