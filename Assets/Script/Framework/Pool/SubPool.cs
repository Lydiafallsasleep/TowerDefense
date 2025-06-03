using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SubPool : MonoBehaviour
{
    GameObject poolObject;

    List<GameObject> poolList = new List<GameObject>();

    public string Name 
    { 
        get{return poolObject.name;} 
    }

    public SubPool(GameObject prefab)
    {
        this.poolObject = prefab;
    }

    public GameObject OnSpawn()
    {
        GameObject go = null;

        foreach (GameObject obj in poolList)
        {
            if (!obj.activeSelf)
            {
                go = obj;
                break;
            }
        }

        if (go == null)
        {
            go = GameObject.Instantiate(poolObject);
            poolList.Add(go);
        }

        go.SetActive(true);
        go.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver);

        return go;
    }

    public void OnDespawn(GameObject go)
    {
        if (!isContains(go))
        {
            go.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
            go.SetActive(false);
        }

    }

    public void RecycleAll()
    {
        foreach (GameObject go in poolList)
        {
            if (go.activeSelf)
            {
                go.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
                go.SetActive(false);
            }
    }
    }

    public bool isContains(GameObject go)
    {
        return poolList.Contains(go);
    }
}
