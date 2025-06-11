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
        
        // 安全检查
        if (prefab == null)
        {
            Debug.LogError("警告：SubPool 创建时传入的预制体为null");
        }
    }

    public GameObject OnSpawn()
    {
        GameObject go = null;

        // 先检查池中是否有可复用的对象
        foreach (GameObject obj in poolList)
        {
            if (obj != null && !obj.activeSelf)
            {
                go = obj;
                Debug.Log($"对象池 {Name} 复用了现有实例 {go.name}");
                break;
            }
        }

        // 如果没有找到可复用对象，则创建新的
        if (go == null)
        {
            // 确保poolObject不为null
            if (poolObject == null)
            {
                Debug.LogError("无法创建实例：预制体为null");
                return null;
            }
            
            try
            {
            go = GameObject.Instantiate(poolObject);
            
            // 为对象添加唯一标识
            int instanceId = go.GetInstanceID();
            go.name = $"{Name}_{poolList.Count}_{instanceId}";
            
            poolList.Add(go);
            Debug.Log($"对象池 {Name} 创建了新实例 {go.name}，当前池大小：{poolList.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"实例化对象时出错：{e.Message}");
                return null;
            }
        }

        if (go == null)
        {
            Debug.LogError("无法创建实例或复用现有实例");
            return null;
        }

        // 将对象的实例ID与此池关联
        int id = go.GetInstanceID();
        if (!instanceTracker.ContainsKey(id))
        {
            instanceTracker.Add(id, go);
        }

        // 确保对象处于激活状态
        if (!go.activeSelf)
        {
        go.SetActive(true);
        }
        
        // 发送OnSpawn消息
        try
        {
        go.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"调用OnSpawn时出错：{e.Message}");
        }

        return go;
    }

    public void OnDespawn(GameObject go)
    {
        // 先检查参数是否为null
        if (go == null)
        {
            Debug.LogError("尝试回收null对象！");
            return;
        }
        
        // 使用实例ID检查对象是否在池中
        int instanceId = go.GetInstanceID();
        bool inPool = instanceTracker.ContainsKey(instanceId);
        
        // 检查对象是否在池中，增加安全检查
        if (inPool || poolList.Contains(go))
        {
            // 如果对象在列表中但不在字典中，添加到字典
            if (!inPool && poolList.Contains(go))
            {
                instanceTracker.Add(instanceId, go);
            }
            
            try
            {
                // 发送OnDespawn消息
                go.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
                // 禁用对象而不是销毁它
                go.SetActive(false);
                Debug.Log($"成功回收对象 {go.name} 到对象池 {Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"回收对象{go.name}时出现异常: {e.Message}");
                // 即使出错也尝试禁用对象
                try { go.SetActive(false); } catch {}
            }
        }
        else
        {
            // 对象不在此池中，但我们仍然尝试禁用它
            Debug.LogWarning($"对象 {go.name} 不在对象池 {Name} 中，但仍尝试禁用");
            try
            {
                go.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"禁用对象时出错：{e.Message}");
            }
            
            // 添加到池中，以便将来可能的重用
            try
            {
                poolList.Add(go);
                instanceTracker.Add(instanceId, go);
                Debug.Log($"将外部对象 {go.name} 添加到池 {Name} 中");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"添加对象到池中时出错：{e.Message}");
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
                    Debug.LogError($"回收对象{go.name}时出现异常: {e.Message}");
                }
            }
    }
    }

    public bool isContains(GameObject go)
    {
        // 先检查参数是否为null
        if (go == null)
        {
            Debug.LogError("尝试检查null对象是否在池中！");
            return false;
        }
        
        // 优先使用实例ID查找，这样更快更可靠
        int instanceId = go.GetInstanceID();
        bool instanceTracked = instanceTracker.ContainsKey(instanceId);
        
        // 如果实例ID没找到，再尝试列表查找
        bool listContains = false;
        if (!instanceTracked)
        {
            try
            {
                listContains = poolList.Contains(go);
                
                // 如果在列表中但不在字典中，添加到字典
                if (listContains)
                {
                    instanceTracker.Add(instanceId, go);
                    Debug.Log($"对象 {go.name} 在池列表中找到，但不在实例跟踪器中，已添加");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"检查对象是否在池中时出现异常: {e.Message}");
                return false;
            }
        }
        
        bool contains = instanceTracked || listContains;
        
        if (!contains)
        {
            Debug.LogWarning($"对象 {go.name} 不在对象池 {Name} 中！");
            LogPoolStatus();
        }
        
        return contains;
    }

    private void LogPoolStatus()
    {
        Debug.Log($"对象池 {Name} 当前状态：");
        Debug.Log($"- 池中对象总数：{poolList.Count}");
        Debug.Log($"- 实例追踪器中的对象数：{instanceTracker.Count}");
        int activeCount = 0;
        
        for (int i = 0; i < poolList.Count; i++)
        {
            GameObject obj = poolList[i];
            bool isActive = obj != null && obj.activeSelf;
            if (isActive) activeCount++;
            Debug.Log($"- 对象[{i}]: {(obj != null ? obj.name : "null")}, 激活状态: {isActive}");
        }
        
        Debug.Log($"- 激活对象数: {activeCount}, 非激活对象数: {poolList.Count - activeCount}");
    }
}
