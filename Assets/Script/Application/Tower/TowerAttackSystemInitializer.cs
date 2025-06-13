using UnityEngine;

/// <summary>
/// 塔防攻击系统初始化器
/// 用于在游戏启动时初始化粒子效果系统
/// </summary>
public class TowerAttackSystemInitializer : MonoBehaviour
{
    [Header("粒子效果设置")]
    public bool enableParticleEffects = true;
    public float particleEffectScale = 1.0f;
    public float particleEffectDuration = 1.0f;
    
    void Awake()
    {
        // 初始化粒子效果系统
        if (enableParticleEffects)
        {
            InitializeParticleEffects();
        }
    }
    
    /// <summary>
    /// 初始化粒子效果系统
    /// </summary>
    private void InitializeParticleEffects()
    {
        // 确保粒子效果管理器存在
        if (TowerParticleEffects.Instance != null)
        {
            // 配置粒子效果
            TowerParticleEffects.Instance.effectScale = particleEffectScale;
            TowerParticleEffects.Instance.effectDuration = particleEffectDuration;
            
            Debug.Log("粒子效果系统已初始化");
        }
        else
        {
            Debug.LogError("无法初始化粒子效果系统");
        }
    }
} 