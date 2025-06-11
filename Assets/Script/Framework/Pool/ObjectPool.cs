using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    public string ResourceDir = "";
    
    // 添加调试开关
    [Header("调试设置")]
    public bool enableDebugLogs = true;

    Dictionary<string, SubPool> poolDict = new Dictionary<string, SubPool>();
    Dictionary<string, GameObject> tempPrefabs = new Dictionary<string, GameObject>();

    void Start()
    {
        // 在启动时输出调试信息
        DebugAvailableResources();
    }
    
    // 调试函数，列出所有可访问的Resources资源
    public void DebugAvailableResources()
    {
        if (!enableDebugLogs) return;
        
        Debug.Log("===== ObjectPool资源诊断 =====");
        
        // 尝试加载enemy文件夹下的所有预制体
        Object[] allEnemyObjs = Resources.LoadAll("enemy", typeof(GameObject));
        Debug.Log($"在enemy文件夹下找到{allEnemyObjs.Length}个GameObject资源:");
        
        foreach (Object obj in allEnemyObjs)
        {
            Debug.Log($" - {obj.name} ({obj.GetType().Name})");
        }
        
        // 直接尝试加载Slime和Fish
        TryLoadAndReportPrefab("enemy/Slime");
        TryLoadAndReportPrefab("enemy/Fish");
        TryLoadAndReportPrefab("Slime");
        TryLoadAndReportPrefab("Fish");
        
        Debug.Log("===== 资源诊断结束 =====");
    }
    
    // 尝试加载并报告结果
    private void TryLoadAndReportPrefab(string path)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        Debug.Log($"Resources.Load<GameObject>(\"{path}\") 结果: {(prefab != null ? "成功" : "失败")}");
    }

    public GameObject OnSpawn(string name)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"尝试从对象池获取: {name}");
        }
        
        if (!poolDict.ContainsKey(name))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"对象池中不存在{name}，尝试注册...");
            }
            RegisterSubPool(name);
        }
        
        // 确保池存在，即使RegisterSubPool可能因为缺少预制体而失败
        if (!poolDict.ContainsKey(name))
        {
            Debug.LogError($"无法创建对象池: {name}，将创建临时对象");
            CreateTemporaryPrefab(name);
            // 再次尝试注册
            RegisterSubPool(name);
        }
        
        SubPool pool = poolDict[name];
        return pool.OnSpawn();
    }

    public void OnDespawn(GameObject go)
    {
        // 参数安全检查
        if (go == null)
        {
            Debug.LogError("尝试回收null对象！");
            return;
        }

        SubPool pool = null;
        foreach (SubPool p in poolDict.Values)
        {
            if (p != null && p.isContains(go))
            {
                pool = p;
                break;
            }
        }

        // 检查是否找到了对应的对象池
        if (pool != null)
        {
            pool.OnDespawn(go);
        }
        else
        {
            Debug.LogWarning($"未找到对象 {go.name} 所属的对象池，直接禁用该对象");
            go.SetActive(false);
        }
    }

    public void RecycleAll()
    {
        foreach (SubPool p in poolDict.Values)
        {
            p.RecycleAll();
        }
    }

    void RegisterSubPool(string name)
    {
        // 尝试多种路径格式
        Debug.Log($"尝试为{name}注册对象池，ResourceDir={ResourceDir}");
        
        GameObject prefab = null;
        List<string> pathsToTry = new List<string>
        {
            "enemy/" + name,           // 直接用enemy文件夹: enemy/Slime
            name,                      // 直接名称: Slime
            name.ToLower(),            // 小写名称: slime
            "enemy/" + name.ToLower(), // 小写完整路径: enemy/slime
            ResourceDir + (string.IsNullOrEmpty(ResourceDir) ? "" : "/") + name  // 使用配置的ResourceDir
        };
        
        // 尝试所有可能的路径
        foreach (string path in pathsToTry)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"尝试加载预制体: {path}");
            }
            
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"成功从路径加载预制体: {path}");
                break;
            }
        }
        
        // 如果加载失败，检查临时预制体
        if (prefab == null && tempPrefabs.ContainsKey(name))
        {
            Debug.LogWarning($"从Resources加载预制体失败，使用临时预制体: {name}");
            prefab = tempPrefabs[name];
        }
        
        // 如果仍为null，创建临时预制体
        if (prefab == null)
        {
            Debug.LogError($"无法加载预制体，创建临时预制体: {name}");
            prefab = CreateTemporaryPrefab(name);
        }
        
        if (prefab != null)
        {
        SubPool pool = new SubPool(prefab);
        poolDict.Add(name, pool);
            Debug.Log($"成功为 {name} 注册对象池");
        }
        else
        {
            Debug.LogError($"无法为 {name} 创建对象池：预制体仍为null");
        }
    }
    
    // 创建临时预制体
    private GameObject CreateTemporaryPrefab(string name)
    {
        Debug.Log($"创建临时预制体: {name}");
        
        // 检查是否已存在
        if (tempPrefabs.ContainsKey(name))
        {
            return tempPrefabs[name];
        }
        
        // 创建简单预制体
        GameObject tempObj = new GameObject(name + "_TempPrefab");
        
        // 添加组件
        if (name == "Slime" || name == "Fish")
        {
            // 添加基本组件
            tempObj.AddComponent<SpriteRenderer>();
            var rb = tempObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            
            // 添加敌人组件
            var movement = tempObj.AddComponent<EnemyMovement>();
            movement.monsterType = name == "Slime" ? 
                EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish;
                
            tempObj.AddComponent<EnemyHealth>();
            tempObj.AddComponent<EnemyPoolObject>();
            
            // 设置标签
            tempObj.tag = "Enemy";
            
            // 设置颜色
            var renderer = tempObj.GetComponent<SpriteRenderer>();
            renderer.color = name == "Slime" ? Color.green : Color.blue;
            
            Debug.Log($"已创建临时{name}预制体");
        }
        
        // 隐藏对象并防止销毁
        tempObj.SetActive(false);
        DontDestroyOnLoad(tempObj);
        
        // 保存引用
        tempPrefabs[name] = tempObj;
        
        return tempObj;
    }
}