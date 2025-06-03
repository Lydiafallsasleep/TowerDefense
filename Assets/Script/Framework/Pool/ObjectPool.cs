using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    public string ResourceDir = "";

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
        SubPool pool = null;
        foreach (SubPool p in poolDict.Values)
        {
            if (p.isContains(go))
            {
                pool = p;
                break;
            }
        }
        pool.OnDespawn(go);
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

        string path = "";
        if(string.IsNullOrEmpty(ResourceDir))
        {
            path = name;
        }
        else
        {
            path = ResourceDir + "/" + name;
        }

        GameObject prefab = Resources.Load<GameObject>(path);

        SubPool pool = new SubPool(prefab);
        poolDict.Add(name, pool);
    }
    
}