using UnityEngine;

public class EnemyMovement2D : MonoBehaviour
{
    public enum MonsterType { Slime, Fish }

    [Header("Settings")]
    public MonsterType monsterType;
    public float moveSpeed = 2f;
    public float waypointThreshold = 0.1f;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        InitializePath();
    }

    void InitializePath()
    {
        string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
        Transform pathParent = GameObject.Find(parentName)?.transform;

        if (pathParent == null || pathParent.childCount == 0)
        {
            Debug.LogError($"Path not found for {monsterType}!");
            gameObject.SetActive(false);
            return;
        }

        waypoints = new Transform[pathParent.childCount];
        for (int i = 0; i < pathParent.childCount; i++)
        {
            waypoints[i] = pathParent.GetChild(i);
        }
        currentWaypointIndex = 0;
    }

    void FixedUpdate()
    {
        if (currentWaypointIndex < waypoints.Length)
        {
            Vector2 direction = (waypoints[currentWaypointIndex].position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 2D朝向移动方向
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < waypointThreshold)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            ReachedEnd();
        }
    }

    void ReachedEnd()
    {
        // 通知游戏扣减生命值等
        //GameManager.Instance.PlayerTakeDamage(1);

        // 回收到对象池
        EnemyPool.Instance.ReturnToPool(this.gameObject);
    }
}