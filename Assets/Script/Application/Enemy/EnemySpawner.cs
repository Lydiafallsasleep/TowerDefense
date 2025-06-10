using UnityEngine;
using System.Collections;

public class EnemySpawner : Singleton<EnemySpawner>
{
    [Header("生成设置")]
    public float spawnInterval = 2f;
    public Transform spawnPoint;
    public bool autoSpawn = true;
    [Tooltip("初始延迟，确保路径先生成")]
    public float initialDelay = 3f; // 延长初始等待时间
    [Tooltip("场上允许的最大敌人数量")]
    public int maxEnemiesOnScreen = 10;

    [Header("路径检查")]
    public int maxPathCheckAttempts = 5;
    public float pathCheckInterval = 1f;
    [Tooltip("如果设为true，找不到路径时会强制重新生成")]
    public bool regeneratePathsIfMissing = true;
    [Tooltip("如果设为true，总是重新生成路径")]
    public bool alwaysRegeneratePaths = false;

    private float timer = 0f;
    private bool initialized = false;
    private bool pathsAvailable = false;

    void Start()
    {
        // 延迟一段时间再开始生成敌人，确保路径已经生成
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        Debug.Log("EnemySpawner正在等待初始化...");
        yield return new WaitForSeconds(initialDelay);
        
        // 尝试等待并检查路径是否准备好
        yield return StartCoroutine(WaitForPaths());
        
        initialized = true;
        
        if (pathsAvailable)
        {
            Debug.Log("EnemySpawner初始化完成，路径可用，开始生成敌人");
        }
        else
        {
            Debug.LogError("EnemySpawner初始化完成，但路径不可用，敌人可能无法正确移动！");
            // 如果配置为需要重新生成，则强制生成路径
            if (regeneratePathsIfMissing && PathManager.Instance != null)
            {
                Debug.Log("尝试强制生成路径...");
                PathManager.Instance.forceRegeneratePaths = true;
                PathManager.Instance.GeneratePaths();
                yield return new WaitForSeconds(0.5f);
                pathsAvailable = CheckPaths();
            }
        }
    }

    IEnumerator WaitForPaths()
    {
        int attempts = 0;
        
        // 如果设置了总是重新生成路径
        if (alwaysRegeneratePaths && PathManager.Instance != null)
        {
            Debug.Log("配置为总是重新生成路径");
            PathManager.Instance.forceRegeneratePaths = true;
            PathManager.Instance.GeneratePaths();
            yield return new WaitForSeconds(0.5f);
        }
        
        // 优先使用PathManager检查路径
        if (PathManager.Instance != null)
        {
            while (attempts < maxPathCheckAttempts && !pathsAvailable)
            {
                Debug.Log($"检查路径可用性，尝试 {attempts+1}/{maxPathCheckAttempts}");
                pathsAvailable = PathManager.Instance.ArePathsGenerated();
                
                if (pathsAvailable)
                {
                    Debug.Log("通过PathManager确认路径已生成");
                    break;
                }
                
                attempts++;
                yield return new WaitForSeconds(pathCheckInterval);
            }
        }
        else
        {
            // 直接检查路径
            while (attempts < maxPathCheckAttempts && !pathsAvailable)
            {
                Debug.Log($"直接检查路径可用性，尝试 {attempts+1}/{maxPathCheckAttempts}");
                pathsAvailable = CheckPaths();
                
                if (pathsAvailable)
                    break;
                
                attempts++;
                yield return new WaitForSeconds(pathCheckInterval);
            }
        }
    }

