using UnityEngine;
using System.Collections;

public class EnemySpawner : Singleton<EnemySpawner>
{
    [Header("生成设置")]
    public float spawnInterval = 2f;
    public Transform spawnPoint;
    public bool autoSpawn = true;
    [Tooltip("初始延迟，确保路径先生成")]
    public float initialDelay = 1f;
    [Tooltip("场上允许的最大敌人数量")]
    public int maxEnemiesOnScreen = 10;

    // 简化路径相关设置
    [Header("路径设置")]
    [Tooltip("默认的陆地路径名称")]
    public string landPathName = "LandPathParent";
    [Tooltip("默认的水路径名称")]
    public string waterPathName = "WaterPathParent";

    private float timer = 0f;
    private bool initialized = false;
    private bool pathsAvailable = false;
    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private GameManager gameManager;

    void Start()
    {
        // 查找PlayerHealth组件
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            // 订阅游戏结束事件
            playerHealth.OnGameOver += OnGameOver;
            Debug.Log("[EnemySpawner] 已订阅PlayerHealth的OnGameOver事件");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] 未找到PlayerHealth组件");
        }
        
        // 查找GameManager组件
        gameManager = GameManager.Instance;
        
        // 延迟一段时间再开始生成敌人
        StartCoroutine(DelayedStart());
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (playerHealth != null)
        {
            playerHealth.OnGameOver -= OnGameOver;
        }
    }
    
    // 游戏结束回调
    private void OnGameOver()
    {
        Debug.Log("[EnemySpawner] 收到游戏结束事件，停止生成敌人");
        isGameOver = true;
        autoSpawn = false;
        StopAllCoroutines();
    }

    IEnumerator DelayedStart()
    {
        Debug.Log("EnemySpawner正在等待初始化...");
        yield return new WaitForSeconds(initialDelay);
        
        // 检查路径是否存在
        pathsAvailable = CheckPaths();
        initialized = true;
        
        if (pathsAvailable)
        {
            Debug.Log("EnemySpawner初始化完成，路径可用，开始生成敌人");
        }
        else
        {
            Debug.LogError("EnemySpawner初始化完成，但路径不可用，敌人可能无法正确移动！");
            // 简化处理，直接强制允许生成
            pathsAvailable = true;
            CreateDefaultPaths();
        }
    }

    // 简化的路径检查方法
    bool CheckPaths()
    {
        bool landPathOk = false;
        bool waterPathOk = false;
        
        // 检查陆地路径
        GameObject landPathObj = GameObject.Find(landPathName);
        Transform landPath = landPathObj?.transform;
        if (landPath != null && landPath.childCount > 0)
        {
            Debug.Log($"找到陆地路径，路径点数量：{landPath.childCount}");
            landPathOk = true;
        }
        else
        {
            Debug.LogError($"无法找到有效的陆地路径({landPathName})或路径点为空");
            CreateDefaultPath(landPathName, true);
            landPathOk = true;
        }

        // 检查水路径
        GameObject waterPathObj = GameObject.Find(waterPathName);
        Transform waterPath = waterPathObj?.transform;
        if (waterPath != null && waterPath.childCount > 0)
        {
            Debug.Log($"找到水路径，路径点数量：{waterPath.childCount}");
            waterPathOk = true;
        }
        else
        {
            Debug.LogError($"无法找到有效的水路径({waterPathName})或路径点为空");
            CreateDefaultPath(waterPathName, false);
            waterPathOk = true;
        }
        
        return landPathOk && waterPathOk;
    }

    // 创建默认路径
    void CreateDefaultPaths()
    {
        CreateDefaultPath(landPathName, true);
        CreateDefaultPath(waterPathName, false);
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

    void Update()
    {
        // 如果游戏已结束，停止生成敌人
        if (isGameOver)
        {
            return;
        }
        
        // 检查GameManager是否标记游戏结束
        if (gameManager != null && gameManager.isGameOver)
        {
            Debug.Log("[EnemySpawner] 检测到GameManager.isGameOver为true，停止生成敌人");
            isGameOver = true;
            autoSpawn = false;
            return;
        }
        
        if (!autoSpawn || !initialized || !pathsAvailable)
        {
            return;
        }

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
        // 如果游戏已结束，不再生成敌人
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] 游戏已结束，不再生成敌人");
            return;
        }
        
        // 确保路径可用
        if (!pathsAvailable)
        {
            pathsAvailable = CheckPaths();
            if (!pathsAvailable)
            {
                Debug.LogError("由于路径不可用，无法生成敌人，但不会阻止下次尝试");
                pathsAvailable = true; // 强制重设为true，确保下次仍然尝试生成
                return;
            }
        }
        
        // 决定生成哪种敌人类型
        EnemyMovement.MonsterType enemyType = type ?? (Random.value > 0.5f ? EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish);
        string enemyPrefabName = enemyType == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
        
        // 尝试从对象池获取敌人
        GameObject enemy = null;
        if (ObjectPool.Instance != null)
        {
            enemy = ObjectPool.Instance.OnSpawn(enemyPrefabName);
        }
        
        // 如果对象池没有返回有效对象，尝试直接实例化
        if (enemy == null)
        {
            // 查找预制体资源
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Enemies/{enemyPrefabName}");
            if (prefab != null)
            {
                enemy = Instantiate(prefab);
            }
            else
            {
                Debug.LogError($"无法加载敌人预制体: {enemyPrefabName}");
                return;
            }
        }
        
        // 设置敌人位置和类型
        if (enemy != null)
        {
            // 设置初始位置
            if (spawnPoint != null)
            {
                enemy.transform.position = spawnPoint.position;
            }
            else
            {
                enemy.transform.position = Vector3.zero;
            }
            
            // 设置敌人类型
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.monsterType = enemyType;
            }
            else
            {
                Debug.LogError($"生成的敌人对象没有EnemyMovement组件: {enemy.name}");
            }
        }
    }

    public void SpawnMultipleEnemies(int count)
    {
        // 如果游戏已结束，不再生成敌人
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] 游戏已结束，不再生成敌人");
            return;
        }
        
        StartCoroutine(SpawnMultipleEnemiesCoroutine(count));
    }

    private IEnumerator SpawnMultipleEnemiesCoroutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 如果游戏已结束，中断生成
            if (isGameOver)
            {
                Debug.Log("[EnemySpawner] 游戏已结束，中断批量生成敌人");
                yield break;
            }
            
            SpawnEnemy();
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    // 公共方法：设置游戏结束状态
    public void SetGameOver(bool gameOver)
    {
        isGameOver = gameOver;
        if (isGameOver)
        {
            autoSpawn = false;
            StopAllCoroutines();
            Debug.Log("[EnemySpawner] 游戏结束状态已设置，停止生成敌人");
        }
        else
        {
            autoSpawn = true;
            Debug.Log("[EnemySpawner] 游戏结束状态已取消，恢复生成敌人");
        }
    }
    
    /// <summary>
    /// 生成指定类型的敌人并返回敌人游戏对象
    /// </summary>
    /// <param name="type">敌人类型</param>
    /// <returns>生成的敌人游戏对象</returns>
    public GameObject SpawnEnemyWithType(EnemyMovement.MonsterType type)
    {
        // 如果游戏已结束，不再生成敌人
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] 游戏已结束，不再生成敌人");
            return null;
        }
        
        // 确保路径可用
        if (!pathsAvailable)
        {
            pathsAvailable = CheckPaths();
            if (!pathsAvailable)
            {
                Debug.LogError("由于路径不可用，无法生成敌人");
                return null;
            }
        }
        
        // 根据类型确定预制体名称
        string enemyPrefabName = type == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
        
        // 尝试从对象池获取敌人
        GameObject enemy = null;
        if (ObjectPool.Instance != null)
        {
            enemy = ObjectPool.Instance.OnSpawn(enemyPrefabName);
        }
        
        // 如果对象池没有返回有效对象，尝试直接实例化
        if (enemy == null)
        {
            // 查找预制体资源
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Enemies/{enemyPrefabName}");
            if (prefab != null)
            {
                enemy = Instantiate(prefab);
            }
            else
            {
                Debug.LogError($"无法加载敌人预制体: {enemyPrefabName}");
                return null;
            }
        }
        
        // 设置敌人位置和类型
        if (enemy != null)
        {
            // 设置初始位置
            if (spawnPoint != null)
            {
                enemy.transform.position = spawnPoint.position;
            }
            else
            {
                enemy.transform.position = Vector3.zero;
            }
            
            // 设置敌人类型
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.monsterType = type;
            }
            else
            {
                Debug.LogError($"生成的敌人对象没有EnemyMovement组件: {enemy.name}");
            }
        }
        
        return enemy;
    }
    
    // 重置敌人生成器状态
    public void ResetState()
    {
        Debug.Log("[EnemySpawner] 重置敌人生成器状态");
        
        // 停止所有协程
        StopAllCoroutines();
        
        // 重置状态
        isGameOver = false;
        autoSpawn = true;
        timer = 0f;
        
        // 重新初始化
        StartCoroutine(DelayedStart());
        
        Debug.Log("[EnemySpawner] 敌人生成器状态已重置");
    }
} 