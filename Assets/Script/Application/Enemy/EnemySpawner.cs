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

    void Start()
    {
        // 延迟一段时间再开始生成敌人
        StartCoroutine(DelayedStart());
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
        if (!autoSpawn || !initialized || !pathsAvailable)
        {
            Debug.LogWarning($"敌人生成被暂停: autoSpawn={autoSpawn}, initialized={initialized}, pathsAvailable={pathsAvailable}");
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
        
        Debug.Log($"准备生成敌人：{enemyPrefabName}，当前激活敌人数量：{GameObject.FindObjectsOfType<EnemyMovement>().Length}");
        
        // 检查路径是否存在
        string pathParentName = enemyType == EnemyMovement.MonsterType.Slime ? landPathName : waterPathName;
        GameObject pathParentObj = GameObject.Find(pathParentName);
        Transform pathParent = pathParentObj?.transform;
        
        if (pathParent == null)
        {
            Debug.LogError($"无法生成{enemyPrefabName}：找不到{pathParentName}路径！创建默认路径");
            CreateDefaultPath(pathParentName, enemyType == EnemyMovement.MonsterType.Slime);
            pathParentObj = GameObject.Find(pathParentName);
            pathParent = pathParentObj?.transform;
            
            if (pathParent == null)
            {
                Debug.LogError("无法创建路径，但不会阻止下次尝试生成敌人");
            return;
            }
        }
        
        if (pathParent.childCount == 0)
        {
            Debug.LogError($"无法生成{enemyPrefabName}：{pathParentName}路径下没有路径点！创建默认路径");
            CreateDefaultPath(pathParentName, enemyType == EnemyMovement.MonsterType.Slime);
            // 重新获取路径对象
            pathParentObj = GameObject.Find(pathParentName);
            pathParent = pathParentObj?.transform;
            
            if (pathParent == null || pathParent.childCount == 0) 
            {
                Debug.LogError("无法创建默认路径，但不会阻止下次尝试生成敌人");
            return;
            }
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