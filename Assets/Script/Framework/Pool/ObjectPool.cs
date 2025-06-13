using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    public string ResourceDir = "";
    
    // Add debug switch
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    Dictionary<string, SubPool> poolDict = new Dictionary<string, SubPool>();
    Dictionary<string, GameObject> tempPrefabs = new Dictionary<string, GameObject>();

    void Start()
    {
        // Output debug information at startup
        DebugAvailableResources();
    }
    
    // Debug function, list all accessible Resources
    public void DebugAvailableResources()
    {
        if (!enableDebugLogs) return;
        
        Debug.Log("===== ObjectPool Resource Diagnostics =====");
        
        // Try to load all prefabs in the enemy folder
        Object[] allEnemyObjs = Resources.LoadAll("enemy", typeof(GameObject));
        Debug.Log($"Found {allEnemyObjs.Length} GameObject resources in the enemy folder:");
        
        foreach (Object obj in allEnemyObjs)
        {
            Debug.Log($" - {obj.name} ({obj.GetType().Name})");
        }
        
        // Directly try to load Slime and Fish
        TryLoadAndReportPrefab("enemy/Slime");
        TryLoadAndReportPrefab("enemy/Fish");
        TryLoadAndReportPrefab("Slime");
        TryLoadAndReportPrefab("Fish");
        
        Debug.Log("===== Resource Diagnostics End =====");
    }
    
    // Try to load and report results
    private void TryLoadAndReportPrefab(string path)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        Debug.Log($"Resources.Load<GameObject>(\"{path}\") result: {(prefab != null ? "Success" : "Failed")}");
    }

    public GameObject OnSpawn(string name)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Trying to get from object pool: {name}");
        }
        
        if (!poolDict.ContainsKey(name))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Object pool for {name} doesn't exist, trying to register...");
            }
            RegisterSubPool(name);
        }
        
        // Ensure the pool exists, even if RegisterSubPool might fail due to missing prefab
        if (!poolDict.ContainsKey(name))
        {
            Debug.LogError($"Unable to create object pool: {name}, will create temporary object");
            CreateTemporaryPrefab(name);
            // Try to register again
            RegisterSubPool(name);
        }
        
        SubPool pool = poolDict[name];
        return pool.OnSpawn();
    }

    public void OnDespawn(GameObject go)
    {
        // Parameter safety check
        if (go == null)
        {
            Debug.LogError("Trying to recycle null object!");
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

        // Check if corresponding object pool was found
        if (pool != null)
        {
            pool.OnDespawn(go);
        }
        else
        {
            Debug.LogWarning($"Object pool for {go.name} not found, directly disabling the object");
            go.SetActive(false);
        }
    }

    // Method added for compatibility, functions the same as OnDespawn
    public void OnUnspawn(GameObject go)
    {
        OnDespawn(go);
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
        // Try multiple path formats
        Debug.Log($"Trying to register object pool for {name}, ResourceDir={ResourceDir}");
        
        GameObject prefab = null;
        List<string> pathsToTry = new List<string>
        {
            "enemy/" + name,           // Direct enemy folder: enemy/Slime
            name,                      // Direct name: Slime
            name.ToLower(),            // Lowercase name: slime
            "enemy/" + name.ToLower(), // Lowercase full path: enemy/slime
            ResourceDir + (string.IsNullOrEmpty(ResourceDir) ? "" : "/") + name  // Use configured ResourceDir
        };
        
        // Try all possible paths
        foreach (string path in pathsToTry)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Trying to load prefab: {path}");
            }
            
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"Successfully loaded prefab from path: {path}");
                break;
            }
        }
        
        // If loading fails, check temporary prefabs
        if (prefab == null && tempPrefabs.ContainsKey(name))
        {
            Debug.LogWarning($"Failed to load prefab from Resources, using temporary prefab: {name}");
            prefab = tempPrefabs[name];
        }
        
        // If still null, create temporary prefab
        if (prefab == null)
        {
            Debug.LogError($"Unable to load prefab, creating temporary prefab: {name}");
            prefab = CreateTemporaryPrefab(name);
        }
        
        if (prefab != null)
        {
        SubPool pool = new SubPool(prefab);
        poolDict.Add(name, pool);
            Debug.Log($"Successfully registered object pool for {name}");
        }
        else
        {
            Debug.LogError($"Cannot create object pool for {name}: prefab is still null");
        }
    }
    
    // Create temporary prefab
    private GameObject CreateTemporaryPrefab(string name)
    {
        Debug.Log($"Creating temporary prefab: {name}");
        
        // Check if already exists
        if (tempPrefabs.ContainsKey(name))
        {
            return tempPrefabs[name];
        }
        
        // Create simple prefab
        GameObject tempObj = new GameObject(name + "_TempPrefab");
        
        // Add components
        if (name == "Slime" || name == "Fish")
        {
            // Add basic components
            tempObj.AddComponent<SpriteRenderer>();
            var rb = tempObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            
            // Add enemy components
            var movement = tempObj.AddComponent<EnemyMovement>();
            movement.monsterType = name == "Slime" ? 
                EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish;
                
            tempObj.AddComponent<EnemyHealth>();
            tempObj.AddComponent<EnemyPoolObject>();
            
            // Set tag
            tempObj.tag = "Enemy";
            
            // Set color
            var renderer = tempObj.GetComponent<SpriteRenderer>();
            renderer.color = name == "Slime" ? Color.green : Color.blue;
            
            Debug.Log($"Created temporary {name} prefab");
        }
        
        // Hide object and prevent destruction
        tempObj.SetActive(false);
        DontDestroyOnLoad(tempObj);
        
        // Save reference
        tempPrefabs[name] = tempObj;
        
        return tempObj;
    }
}