    bool CheckPaths()
    {
        bool landPathOk = false;
        bool waterPathOk = false;
        
        // 检查陆地路径
        GameObject landPathObj = GameObject.Find("LandPathParent");
        Transform landPath = landPathObj?.transform;
        if (landPath != null && landPath.childCount > 0)
        {
            Debug.Log($"找到陆地路径，路径点数量：{landPath.childCount}");
            landPathOk = true;
        }
        else
        {
            Debug.LogError("无法找到有效的陆地路径(LandPathParent)或路径点为空");
            
            // 只有在配置为需要重新生成时才尝试生成
            if (regeneratePathsIfMissing)
            {
                // 尝试调用路径生成器
                LandPathGenerator generator = FindObjectOfType<LandPathGenerator>();
                if (generator != null)
                {
                    Debug.Log("尝试通过LandPathGenerator生成陆地路径");
                    generator.GenerateLandPath();
                }
            }
        }

        // 检查水路径
        GameObject waterPathObj = GameObject.Find("WaterPathParent");
        Transform waterPath = waterPathObj?.transform;
        if (waterPath != null && waterPath.childCount > 0)
        {
            Debug.Log($"找到水路径，路径点数量：{waterPath.childCount}");
            waterPathOk = true;
        }
        else
        {
            Debug.LogError("无法找到有效的水路径(WaterPathParent)或路径点为空");
            
            // 只有在配置为需要重新生成时才尝试生成
            if (regeneratePathsIfMissing)
            {
                // 尝试调用路径生成器
                WaterPathGenerator generator = FindObjectOfType<WaterPathGenerator>();
                if (generator != null)
                {
                    Debug.Log("尝试通过WaterPathGenerator生成水路径");
                    generator.GenerateWaterPath();
                }
            }
        }
        
        return landPathOk && waterPathOk;
    }

    void Update()
    {
        if (!autoSpawn || !initialized || !pathsAvailable)
            return;

        // 检查当前场景中的敌人数量
        int currentEnemyCount = GameObject.FindObjectsOfType<EnemyMovement>().Length;
        
        // 如果已达到最大敌人数，则不再生成
        if (currentEnemyCount >= maxEnemiesOnScreen)
        {
            timer = 0f; // 重置计时器
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
            Debug.Log($"自动生成敌人，当前敌人数量：{currentEnemyCount + 1}");
        }
    }

    public void SpawnEnemy(EnemyMovement.MonsterType? type = null)
    {
        // 如果路径不可用，再次检查
        if (!pathsAvailable)
        {
            pathsAvailable = CheckPaths();
            if (!pathsAvailable)
            {
                Debug.LogError("由于路径不可用，无法生成敌人");
                return;
            }
        }
        
        // 决定生成哪种敌人类型
        EnemyMovement.MonsterType enemyType = type ?? (Random.value > 0.5f ? EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish);
        string enemyPrefabName = enemyType == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
        
        Debug.Log($"准备生成敌人：{enemyPrefabName}，当前激活敌人数量：{GameObject.FindObjectsOfType<EnemyMovement>().Length}");
        
        // 检查路径是否存在
        string pathParentName = enemyType == EnemyMovement.MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
        GameObject pathParentObj = GameObject.Find(pathParentName);
        Transform pathParent = pathParentObj?.transform;
        
        if (pathParent == null)
        {
            Debug.LogError($"无法生成{enemyPrefabName}：找不到{pathParentName}路径！");
            return;
        }
        
        if (pathParent.childCount == 0)
        {
            Debug.LogError($"无法生成{enemyPrefabName}：{pathParentName}路径下没有路径点！");
            return;
        }
        
        // 从对象池获取敌人实例
        GameObject enemy = ObjectPool.Instance.OnSpawn(enemyPrefabName);
        if (enemy == null)
        {
            Debug.LogError($"无法从对象池获取{enemyPrefabName}，请确保预制体已正确放入Resources文件夹");
            return;
        }

        // 确保敌人是激活的
        enemy.SetActive(true);
        
        // 设置初始位置为对应路径的第一个waypoint
        if (pathParent.childCount > 0)
        {
            enemy.transform.position = pathParent.GetChild(0).position;
            Debug.Log($"在路径{pathParentName}的起点生成了{enemyPrefabName}，位置：{enemy.transform.position}，激活状态：{enemy.activeSelf}");
        }
        else if (spawnPoint != null)
        {
            enemy.transform.position = spawnPoint.position;
            Debug.Log($"在指定生成点生成了{enemyPrefabName}，位置：{enemy.transform.position}，激活状态：{enemy.activeSelf}");
        }

        // 设置敌人类型
        var movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.monsterType = enemyType;
        }
        else
        {
            Debug.LogError($"生成的{enemyPrefabName}没有EnemyMovement组件！");
        }
    }

    // 添加一个公共方法，用于手动生成多个敌人
    public void SpawnMultipleEnemies(int count)
    {
        Debug.Log($"请求生成{count}个敌人...");
        StartCoroutine(SpawnMultipleEnemiesCoroutine(count));
    }
    
    private IEnumerator SpawnMultipleEnemiesCoroutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            // 短暂等待以确保每个敌人都能正确初始化
            yield return new WaitForSeconds(0.2f);
        }
    }
} 