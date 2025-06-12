using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 塔放置点调试工具，用于可视化放置点和鼠标位置
/// </summary>
public class TowerPlacementDebugger : MonoBehaviour
{
    [Header("参考")]
    public TowerPlacementManager placementManager;
    
    [Header("调试设置")]
    public bool drawGizmos = true;
    public float pointRadius = 0.5f;
    public Color availableColor = Color.green;
    public Color occupiedColor = Color.red;
    public Color disabledColor = Color.gray;
    public Color mousePositionColor = Color.yellow;
    public float mouseRadius = 0.3f;
    
    // 鼠标的世界坐标
    private Vector3 mouseWorldPosition;
    
    void Start()
    {
        // 查找塔放置管理器（如果未指定）
        if (placementManager == null)
        {
            placementManager = FindObjectOfType<TowerPlacementManager>();
            
            if (placementManager == null)
            {
                Debug.LogError("TowerPlacementDebugger: 未找到TowerPlacementManager!");
                return;
            }
        }
        
        // 输出放置点信息
        LogPlacementPointsInfo();
    }
    
    void Update()
    {
        // 更新鼠标位置
        Vector3 mousePosition = Input.mousePosition;
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mouseWorldPosition.z = 0; // 确保z坐标为0，与2D平面对齐
        
        // 调试鼠标位置
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"鼠标点击位置: {mouseWorldPosition}");
            
            // 查找最近的放置点
            TowerPlacementPoint nearestPoint = FindNearestPlacementPoint(mouseWorldPosition);
            if (nearestPoint != null)
            {
                float distance = Vector3.Distance(mouseWorldPosition, nearestPoint.transform.position);
                Debug.Log($"最近的放置点: {nearestPoint.pointID}, 位置: {nearestPoint.transform.position}, 距离: {distance}");
            }
            else
            {
                Debug.Log("附近没有放置点");
            }
        }
    }
    
    // 记录所有放置点的信息
    private void LogPlacementPointsInfo()
    {
        if (placementManager == null || placementManager.placementPoints == null) return;
        
        Debug.Log($"====== 塔放置点信息 ======");
        Debug.Log($"共有 {placementManager.placementPoints.Count} 个放置点");
        
        foreach (TowerPlacementPoint point in placementManager.placementPoints)
        {
            if (point == null) continue;
            
            string status = point.isEnabled ? (point.isOccupied ? "已占用" : "可用") : "已禁用";
            Debug.Log($"放置点 {point.pointID}: 位置={point.transform.position}, 网格位置={point.gridPosition}, 状态={status}");
        }
        
        Debug.Log($"=========================");
    }
    
    // 查找最近的放置点
    private TowerPlacementPoint FindNearestPlacementPoint(Vector3 worldPosition)
    {
        if (placementManager == null || placementManager.placementPoints == null) return null;
        
        TowerPlacementPoint nearestPoint = null;
        float nearestDistance = float.MaxValue;
        
        foreach (TowerPlacementPoint point in placementManager.placementPoints)
        {
            if (point == null) continue;
            
            // 只计算XY平面上的距离，忽略Z轴
            float dx = worldPosition.x - point.transform.position.x;
            float dy = worldPosition.y - point.transform.position.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }
        }
        
        return nearestPoint;
    }
    
    // 在场景视图中绘制Gizmos
    private void OnDrawGizmos()
    {
        if (!drawGizmos || placementManager == null || placementManager.placementPoints == null) return;
        
        // 绘制放置点
        foreach (TowerPlacementPoint point in placementManager.placementPoints)
        {
            if (point == null) continue;
            
            // 根据放置点状态选择颜色
            Gizmos.color = point.isEnabled ? 
                (point.isOccupied ? occupiedColor : availableColor) : 
                disabledColor;
            
            // 绘制放置点
            Gizmos.DrawWireSphere(point.transform.position, pointRadius);
            
            // 绘制ID
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(point.transform.position + Vector3.up * 0.7f, point.pointID);
            #endif
        }
        
        // 在运行时绘制鼠标位置
        if (Application.isPlaying)
        {
            Gizmos.color = mousePositionColor;
            Gizmos.DrawWireSphere(mouseWorldPosition, mouseRadius);
        }
    }
} 