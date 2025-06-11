using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 相机设置工具，用于确保相机能够进行射线检测
/// </summary>
public class CameraSetup : MonoBehaviour
{
    void Start()
    {
        // 确保相机有Physics Raycaster组件
        SetupPhysicsRaycaster();
        
        Debug.Log("相机射线检测设置已完成");
    }
    
    /// <summary>
    /// 为相机添加Physics Raycaster组件，确保能够进行射线检测
    /// </summary>
    public void SetupPhysicsRaycaster()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("没有找到Camera组件!");
            return;
        }
        
        // 添加PhysicsRaycaster如果不存在
        PhysicsRaycaster physicsRaycaster = cam.GetComponent<PhysicsRaycaster>();
        if (physicsRaycaster == null)
        {
            physicsRaycaster = cam.gameObject.AddComponent<PhysicsRaycaster>();
            Debug.Log("已添加PhysicsRaycaster组件到相机");
        }
        
        // 设置射线检测掩码（可选）
        // physicsRaycaster.eventMask = LayerMask.GetMask("UI", "Obstacle");
        
        // 确保EventSystem存在
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("已创建EventSystem");
        }
    }
} 