using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [Header("调试组件")]
    public EnemyDebugger enemyDebugger;
    public EnemyPrefabFixer enemyPrefabFixer;
    
    [Header("全局调试选项")]
    public bool enableDebugLogging = true;
    public bool showGizmos = true;
    
    void Awake()
    {
        if (enableDebugLogging)
            Debug.unityLogger.logEnabled = true;
        else
            Debug.unityLogger.logEnabled = false;
        
        // 确保有所需的调试组件
        if (enemyDebugger == null)
            enemyDebugger = gameObject.AddComponent<EnemyDebugger>();
        
        if (enemyPrefabFixer == null)
            enemyPrefabFixer = gameObject.AddComponent<EnemyPrefabFixer>();
            
        enemyDebugger.showPathGizmos = showGizmos;
        
        Debug.Log("调试管理器初始化完成");
    }
    
    [ContextMenu("手动生成敌人")]
    public void SpawnEnemies()
    {
        if (enemyDebugger != null)
            enemyDebugger.SpawnTestEnemies();
        else
            Debug.LogError("找不到EnemyDebugger组件");
    }
    
    [ContextMenu("修复预制体")]
    public void FixEnemyPrefabs()
    {
        if (enemyPrefabFixer != null)
            enemyPrefabFixer.FixEnemyPrefabs();
        else
            Debug.LogError("找不到EnemyPrefabFixer组件");
    }
} 