using UnityEngine;
using System.Collections;

/// <summary>
/// 防御塔粒子效果管理器
/// 用于生成和控制不同类型防御塔的攻击粒子效果
/// </summary>
public class TowerParticleEffects : MonoBehaviour
{
    [Header("箭塔特效")]
    public GameObject arrowImpactPrefab;        // 箭矢命中特效
    public GameObject arrowTrailPrefab;         // 箭矢轨迹特效
    public Color arrowEffectColor = new Color(0.5f, 0.8f, 1f); // 蓝色调
    
    [Header("激光塔特效")]
    public GameObject laserBeamPrefab;          // 激光束特效
    public GameObject laserImpactPrefab;        // 激光命中特效
    public Color laserEffectColor = new Color(0.2f, 1f, 0.5f); // 绿色调
    
    [Header("炮塔特效")]
    public GameObject explosionPrefab;          // 爆炸特效
    public GameObject muzzleFlashPrefab;        // 炮口闪光特效
    public GameObject cannonballTrailPrefab;    // 炮弹轨迹特效
    public Color explosionEffectColor = new Color(1f, 0.6f, 0.2f); // 橙色调
    
    [Header("通用设置")]
    public float effectDuration = 1.0f;         // 特效持续时间
    public float effectScale = 1.0f;            // 特效缩放
    
    // 单例模式
    private static TowerParticleEffects _instance;
    public static TowerParticleEffects Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TowerParticleEffects>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TowerParticleEffects");
                    _instance = obj.AddComponent<TowerParticleEffects>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeParticleEffects();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化粒子效果
    /// </summary>
    private void InitializeParticleEffects()
    {
        // 如果没有预设，则创建默认的粒子系统
        if (arrowImpactPrefab == null)
            arrowImpactPrefab = CreateDefaultParticleSystem("ArrowImpact", arrowEffectColor);
            
        if (arrowTrailPrefab == null)
            arrowTrailPrefab = CreateTrailEffect("ArrowTrail", arrowEffectColor);
            
        if (laserBeamPrefab == null)
            laserBeamPrefab = CreateLaserBeamEffect(laserEffectColor);
            
        if (laserImpactPrefab == null)
            laserImpactPrefab = CreateDefaultParticleSystem("LaserImpact", laserEffectColor);
            
        if (explosionPrefab == null)
            explosionPrefab = CreateExplosionEffect(explosionEffectColor);
            
        if (muzzleFlashPrefab == null)
            muzzleFlashPrefab = CreateMuzzleFlashEffect(explosionEffectColor);
            
        if (cannonballTrailPrefab == null)
            cannonballTrailPrefab = CreateTrailEffect("CannonballTrail", explosionEffectColor);
    }
    
