using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public float explosionRadius = 0f; // 0表示单体伤害，>0表示范围伤害
    public GameObject impactEffect;
    
    private Transform target;

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

        // 向目标移动
        Vector3 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // 检查是否足够接近以造成伤害
        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        // 移动弹药
        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
        
        // 让弹药朝向目标
        transform.LookAt(target);
        transform.Rotate(new Vector3(0, -90, 0)); // 调整精灵朝向
    }

    void HitTarget()
    {
        // 创建击中特效
        if (impactEffect != null)
        {
            GameObject effectInstance = Instantiate(impactEffect, transform.position, transform.rotation);
            Destroy(effectInstance, 2f); // 2秒后销毁特效
        }

        // 处理范围伤害
        if (explosionRadius > 0f)
        {
            Explode();
        }
        else
        {
            // 单体伤害
            Damage(target);
        }

        Destroy(gameObject);
    }

    void Explode()
    {
        // 获取范围内的所有碰撞体
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Damage(collider.transform);
            }
        }
    }

    protected virtual void Damage(Transform enemy)
    {
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }

    // 在场景视图中绘制爆炸范围
    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
} 