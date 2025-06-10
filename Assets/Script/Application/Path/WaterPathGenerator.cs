using UnityEngine;

public class WaterPathGenerator : PathGenerator
{
    void Reset()
    {
        // Set default values for water path
        pathType = PathType.Water;
        gizmoColor = new Color(0.2f, 0.4f, 0.8f); // Blue color for water
        gizmoRadius = 0.4f; // Slightly larger for visibility
        waypointInterval = 1.2f; // Longer interval for smoother water movement
    }

    protected override void Start()
    {
        // 如果没有指定路径父对象，尝试找到或创建一个
        if (pathParent == null)
        {
            GameObject pathParentObj = GameObject.Find("WaterPathParent");
            pathParent = pathParentObj?.transform;
            
            if (pathParent == null)
            {
                Debug.LogWarning("没有找到WaterPathParent，将创建一个新的路径父对象");
                GameObject parentObject = new GameObject("WaterPathParent");
                pathParent = parentObject.transform;
            }
            
            Debug.Log($"WaterPathGenerator使用路径父对象: {pathParent.name}");
        }
        
        // 调用基类的Start方法
        base.Start();
    }

    [ContextMenu("Generate Water Path")]
    public void GenerateWaterPath()
    {
        // 先检查pathParent是否还存在，可能已经被清理时替换了
        if (pathParent == null || pathParent.gameObject == null)
        {
            GameObject pathParentObj = GameObject.Find("WaterPathParent");
            
            if (pathParentObj == null)
            {
                pathParentObj = new GameObject("WaterPathParent");
                Debug.Log("创建新的WaterPathParent");
            }
            
            pathParent = pathParentObj.transform;
            Debug.Log($"更新路径父对象引用: {pathParent.name}");
        }
        
        base.GenerateWaypoints();
        
        // 对生成完毕后的路径父对象重新获取引用
        GameObject currentPathObj = GameObject.Find("WaterPathParent");
        if (currentPathObj != null && currentPathObj.transform != pathParent)
        {
            Debug.Log("路径父对象已被替换，更新引用");
            pathParent = currentPathObj.transform;
        }
    }
}