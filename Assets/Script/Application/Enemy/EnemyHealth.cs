using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("死亡设置")]
    public int goldReward = 10;
    public GameObject deathEffect;
    
    private bool isDead = false;
    
    void OnEnable()
    {
        // 在启用时重置生命值
        currentHealth = maxHealth;
        isDead = false;
    }
    
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // 添加金币
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.AddGold(goldReward);
        }
        
        // 播放死亡特效
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后销毁特效
        }
        
        // 回收敌人对象到对象池
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.OnDespawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
} 