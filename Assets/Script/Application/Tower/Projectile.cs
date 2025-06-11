using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 20f;
    public float explosionRadius = 0f; // 爆炸半径，0表示无爆炸
    public bool hasSlowEffect = false; // 减速效果
    public float slowFactor = 0.5f;   // 减速因子
    public float slowDuration = 1f;   // 减速持续时间
    
    protected Transform target;

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 移动逻辑
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
        
        // 2D游戏中使用LookAt2D
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    // 改为protected virtual方法
    protected virtual void HitTarget()
    {
        if (explosionRadius > 0f)
        {
            Explode(); // 爆炸伤害
        }
        else
        {
            Damage(target); // 单体伤害
        }

        Destroy(gameObject);
    }

    // 改为protected virtual方法
    protected virtual void Explode()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Damage(collider.transform);
            }
        }
    }

    // 修改为protected virtual方法，以便子类可以重写
    protected virtual void Damage(Transform enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
    }

        // 应用减速效果
        if (hasSlowEffect)
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.ApplySlow(slowFactor, slowDuration);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
} 