    /// <summary>
    /// 创建默认粒子系统
    /// </summary>
    private GameObject CreateDefaultParticleSystem(string name, Color color)
    {
        GameObject particleObj = new GameObject(name);
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        
        // 先停止粒子系统，确保可以安全设置duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 配置粒子系统主模块
        var main = ps.main;
        main.startColor = color;
        main.startSize = 0.3f;
        main.startSpeed = 2f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.5f;
        main.loop = false;
        
        // 配置发射器
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 20)
        });
        
        // 配置形状
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        // 配置大小随时间变化
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        // 配置颜色随时间变化
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(color, 0.0f), 
                new GradientColorKey(color, 0.7f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // 添加渲染器
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // 默认禁用并设为预制体
        particleObj.SetActive(false);
        DontDestroyOnLoad(particleObj);
        
        return particleObj;
    }
    
    /// <summary>
    /// 创建轨迹特效
    /// </summary>
    private GameObject CreateTrailEffect(string name, Color color)
    {
        GameObject trailObj = new GameObject(name);
        TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
        
        trail.startWidth = 0.1f;
        trail.endWidth = 0.02f;
        trail.time = 0.3f; // 轨迹持续时间
        
        // 设置渐变颜色
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(color, 0.0f), 
                new GradientColorKey(color, 0.7f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        trail.colorGradient = gradient;
        
        // 设置材质
        trail.material = new Material(Shader.Find("Sprites/Default"));
        
        // 默认禁用并设为预制体
        trailObj.SetActive(false);
        DontDestroyOnLoad(trailObj);
        
        return trailObj;
    }
    
    /// <summary>
    /// 创建激光束特效
    /// </summary>
    private GameObject CreateLaserBeamEffect(Color color)
    {
        GameObject laserObj = new GameObject("LaserBeam");
        LineRenderer laser = laserObj.AddComponent<LineRenderer>();
        
        laser.startWidth = 0.1f;
        laser.endWidth = 0.1f;
        laser.positionCount = 2;
        laser.useWorldSpace = true;
        
        // 设置材质
        laser.material = new Material(Shader.Find("Sprites/Default"));
        laser.startColor = color;
        laser.endColor = color;
        
        // 默认禁用并设为预制体
        laserObj.SetActive(false);
        DontDestroyOnLoad(laserObj);
        
        return laserObj;
    }
    
    /// <summary>
    /// 创建爆炸特效
    /// </summary>
    private GameObject CreateExplosionEffect(Color color)
    {
        GameObject explosionObj = new GameObject("Explosion");
        ParticleSystem ps = explosionObj.AddComponent<ParticleSystem>();
        
        // 先停止粒子系统，确保可以安全设置duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 配置粒子系统主模块
        var main = ps.main;
        main.startColor = color;
        main.startSize = 0.5f;
        main.startSpeed = 5f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.5f;
        main.loop = false;
        
        // 配置发射器
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 50)
        });
        
        // 配置形状
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        
        // 配置大小随时间变化
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(0.2f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        // 配置颜色随时间变化
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0.0f), 
                new GradientColorKey(color, 0.2f),
                new GradientColorKey(Color.red, 0.7f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // 添加渲染器
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // 默认禁用并设为预制体
        explosionObj.SetActive(false);
        DontDestroyOnLoad(explosionObj);
        
        return explosionObj;
    }
    
    /// <summary>
    /// 创建炮口闪光特效
    /// </summary>
    private GameObject CreateMuzzleFlashEffect(Color color)
    {
        GameObject muzzleObj = new GameObject("MuzzleFlash");
        ParticleSystem ps = muzzleObj.AddComponent<ParticleSystem>();
        
        // 先停止粒子系统，确保可以安全设置duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 配置粒子系统主模块
        var main = ps.main;
        main.startColor = Color.yellow;
        main.startSize = 0.5f;
        main.startSpeed = 0f;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.2f;
        main.loop = false;
        
        // 配置发射器
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 5)
        });
        
        // 配置形状
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        
        // 配置大小随时间变化
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        // 配置颜色随时间变化
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0.0f), 
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(color, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // 添加渲染器
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // 默认禁用并设为预制体
        muzzleObj.SetActive(false);
        DontDestroyOnLoad(muzzleObj);
        
        return muzzleObj;
    }
    
    /// <summary>
    /// 播放箭矢命中特效
    /// </summary>
    public void PlayArrowImpactEffect(Vector3 position)
    {
        PlayParticleEffect(arrowImpactPrefab, position, effectDuration);
    }
    
    /// <summary>
    /// 添加箭矢轨迹特效
    /// </summary>
    public GameObject AddArrowTrailEffect(GameObject arrow)
    {
        if (arrow == null || arrowTrailPrefab == null)
            return null;
            
        GameObject trail = Instantiate(arrowTrailPrefab, arrow.transform.position, Quaternion.identity);
        trail.transform.SetParent(arrow.transform);
        trail.SetActive(true);
        
        return trail;
    }
    
    /// <summary>
    /// 播放激光命中特效
    /// </summary>
    public void PlayLaserImpactEffect(Vector3 position)
    {
        PlayParticleEffect(laserImpactPrefab, position, effectDuration);
    }
    
    /// <summary>
    /// 创建激光束特效
    /// </summary>
    public GameObject CreateLaserBeamEffect(Vector3 startPosition, Vector3 endPosition)
    {
        if (laserBeamPrefab == null)
            return null;
            
        GameObject laserBeam = Instantiate(laserBeamPrefab, startPosition, Quaternion.identity);
        LineRenderer lineRenderer = laserBeam.GetComponent<LineRenderer>();
        
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);
        }
        
        laserBeam.SetActive(true);
        
        return laserBeam;
    }
    
    /// <summary>
    /// 播放爆炸特效
    /// </summary>
    public void PlayExplosionEffect(Vector3 position, float scale = 1.0f)
    {
        PlayParticleEffect(explosionPrefab, position, effectDuration, scale);
    }
    
    /// <summary>
    /// 播放炮口闪光特效
    /// </summary>
    public void PlayMuzzleFlashEffect(Vector3 position, Quaternion rotation)
    {
        if (muzzleFlashPrefab == null)
            return;
            
        GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, position, rotation);
        muzzleFlash.SetActive(true);
        
        Destroy(muzzleFlash, 0.2f);
    }
    
    /// <summary>
    /// 添加炮弹轨迹特效
    /// </summary>
    public GameObject AddCannonballTrailEffect(GameObject cannonball)
    {
        if (cannonball == null || cannonballTrailPrefab == null)
            return null;
            
        GameObject trail = Instantiate(cannonballTrailPrefab, cannonball.transform.position, Quaternion.identity);
        trail.transform.SetParent(cannonball.transform);
        trail.SetActive(true);
        
        return trail;
    }
    
    /// <summary>
    /// 通用方法：播放粒子特效
    /// </summary>
    private void PlayParticleEffect(GameObject effectPrefab, Vector3 position, float duration, float scale = 1.0f)
    {
        if (effectPrefab == null)
            return;
            
        GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * scale * effectScale;
        effect.SetActive(true);
        
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // 确保粒子系统播放
            ps.Play();
            
            // 根据粒子系统的实际持续时间销毁对象
            float actualDuration = Mathf.Max(ps.main.duration, duration);
            Destroy(effect, actualDuration);
        }
        else
        {
            // 如果不是粒子系统，使用默认持续时间
            Destroy(effect, duration);
        }
    }
}
