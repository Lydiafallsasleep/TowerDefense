using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnDebugger : MonoBehaviour
{
    [Header("调试功能")]
    [Tooltip("按下此键直接生成Slime")]
    public KeyCode spawnSlimeKey = KeyCode.F1;
    [Tooltip("按下此键直接生成Fish")]
    public KeyCode spawnFishKey = KeyCode.F2;
    [Tooltip("按下此键重置资源加载")]
    public KeyCode reloadResourcesKey = KeyCode.F3;
    [Tooltip("按下此键显示调试信息")]
    public KeyCode showDebugInfoKey = KeyCode.F4;

    [Header("手动生成设置")]
    public Transform spawnPosition;
    public bool createSpawnPoint = true;
    
    [Header("调试信息")]
    [TextArea(3, 8)]
    public string debugInfo = "";
    private List<string> debugLog = new List<string>();
    private bool resourcesReloaded = false;
    
    void Start()
    {
        // 如果未指定生成点，创建一个
        if (spawnPosition == null && createSpawnPoint)
        {
            GameObject spawnPoint = new GameObject("DebugSpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 0, 0);
            spawnPosition = spawnPoint.transform;
            LogDebug("创建了调试生成点");
        }
    }
    
    void Update()
    {
        // 检测快捷键
        if (Input.GetKeyDown(spawnSlimeKey))
        {
            SpawnEnemy(EnemyMovement.MonsterType.Slime);
        }
        
        if (Input.GetKeyDown(spawnFishKey))
        {
            SpawnEnemy(EnemyMovement.MonsterType.Fish);
        }
        
        if (Input.GetKeyDown(reloadResourcesKey))
        {
            ReloadResources();
        }
        
        if (Input.GetKeyDown(showDebugInfoKey))
        {
            ShowDebugInfo();
        }
    }
    
    // 手动生成敌人
    public void SpawnEnemy(EnemyMovement.MonsterType type)
    {
        string prefabName = type == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
        LogDebug($"尝试手动生成敌人: {prefabName}");
        
        // 直接尝试从Resources加载，检查路径是否正确
        GameObject prefab = Resources.Load<GameObject>($"enemy/{prefabName}");
        if (prefab != null)
        {
            LogDebug($"成功加载预制体: enemy/{prefabName}");
        }
        else
        {
            LogDebug($"无法直接加载预制体: enemy/{prefabName}");
        }
        
        // 从对象池生成敌人
        GameObject enemy = null;
        try
        {
            enemy = ObjectPool.Instance.OnSpawn(prefabName);
            LogDebug($"从对象池生成敌人: {(enemy != null ? "成功" : "失败")}");
        }
        catch (System.Exception e)
        {
            LogDebug($"对象池生成异常: {e.Message}");
        }
        
        // 如果成功生成，设置位置和激活状态
        if (enemy != null)
        {
            enemy.SetActive(true);
            
            // 如果有指定生成点，使用该位置
            if (spawnPosition != null)
            {
                enemy.transform.position = spawnPosition.position;
                LogDebug($"设置敌人位置: {spawnPosition.position}");
            }
            
            // 检查敌人组件
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.monsterType = type;
                LogDebug($"设置敌人类型: {type}");
            }
            else
            {
                LogDebug("警告: 敌人缺少EnemyMovement组件");
            }
        }
    }
    
    // 重新加载所有Resources资源
    public void ReloadResources()
    {
        LogDebug("尝试重新加载资源...");
        
        // 强制Unity重新加载资源
        Resources.UnloadUnusedAssets();
        
        // 检查资源加载情况
        GameObject slime = Resources.Load<GameObject>("enemy/Slime");
        GameObject fish = Resources.Load<GameObject>("enemy/Fish");
        
        LogDebug($"资源检查 - Slime: {(slime != null ? "可用" : "不可用")}");
        LogDebug($"资源检查 - Fish: {(fish != null ? "可用" : "不可用")}");
        
        // 尝试所有可能的路径
        CheckResourcePath("enemy/Slime");
        CheckResourcePath("enemy/Fish");
        CheckResourcePath("Slime");
        CheckResourcePath("Fish");
        
        resourcesReloaded = true;
    }
    
    // 检查资源路径
    private void CheckResourcePath(string path)
    {
        GameObject obj = Resources.Load<GameObject>(path);
        LogDebug($"路径检查: {path} - {(obj != null ? "可用" : "不可用")}");
    }
    
    // 显示详细调试信息
    public void ShowDebugInfo()
    {
        // 收集项目关键路径信息
        string dataPath = Application.dataPath;
        string persistentDataPath = Application.persistentDataPath;
        string streamingAssetsPath = Application.streamingAssetsPath;
        
        LogDebug("===== Unity路径信息 =====");
        LogDebug($"Application.dataPath: {dataPath}");
        LogDebug($"Application.persistentDataPath: {persistentDataPath}");
        LogDebug($"Application.streamingAssetsPath: {streamingAssetsPath}");
        LogDebug("===== 敌人状态 =====");
        
        // 检查当前场景中的敌人
        EnemyMovement[] enemies = GameObject.FindObjectsOfType<EnemyMovement>();
        LogDebug($"当前场景中敌人数量: {enemies.Length}");
        
        // 检查路径情况
        GameObject landPath = GameObject.Find("LandPathParent");
        GameObject waterPath = GameObject.Find("WaterPathParent");
        
        LogDebug($"陆地路径: {(landPath != null ? $"存在，路径点:{landPath.transform.childCount}" : "不存在")}");
        LogDebug($"水路径: {(waterPath != null ? $"存在，路径点:{waterPath.transform.childCount}" : "不存在")}");
    }
    
    // 添加调试日志
    private void LogDebug(string message)
    {
        Debug.Log($"[EnemyDebugger] {message}");
        debugLog.Add(message);
        
        // 只保留最近的10条日志
        while (debugLog.Count > 10)
        {
            debugLog.RemoveAt(0);
        }
        
        // 更新调试信息显示
        debugInfo = string.Join("\n", debugLog);
    }
} 