using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理塔防放置点，提供访问和管理所有放置点的功能
/// </summary>
public class TowerPlacementManager : MonoBehaviour
{
    // 单例实例
    public static TowerPlacementManager Instance { get; private set; }
    
    // 所有放置点的列表
    public List<TowerPlacementPoint> placementPoints = new List<TowerPlacementPoint>();
    
    // 记录放置点的字典，便于快速查找
    private Dictionary<string, TowerPlacementPoint> pointsById = new Dictionary<string, TowerPlacementPoint>();
    private Dictionary<Vector3Int, TowerPlacementPoint> pointsByGrid = new Dictionary<Vector3Int, TowerPlacementPoint>();
    
    [Header("高亮显示设置")]
    public Color availableColor = Color.green;  // 可用放置点颜色
    public Color unavailableColor = Color.red;  // 不可用放置点颜色
    public float highlightIntensity = 1.5f;     // 高亮强度
    public bool highlightAvailablePoints = false; // 是否自动高亮显示可用放置点
    private bool isHighlighting = false;        // 是否正在高亮显示
    
    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("发现多个TowerPlacementManager实例，正在销毁重复实例。");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 初始化时查找所有已存在的放置点
        FindAllPlacementPoints();
    }
    
    // 查找并注册所有场景中的放置点
    public void FindAllPlacementPoints()
    {
        placementPoints.Clear();
        pointsById.Clear();
        pointsByGrid.Clear();
        
        // 查找场景中所有的放置点
        TowerPlacementPoint[] points = FindObjectsOfType<TowerPlacementPoint>();
        
        foreach (var point in points)
        {
            RegisterPlacementPoint(point);
        }
        
        Debug.Log($"TowerPlacementManager: 找到{placementPoints.Count}个放置点");
    }
    
    // 注册一个放置点
    public void RegisterPlacementPoint(TowerPlacementPoint point)
    {
        if (point == null) return;
        
        // 添加到列表
        if (!placementPoints.Contains(point))
        {
            placementPoints.Add(point);
            
            // 添加到字典，便于快速查找
            if (!string.IsNullOrEmpty(point.pointID) && !pointsById.ContainsKey(point.pointID))
            {
                pointsById[point.pointID] = point;
            }
            
            // 添加到网格字典
            if (!pointsByGrid.ContainsKey(point.gridPosition))
            {
                pointsByGrid[point.gridPosition] = point;
            }
        }
    }
    
    // 创建一个新的放置点
    public TowerPlacementPoint CreatePlacementPoint(Vector3 worldPos, Vector3Int gridPos, string pointID)
    {
        // 创建物体
        GameObject pointObj = new GameObject($"PlacementPoint_{pointID}");
        pointObj.transform.position = worldPos;
        
        // 添加TowerPlacementPoint组件
        TowerPlacementPoint point = pointObj.AddComponent<TowerPlacementPoint>();
        point.gridPosition = gridPos;
        point.pointID = pointID;
        
        // 注册放置点
        RegisterPlacementPoint(point);
        
        return point;
    }
    
    // 根据ID获取放置点
    public TowerPlacementPoint GetPlacementPointByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        
        if (pointsById.ContainsKey(id))
        {
            return pointsById[id];
        }
        
        return null;
    }
    
    // 根据网格坐标获取放置点
    public TowerPlacementPoint GetPlacementPointByGrid(Vector3Int gridPos)
    {
        if (pointsByGrid.ContainsKey(gridPos))
        {
            return pointsByGrid[gridPos];
        }
        
        return null;
    }
    
    // 获取最近的可用放置点
    public TowerPlacementPoint GetNearestAvailablePoint(Vector3 worldPosition)
    {
        TowerPlacementPoint nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var point in placementPoints)
        {
            // 检查放置点是否可用
            if (point != null && point.isEnabled && !point.isOccupied)
            {
                float distance = Vector3.Distance(worldPosition, point.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = point;
                }
            }
        }
        
        return nearest;
    }
    
    // 检查是否可以在指定的网格位置放置塔
    public bool CanPlaceTowerAt(Vector3Int gridPos)
    {
        // 检查这个位置是否有放置点
        TowerPlacementPoint point = GetPlacementPoint(gridPos);
        
        // 如果没有找到放置点，不能放置
        if (point == null)
        {
            return false;
        }
        
        // 检查放置点是否可用（启用且未被占用）
        return point.isEnabled && !point.isOccupied;
    }
    
    // 获取指定网格位置的放置点
    public TowerPlacementPoint GetPlacementPoint(Vector3Int gridPos)
    {
        return GetPlacementPointByGrid(gridPos);
    }
    
    // 重新初始化所有放置点 - 这个方法从ObstacleManager中调用
    public void ReinitializePlacementPoints()
    {
        // 清除并重建放置点缓存
        pointsById.Clear();
        pointsByGrid.Clear();
        
        // 重新注册所有已存在的放置点
        List<TowerPlacementPoint> existingPoints = new List<TowerPlacementPoint>(placementPoints);
        placementPoints.Clear();
        
        foreach (var point in existingPoints)
        {
            if (point != null)
            {
                RegisterPlacementPoint(point);
            }
        }
        
        // 查找其他可能新增的放置点
        FindAllPlacementPoints();
        
        // 如果启用了高亮，刷新高亮效果
        if (isHighlighting)
        {
            HighlightAvailablePoints(true);
        }
        
        Debug.Log($"TowerPlacementManager: 重新初始化了{placementPoints.Count}个放置点");
    }
    
    // 高亮显示所有可用的放置点（接受启用/禁用参数）
    public void HighlightAvailablePoints(bool enable)
    {
        isHighlighting = enable;
        
        if (!enable)
        {
            DisableHighlighting();
            return;
        }
        
        foreach (var point in placementPoints)
        {
            if (point == null) continue;
            
            SpriteRenderer renderer = point.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;
            
            // 设置颜色
            if (point.isEnabled && !point.isOccupied)
            {
                // 可用点 - 绿色高亮
                renderer.color = availableColor * highlightIntensity;
            }
            else
            {
                // 不可用点 - 红色高亮
                renderer.color = unavailableColor * highlightIntensity;
            }
            
            // 确保放置点可见
            renderer.enabled = true;
        }
    }
    
    // 关闭高亮显示
    public void DisableHighlighting()
    {
        isHighlighting = false;
        
        foreach (var point in placementPoints)
        {
            if (point == null) continue;
            
            SpriteRenderer renderer = point.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;
            
            // 恢复原始颜色
            renderer.color = Color.white;
            
            // 如果不需要显示放置点，可以禁用renderer
            // renderer.enabled = false;
        }
    }
}