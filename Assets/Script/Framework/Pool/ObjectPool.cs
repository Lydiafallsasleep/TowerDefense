using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    public string ResourceDir = "enemy";

    Dictionary<string, SubPool> poolDict = new Dictionary<string, SubPool>();

    public GameObject OnSpawn(string name)
    {
        if (!poolDict.ContainsKey(name))
        {
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
        string path = ResourceDir + "/" + name;

        GameObject prefab = Resources.Load<GameObject>(path);

        SubPool pool = new SubPool(prefab);
        poolDict.Add(name, pool);
    }
    
}