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
    [Tooltip("到达终点时对玩家造成的伤害")]
    public int damageToPlayer = 1;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool pathInitialized = false;
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private PlayerHealth playerHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDirection = Vector2.right; // 初始方向
        
        // 保存原始速度，用于减速效果恢复
        originalMoveSpeed = moveSpeed;
        
        // 查找PlayerHealth组件
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[EnemyMovement] 未找到PlayerHealth组件，将使用GameManager进行伤害处理");
        }
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
        
        // 先等待一小段时间，确保路径有机会初始化
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
            Debug.LogError($"路径初始化失败，已重试{retryCount}次。创建临时路径。");
            // 创建临时路径并尝试使用
            string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
            CreateAndUseTemporaryPath(parentName);
        }
    }

    bool InitializePath()
    {
        string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
        Debug.Log($"[{gameObject.name}] 尝试查找路径父对象：{parentName}");
        
        // 直接查找
        GameObject pathParentObj = GameObject.Find(parentName);
        Transform pathParent = pathParentObj?.transform;
        
        // 如果没有找到路径父对象，创建临时路径
        if (pathParent == null)
        {
            Debug.LogError($"[{gameObject.name}] 未找到路径父对象: {parentName}，创建临时路径");
                return CreateAndUseTemporaryPath(parentName);
        }
        
        // 路径父对象存在，检查子对象
        if (pathParent.childCount == 0)
        {
            // 有父对象但没有子对象，直接创建临时路径
            Debug.LogError($"[{gameObject.name}] 路径对象{parentName}存在，但没有子对象！创建临时路径");
                return CreateAndUseTemporaryPath(parentName);
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
            Debug.Log($"找到现有路径对象: {pathName}，将清除其子对象");
            foreach (Transform child in existingPath.transform)
            {
                Destroy(child.gameObject);
            }
            
            // 创建新的路径点
            Vector3[] points = GetDefaultPathPoints(monsterType);
            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.position = points[i];
                waypoint.transform.SetParent(existingPath.transform);
            }
            
            return existingPath.transform;
        }
        else
        {
            // 创建新的路径父对象
            GameObject pathParent = new GameObject(pathName);
            
            // 创建路径点
            Vector3[] points = GetDefaultPathPoints(monsterType);
            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.position = points[i];
                waypoint.transform.SetParent(pathParent.transform);
            }
            
            return pathParent.transform;
        }
    }

    // 获取默认路径点
    private Vector3[] GetDefaultPathPoints(MonsterType type)
    {
        Vector3[] points;
        float startX = transform.position.x;
        float startY = transform.position.y;
        
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
        Debug.Log($"{gameObject.name} 到达了路径终点，准备扣除玩家生命值");
        
        // 优先使用PlayerHealth组件扣除生命值
        if (playerHealth != null)
        {
            Debug.Log($"[EnemyMovement] 通过PlayerHealth扣除玩家{damageToPlayer}点生命值");
            playerHealth.TakeDamage(damageToPlayer);
        }
        // 如果没有找到PlayerHealth组件，则通过GameManager扣除生命值
        else if (GameManager.Instance != null)
        {
            Debug.Log($"[EnemyMovement] 通过GameManager扣除玩家{damageToPlayer}点生命值");
            GameManager.Instance.PlayerTakeDamage(damageToPlayer);
        }
        else
        {
            Debug.LogError("[EnemyMovement] 无法扣除玩家生命值：未找到PlayerHealth或GameManager组件");
        }

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

    // 添加减速效果方法
    public void ApplySlow(float slowFactor, float duration)
    {
        // 减速效果
        moveSpeed = originalMoveSpeed * slowFactor;
        
        // 取消可能存在的减速恢复协程
        CancelInvoke("ResetMoveSpeed");
        
        // 设置定时器，延迟后恢复速度
        Invoke("ResetMoveSpeed", duration);
        
        // 可选：添加视觉效果表示敌人被减速
        spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
    }

    // 恢复正常速度
    private void ResetMoveSpeed()
    {
        moveSpeed = originalMoveSpeed;
        spriteRenderer.color = Color.white; // 恢复正常颜色
    }
}