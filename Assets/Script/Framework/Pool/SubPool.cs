using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SubPool
{
    GameObject poolObject;

    List<GameObject> poolList = new List<GameObject>();
    Dictionary<int, GameObject> instanceTracker = new Dictionary<int, GameObject>();

    public string Name 
    { 
        get{return poolObject != null ? poolObject.name : "NullPool";} 
    }

    public SubPool(GameObject prefab)
    {
        this.poolObject = prefab;
        
        // Safety check
        if (prefab == null)
        {
            Debug.LogError("Warning: SubPool created with null prefab");
        }
    }

    public GameObject OnSpawn()
    {
        GameObject go = null;

        // First check if there are reusable objects in the pool
        foreach (GameObject obj in poolList)
        {
            if (obj != null && !obj.activeSelf)
            {
                go = obj;
                Debug.Log($"Object pool {Name} reused existing instance {go.name}");
                break;
            }
        }

        // If no reusable object was found, create a new one
        if (go == null)
        {
            // Ensure poolObject is not null
            if (poolObject == null)
            {
                Debug.LogError("Cannot create instance: prefab is null");
                return null;
            }
            
            try
            {
            go = GameObject.Instantiate(poolObject);
            
            // Add unique identifier to the object
            int instanceId = go.GetInstanceID();
            go.name = $"{Name}_{poolList.Count}_{instanceId}";
            
            poolList.Add(go);
            Debug.Log($"Object pool {Name} created new instance {go.name}, current pool size: {poolList.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error instantiating object: {e.Message}");
                return null;
            }
        }

        if (go == null)
        {
            Debug.LogError("Unable to create instance or reuse existing instance");
            return null;
        }

        // Associate the object's instance ID with this pool
        int id = go.GetInstanceID();
        if (!instanceTracker.ContainsKey(id))
        {
            instanceTracker.Add(id, go);
        }

        // Ensure the object is active
        if (!go.activeSelf)
        {
        go.SetActive(true);
        }
        
        // Send OnSpawn message
        try
        {
        go.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error calling OnSpawn: {e.Message}");
        }

        return go;
    }

    public void OnDespawn(GameObject go)
    {
        // First check if the parameter is null
        if (go == null)
        {
            Debug.LogError("Trying to recycle null object!");
            return;
        }
        
        // Use instance ID to check if the object is in the pool
        int instanceId = go.GetInstanceID();
        bool inPool = instanceTracker.ContainsKey(instanceId);
        
        // Check if the object is in the pool, add additional safety check
        if (inPool || poolList.Contains(go))
        {
            // If the object is in the list but not in the dictionary, add it to the dictionary
            if (!inPool && poolList.Contains(go))
            {
                instanceTracker.Add(instanceId, go);
            }
            
            try
            {
                // Send OnDespawn message
                go.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
                // Disable the object instead of destroying it
                go.SetActive(false);
                Debug.Log($"Successfully recycled object {go.name} to object pool {Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception occurred while recycling object {go.name}: {e.Message}");
                // Try to disable the object even if an error occurs
                try { go.SetActive(false); } catch {}
            }
        }
        else
        {
            // Object is not in this pool, but we still try to disable it
            Debug.LogWarning($"Object {go.name} is not in object pool {Name}, but still attempting to disable");
            try
            {
                go.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error disabling object: {e.Message}");
            }
            
            // Add to the pool for possible future reuse
            try
            {
                poolList.Add(go);
                instanceTracker.Add(instanceId, go);
                Debug.Log($"Added external object {go.name} to pool {Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error adding object to pool: {e.Message}");
            }
        }
    }

    public void RecycleAll()
    {
        foreach (GameObject go in poolList)
        {
            if (go != null && go.activeSelf)
            {
                try
            {
                go.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
                go.SetActive(false);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Exception occurred while recycling object {go.name}: {e.Message}");
                }
            }
    }
    }

    public bool isContains(GameObject go)
    {
        // First check if the parameter is null
        if (go == null)
        {
            Debug.LogError("Trying to check if null object is in the pool!");
            return false;
        }
        
        // First use instance ID lookup, which is faster and more reliable
        int instanceId = go.GetInstanceID();
        bool instanceTracked = instanceTracker.ContainsKey(instanceId);
        
        // If instance ID not found, try list lookup
        bool listContains = false;
        if (!instanceTracked)
        {
            try
            {
                listContains = poolList.Contains(go);
                
                // If in the list but not in the dictionary, add to the dictionary
                if (listContains)
                {
                    instanceTracker.Add(instanceId, go);
                    Debug.Log($"Object {go.name} found in pool list but not in instance tracker, added");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception occurred while checking if object is in pool: {e.Message}");
                return false;
            }
        }
        
        bool contains = instanceTracked || listContains;
        
        if (!contains)
        {
            Debug.LogWarning($"Object {go.name} is not in object pool {Name}!");
            LogPoolStatus();
        }
        
        return contains;
    }

    private void LogPoolStatus()
    {
        Debug.Log($"Object pool {Name} current status:");
        Debug.Log($"- Total objects in pool: {poolList.Count}");
        Debug.Log($"- Objects in instance tracker: {instanceTracker.Count}");
        int activeCount = 0;
        
        for (int i = 0; i < poolList.Count; i++)
        {
            GameObject obj = poolList[i];
            bool isActive = obj != null && obj.activeSelf;
            if (isActive) activeCount++;
            Debug.Log($"- Object[{i}]: {(obj != null ? obj.name : "null")}, Active: {isActive}");
        }
        
        Debug.Log($"- Active objects: {activeCount}, Inactive objects: {poolList.Count - activeCount}");
    }
}
