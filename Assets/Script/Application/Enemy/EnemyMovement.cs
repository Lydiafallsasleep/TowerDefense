using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    public enum MonsterType { Slime, Fish }

    [Header("Settings")]
    public MonsterType monsterType;
    public float moveSpeed = 2f;
    public float originalMoveSpeed = 2f; // 原始移动速度，用于减速效果恢复
    public float waypointThreshold = 0.1f;
    [Tooltip("路径初始化重试次数")]
    public int maxInitRetries = 3;
    [Tooltip("每次重试间隔时间(秒)")]
    public float retryInterval = 0.5f;
    [Tooltip("启用平滑移动")]
    public bool useSmoothMovement = true;
    [Tooltip("转向速度")]
    public float rotationSpeed = 5f;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool pathInitialized = false;
    private Vector2 currentDirection;
    private Vector2 targetDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDirection = Vector2.right; // 初始方向
    }

    void OnEnable()
    {
        // 重置关键状态
        pathInitialized = false;
        currentWaypointIndex = 0;
        
        // 开启协程，在多帧内尝试初始化路径
        StartCoroutine(InitializePathWithRetry());
    }

    IEnumerator InitializePathWithRetry()
    {
        int retryCount = 0;
        
        // 先等待一小段时间，确保路径管理器有机会初始化
        yield return new WaitForSeconds(0.2f);
        
        // 尝试初始化路径，最多重试maxInitRetries次
        while (!pathInitialized && retryCount < maxInitRetries)
        {
            if (InitializePath())
            {
                pathInitialized = true;
                yield break; // 成功初始化，退出协程
            }
            
            retryCount++;
            Debug.Log($"路径初始化失败，{retryInterval}秒后尝试第{retryCount}次重试...");
            yield return new WaitForSeconds(retryInterval); // 等待一段时间再次尝试
        }
        
        if (!pathInitialized)
        {
            Debug.LogError($"路径初始化失败，已重试{retryCount}次。敌人将被禁用。");
            gameObject.SetActive(false); // 多次重试失败后禁用敌人
        }
    }

    bool InitializePath()
    {
        string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
        Debug.Log($"[{gameObject.name}] 尝试查找路径父对象：{parentName}");
        
        // 直接查找
        GameObject pathParentObj = GameObject.Find(parentName);
        Transform pathParent = pathParentObj?.transform;
        
        // 如果没有找到路径父对象，强制PathManager生成
        if (pathParent == null)
        {
            Debug.LogError($"[{gameObject.name}] 未找到路径父对象: {parentName}，尝试通过PathManager创建");
            PathManager pathManager = PathManager.Instance;
            if (pathManager != null)
            {
                pathManager.GeneratePaths();
                // 重新查找
                pathParentObj = GameObject.Find(parentName);
                pathParent = pathParentObj?.transform;
            }
            
            // 如果还是没找到，创建临时路径
            if (pathParent == null)
            {
                return CreateAndUseTemporaryPath(parentName);
            }
        }
        
        // 路径父对象存在，检查子对象
        if (pathParent.childCount == 0)
        {
            // 有父对象但没有子对象，尝试重新生成
            Debug.LogError($"[{gameObject.name}] 路径对象{parentName}存在，但没有子对象！尝试等待完全初始化...");
            
            // 为了调试，检查Unity是否能看到该对象和它的Transform组件
            if (pathParentObj != null && pathParent != null)
            {
                Debug.Log($"路径对象可用: {pathParentObj.name}, 激活状态: {pathParentObj.activeSelf}, " +
                         $"位置: {pathParent.position}, 子对象数量: {pathParent.childCount}");
            }
            
            // 尝试通过PathManager重新生成
            PathManager pathManager = PathManager.Instance;
            if (pathManager != null)
            {
                Debug.Log($"通过PathManager重新生成{parentName}");
                pathManager.FixEmptyPath(parentName);
                pathParentObj = GameObject.Find(parentName);
                pathParent = pathParentObj?.transform;
            }
            
            // 如果父对象存在但子对象仍为0，则直接创建临时路径
            if (pathParent == null || pathParent.childCount == 0)
            {
                Debug.LogError($"无法修复空路径，创建临时路径");
                return CreateAndUseTemporaryPath(parentName);
            }
        }
        
        Debug.Log($"成功找到路径父对象：{parentName}，子对象数量：{pathParent.childCount}");
        
        // 创建路径点数组并立即填充
        try
        {
            int childCount = pathParent.childCount;
            waypoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                waypoints[i] = pathParent.GetChild(i);
                if (waypoints[i] == null)
                {
                    Debug.LogError($"路径点 {i} 为null！路径初始化失败");
                    return false;
                }
            }
            
            currentWaypointIndex = 0;
            Debug.Log($"为{monsterType}初始化了{waypoints.Length}个路径点");
            
            // 额外验证：确保第一个路径点可用
            if (waypoints.Length > 0 && waypoints[0] != null)
            {
                Debug.Log($"第一个路径点位置: {waypoints[0].position}");
                // 设置初始位置为第一个路径点
                transform.position = waypoints[0].position;
                
                // 如果有第二个路径点，计算初始方向
                if (waypoints.Length > 1)
                {
                    Vector2 direction = (waypoints[1].position - waypoints[0].position).normalized;
                    currentDirection = direction;
                    targetDirection = direction;
                }
                
                return true; // 初始化成功
            }
            else
            {
                Debug.LogError("路径点数组为空或第一个路径点为null！");
                return false; // 初始化失败
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"路径点初始化出现异常: {e.Message}");
            return false;
        }
    }

    // 创建并使用临时路径，返回是否成功
    bool CreateAndUseTemporaryPath(string parentName)
    {
        Debug.Log($"创建并使用临时路径: {parentName}");
        Transform pathParent = CreateTemporaryPath(parentName);
        if (pathParent == null || pathParent.childCount == 0)
        {
            Debug.LogError("创建临时路径失败");
            return false;
        }

        int childCount = pathParent.childCount;
        waypoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            waypoints[i] = pathParent.GetChild(i);
        }
        
        currentWaypointIndex = 0;
        Debug.Log($"使用临时路径，路径点数量: {waypoints.Length}");
        
        // 设置初始位置为第一个路径点
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            
            // 如果有第二个路径点，计算初始方向
            if (waypoints.Length > 1)
            {
                Vector2 direction = (waypoints[1].position - waypoints[0].position).normalized;
                currentDirection = direction;
                targetDirection = direction;
            }
            
            return true;
        }
        
        return false;
    }

    // 创建一个临时路径用于测试
    private Transform CreateTemporaryPath(string pathName)
    {
        Debug.Log($"创建临时路径: {pathName}");
        
        // 清理现有的同名路径父对象
        GameObject existingPath = GameObject.Find(pathName);
        if (existingPath != null)
        {
            Debug.Log($"销毁已存在的{pathName}");
            Destroy(existingPath);
        }
        
        // 创建新的路径父对象
        GameObject pathParent = new GameObject(pathName);
        
        // 根据怪物类型创建不同的测试路径
        Vector3[] points = GetDefaultPathPoints(monsterType);
        
        // 创建路径点
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParent.transform);
        }
        
        Debug.Log($"临时路径已创建，路径点数量: {pathParent.transform.childCount}");
        return pathParent.transform;
    }

    // 根据怪物类型获取默认路径点
    private Vector3[] GetDefaultPathPoints(MonsterType type)
    {
        Vector3[] points;
        float startX = transform.position.x - 5f;
        float startY = transform.position.y - 5f;
        
        if (type == MonsterType.Slime)
        {
            // 创建一条Z字形陆地路径
            points = new Vector3[] {
                new Vector3(startX, startY, 0),
                new Vector3(startX + 10f, startY, 0),
                new Vector3(startX + 10f, startY + 5f, 0),
                new Vector3(startX, startY + 5f, 0),
                new Vector3(startX, startY + 10f, 0),
                new Vector3(startX + 10f, startY + 10f, 0)
            };
        }
        else // Fish
        {
            // 创建一条环形水路径
            points = new Vector3[] {
                new Vector3(startX, startY, 0),
                new Vector3(startX + 10f, startY, 0),
                new Vector3(startX + 10f, startY + 10f, 0),
                new Vector3(startX, startY + 10f, 0),
                new Vector3(startX, startY, 0)
            };
        }
        
        return points;
    }

    void FixedUpdate()
    {
        // 多重检查确保不会出现空引用
        if (!pathInitialized || waypoints == null || waypoints.Length == 0 || 
            currentWaypointIndex >= waypoints.Length || waypoints[currentWaypointIndex] == null)
        {
            // 没有路径时停止移动
            if (rb != null)
                rb.velocity = Vector2.zero;
            return;
        }

        // 获取当前目标点并确保它不为空
        Transform currentTarget = waypoints[currentWaypointIndex];
        if (currentTarget == null)
        {
            Debug.LogError($"路径点 {currentWaypointIndex} 为null！");
            rb.velocity = Vector2.zero;
            return;
        }

        // 计算前进方向
        targetDirection = (currentTarget.position - transform.position).normalized;
        
        // 使用平滑移动或直接移动
        if (useSmoothMovement)
        {
            // 平滑插值当前方向
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, rotationSpeed * Time.fixedDeltaTime);
            
            // 归一化确保速度一致
            currentDirection.Normalize();
            
            // 使用当前方向移动
            rb.velocity = currentDirection * moveSpeed;
        }
        else
        {
            // 直接使用目标方向（原始方式）
            rb.velocity = targetDirection * moveSpeed;
            currentDirection = targetDirection;
        }

        // 处理精灵翻转（如果向左移动则翻转）
        if (currentDirection.x < 0 && spriteRenderer != null)
            spriteRenderer.flipX = true;
        else if (currentDirection.x > 0 && spriteRenderer != null)
            spriteRenderer.flipX = false;

        // 检查是否到达路径点
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distanceToTarget < waypointThreshold)
        {
            // 对齐到当前路径点位置
            transform.position = currentTarget.position;
            
            currentWaypointIndex++;
            
            // 到达终点
            if (currentWaypointIndex >= waypoints.Length)
            {
                ReachedEnd();
            }
            // 设置新的目标方向（提前计算）
            else if (currentWaypointIndex < waypoints.Length - 1)
            {
                Vector2 nextDirection = (waypoints[currentWaypointIndex + 1].position - 
                                        waypoints[currentWaypointIndex].position).normalized;
                targetDirection = nextDirection;
            }
        }
    }

    void ReachedEnd()
    {
        // 通知游戏管理器减少生命值（这部分代码被注释了）
        //GameManager.Instance.PlayerTakeDamage(1);

        Debug.Log($"{gameObject.name} 到达了路径终点，准备回收，当前激活敌人数量：{GameObject.FindObjectsOfType<EnemyMovement>().Length}");

        // 安全回收敌人对象
        try
        {
            if (ObjectPool.Instance != null)
            {
                Debug.Log($"通过对象池回收敌人：{gameObject.name}，激活状态：{gameObject.activeSelf}");
                ObjectPool.Instance.OnDespawn(this.gameObject);
                // 确认回收后的状态
                Debug.Log($"敌人回收后状态：{gameObject.name}，激活状态：{gameObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning("ObjectPool实例为空，直接销毁对象");
                Destroy(gameObject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"对象回收时发生异常: {e.Message}");
            // 确保对象被禁用
            gameObject.SetActive(false);
        }
    }

    // 用于调试 - 在场景视图中绘制路径
    void OnDrawGizmosSelected()
    {
        if (!pathInitialized || waypoints == null || waypoints.Length < 2)
            return;
            
        Gizmos.color = Color.yellow;
        
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i+1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
        }
        
        // 当前目标点用红色标记
        if (currentWaypointIndex < waypoints.Length && waypoints[currentWaypointIndex] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(waypoints[currentWaypointIndex].position, 0.2f);
        }
    }

    // 供EnemyPoolObject调用，重置敌人状态
    public void ResetState()
    {
        // 重置关键状态
        pathInitialized = false;
        currentWaypointIndex = 0;
        
        // 确保停止所有正在运行的协程
        StopAllCoroutines();
        
        // 重新开启路径初始化协程
        StartCoroutine(InitializePathWithRetry());
        
        Debug.Log($"敌人 {gameObject.name} 状态已重置");
    }
    
    // 获取敌人在路径上的进度，0表示起点，1表示终点
    public float GetPathProgress()
    {
        if (waypoints == null || waypoints.Length <= 1)
            return 0f;
            
        return (float)currentWaypointIndex / (waypoints.Length - 1);
    }
}