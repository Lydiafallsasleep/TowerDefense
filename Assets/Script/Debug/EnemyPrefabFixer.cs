using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyPrefabFixer : MonoBehaviour
{
    [Header("预制体修复")]
    [Tooltip("游戏开始时是否自动修复预制体")]
    public bool fixPrefabsOnAwake = false; // 默认关闭，避免每次运行都修改
    [Tooltip("是否仅在实例上修复组件而不修改预制体")]
    public bool fixInstancesOnly = true;
    
    private bool hasFixedPrefabs = false;
    
    void Awake()
    {
        if (fixPrefabsOnAwake && !hasFixedPrefabs)
        {
            if (fixInstancesOnly)
            {
                FixEnemyInstances();
            }
            else
            {
                FixEnemyPrefabs();
            }
            hasFixedPrefabs = true;
        }
    }
    
    // 只修复场景中的实例，不修改预制体
    [ContextMenu("修复场景中的敌人实例")]
    public void FixEnemyInstances()
    {
        Debug.Log("开始修复场景中的敌人实例...");
        
        // 查找场景中所有EnemyMovement
        EnemyMovement[] enemies = FindObjectsOfType<EnemyMovement>();
        
        foreach (EnemyMovement enemy in enemies)
        {
            FixEnemyInstance(enemy.gameObject);
        }
        
        Debug.Log($"场景中的敌人实例修复完成，共修复{enemies.Length}个敌人");
    }
    
    void FixEnemyInstance(GameObject instance)
    {
        // 移除多余的EnemyMovement组件，只保留第一个
        EnemyMovement[] movements = instance.GetComponents<EnemyMovement>();
        if (movements.Length > 1)
        {
            Debug.Log($"敌人 {instance.name} 有 {movements.Length} 个EnemyMovement组件，正在修复...");
            
            // 保留第一个组件的设置
            EnemyMovement firstMovement = movements[0];
            EnemyMovement.MonsterType type = firstMovement.monsterType;
            float speed = firstMovement.moveSpeed;
            float threshold = firstMovement.waypointThreshold;
            
            // 删除多余的组件
            for (int i = 1; i < movements.Length; i++)
            {
                if (Application.isPlaying)
                    Destroy(movements[i]);
                else
                    DestroyImmediate(movements[i]);
            }
            
            // 更新第一个组件的设置
            firstMovement.moveSpeed = speed;
            firstMovement.waypointThreshold = threshold;
            
            // 设置其他属性
            Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }
    
    [ContextMenu("修复敌人预制体(谨慎使用)")]
    public void FixEnemyPrefabs()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogWarning("游戏运行时不应修改预制体！如需修复敌人，请使用FixEnemyInstances()");
            return;
        }
        
        Debug.Log("开始修复敌人预制体...");
        
        // 加载预制体
        GameObject slimePrefab = Resources.Load<GameObject>("Slime");
        GameObject fishPrefab = Resources.Load<GameObject>("Fish");
        
        // 修复史莱姆预制体
        if (slimePrefab != null)
        {
            Debug.Log("修复史莱姆预制体");
            FixEnemyPrefab(slimePrefab, EnemyMovement.MonsterType.Slime);
        }
        else
            Debug.LogError("无法加载史莱姆预制体！请确保它位于Resources文件夹中");
        
        // 修复鱼预制体
        if (fishPrefab != null)
        {
            Debug.Log("修复鱼预制体");
            FixEnemyPrefab(fishPrefab, EnemyMovement.MonsterType.Fish);
        }
        else
            Debug.LogError("无法加载鱼预制体！请确保它位于Resources文件夹中");
        
        Debug.Log("敌人预制体修复完成");
#else
        Debug.LogWarning("只能在编辑器中修复预制体");
#endif
    }
    
    void FixEnemyPrefab(GameObject prefab, EnemyMovement.MonsterType type)
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;
        
        // 移除所有EnemyMovement组件（可能有多个）
        EnemyMovement[] movements = prefab.GetComponents<EnemyMovement>();
        
        // 记录现有设置
        float speed = 2f;
        float threshold = 0.1f;
        int retries = 3;
        float retryInterval = 0.5f;
        bool smoothMovement = true;
        float rotationSpeed = 5f;
        
        // 如果有现有组件，获取它的设置
        if (movements.Length > 0)
        {
            speed = movements[0].moveSpeed;
            threshold = movements[0].waypointThreshold;
            retries = movements[0].maxInitRetries;
            retryInterval = movements[0].retryInterval;
            smoothMovement = movements[0].useSmoothMovement;
            rotationSpeed = movements[0].rotationSpeed;
        }
        
        // 删除所有EnemyMovement组件
        foreach (EnemyMovement movement in movements)
        {
            DestroyImmediate(movement);
        }
        
        // 添加新的EnemyMovement组件
        EnemyMovement newMovement = prefab.AddComponent<EnemyMovement>();
        newMovement.monsterType = type;
        newMovement.moveSpeed = speed;
        newMovement.waypointThreshold = threshold;
        newMovement.maxInitRetries = retries;
        newMovement.retryInterval = retryInterval;
        newMovement.useSmoothMovement = smoothMovement;
        newMovement.rotationSpeed = rotationSpeed;
        
        // 获取并修复Rigidbody2D
        Rigidbody2D rb = prefab.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;  // 禁用重力
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // 冻结旋转
        }
        else
        {
            // 添加Rigidbody2D
            rb = prefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // 重置Transform
        prefab.transform.position = Vector3.zero;
        prefab.transform.rotation = Quaternion.identity;
        prefab.transform.localScale = new Vector3(1f, 1f, 1f);
        
        // 确保有SpriteRenderer组件
        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10;  // 确保在前景
        }
        
        // 设置标签为Enemy
        prefab.tag = "Enemy";
        
        // 保存修改到预制体
        Debug.Log($"保存修改到预制体: {prefab.name}");
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
#endif
    }
} 