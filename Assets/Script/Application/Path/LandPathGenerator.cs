using UnityEngine;

public class LandPathGenerator : PathGenerator
{
    void Reset()
    {
        // Set default values for land path
        pathType = PathType.Land;
        gizmoColor = new Color(0.6f, 0.4f, 0.2f); // Brown color for land
        gizmoRadius = 0.3f;
        waypointInterval = 1f;
    }

    protected override void Start()
    {
        // 如果没有指定路径父对象，尝试找到或创建一个
        if (pathParent == null)
        {
            GameObject pathParentObj = GameObject.Find("LandPathParent");
            pathParent = pathParentObj?.transform;
            
            if (pathParent == null)
            {
                Debug.LogWarning("没有找到LandPathParent，将创建一个新的路径父对象");
                GameObject parentObject = new GameObject("LandPathParent");
                pathParent = parentObject.transform;
            }
            
            Debug.Log($"LandPathGenerator使用路径父对象: {pathParent.name}");
        }
        
        // 调用基类的Start方法
        base.Start();
    }

    [ContextMenu("Generate Land Path")]
    public void GenerateLandPath()
    {
        // 先检查pathParent是否还存在，可能已经被清理时替换了
        if (pathParent == null || pathParent.gameObject == null)
        {
            GameObject pathParentObj = GameObject.Find("LandPathParent");
            
            if (pathParentObj == null)
            {
                pathParentObj = new GameObject("LandPathParent");
                Debug.Log("创建新的LandPathParent");
            }
            
            pathParent = pathParentObj.transform;
            Debug.Log($"更新路径父对象引用: {pathParent.name}");
        }
        
        base.GenerateWaypoints();
        
        // 对生成完毕后的路径父对象重新获取引用
        GameObject currentPathObj = GameObject.Find("LandPathParent");
        if (currentPathObj != null && currentPathObj.transform != pathParent)
        {
            Debug.Log("路径父对象已被替换，更新引用");
            pathParent = currentPathObj.transform;
        }
    }
}