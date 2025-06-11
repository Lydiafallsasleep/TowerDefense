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
    
    [Header("连续生成")]
    public bool enableContinuousSpawning = true;
    [Tooltip("连续生成启用后的敌人生成间隔(秒)")]
    public float spawnInterval = 2f;
    
    [Header("路径设置")]
    [Tooltip("是否在每次测试时生成新路径")]
    public bool createNewPathsOnTest = false;
    [Tooltip("陆地路径名称")]
    public string landPathName = "LandPathParent";
    [Tooltip("水路径名称")]
    public string waterPathName = "WaterPathParent";
    
    void Start()
    {
        if (spawnOnStart)
            Invoke("SpawnTestEnemies", 2f);
            
        // 启动连续生成检查
        InvokeRepeating("EnsureContinuousSpawning", 5f, 3f);
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
        
        // 如果需要，创建新路径
        if (createNewPathsOnTest)
        {
            CreateDefaultPath(landPathName, true);
            CreateDefaultPath(waterPathName, false);
        }
        
        // 检查路径是否存在
        LogPathStatus(landPathName);
        LogPathStatus(waterPathName);
        
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
    
    // 创建一个默认路径
    void CreateDefaultPath(string pathName, bool isLandPath)
    {
        GameObject pathParent = GameObject.Find(pathName);
        if (pathParent == null)
        {
            pathParent = new GameObject(pathName);
        }
        
        // 清除现有子对象
        foreach (Transform child in pathParent.transform)
        {
            Destroy(child.gameObject);
        }
        
        // 创建路径点
        Vector3[] points;
        float centerX = 0f;
        float centerY = 0f;
        
        if (isLandPath)
        {
            // 创建Z字形陆地路径
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0)
            };
        }
        else
        {
            // 创建环形水路径
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY - 5f, 0)
            };
        }
        
        // 创建路径点
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParent.transform);
            Debug.Log($"创建默认路径点 {i} 在位置: {points[i]}");
        }
        
        Debug.Log($"默认{(isLandPath ? "陆地" : "水路")}路径创建完成，路径点数量: {pathParent.transform.childCount}");
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
        
        DrawPath(landPathName, landPathColor);
        DrawPath(waterPathName, waterPathColor);
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
    
    // 确保连续生成功能正常工作
    void EnsureContinuousSpawning()
    {
        if (!enableContinuousSpawning) return;
        
        if (EnemySpawner.Instance != null)
        {
            // 强制设置连续生成参数
            EnemySpawner.Instance.autoSpawn = true;
            EnemySpawner.Instance.spawnInterval = spawnInterval;
            
            // 确保初始化和路径可用标志正确设置
            var spawnerField = EnemySpawner.Instance.GetType().GetField("initialized", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
                
            var pathsField = EnemySpawner.Instance.GetType().GetField("pathsAvailable", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
                
            if (spawnerField != null)
                spawnerField.SetValue(EnemySpawner.Instance, true);
                
            if (pathsField != null)
                pathsField.SetValue(EnemySpawner.Instance, true);
            
            Debug.Log("已强制设置EnemySpawner为持续生成模式");
            
            // 检查路径
            CheckAndFixPaths();
        }
    }
    
    // 检查并修复路径
    void CheckAndFixPaths()
    {
        // 检查陆地路径
        GameObject landPath = GameObject.Find(landPathName);
        if (landPath == null || landPath.transform.childCount == 0)
        {
            Debug.Log("发现陆地路径丢失，尝试修复");
            CreateDefaultPath(landPathName, true);
        }
        
        // 检查水路径
        GameObject waterPath = GameObject.Find(waterPathName);
        if (waterPath == null || waterPath.transform.childCount == 0)
        {
            Debug.Log("发现水路径丢失，尝试修复");
            CreateDefaultPath(waterPathName, false);
        }
    }
} 