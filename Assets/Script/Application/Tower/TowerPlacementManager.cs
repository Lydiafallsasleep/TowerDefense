using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 管理所有塔的放置点
/// </summary>
public class TowerPlacementManager : Singleton<TowerPlacementManager>
{
    [Header("放置点设置")]
    public List<TowerPlacementPoint> placementPoints = new List<TowerPlacementPoint>();
    
    [Header("放置设置")]
    public bool usePresetPositionsOnly = true;  // 是否只使用预设位置
    public bool highlightAvailablePoints = true;  // 是否高亮可用的放置点
    public GameObject highligherPrefab;  // 高亮效果预制体
    
    // 当前选中的放置点
    private TowerPlacementPoint selectedPoint;
    
    // 放置点的字典，用于快速查找
    private Dictionary<string, TowerPlacementPoint> pointsDict = new Dictionary<string, TowerPlacementPoint>();
    private Dictionary<Vector3Int, TowerPlacementPoint> gridPointsDict = new Dictionary<Vector3Int, TowerPlacementPoint>();
    
    // 放置点分组
    private Dictionary<string, List<TowerPlacementPoint>> groupsDict = new Dictionary<string, List<TowerPlacementPoint>>();
    
    void Start()
    {
        // 初始化放置点
        InitializePlacementPoints();
    }
    
    /// <summary>
    /// 初始化放置点
    /// </summary>
    private void InitializePlacementPoints()
    {
        // 清空字典
        pointsDict.Clear();
        gridPointsDict.Clear();
        groupsDict.Clear();
        
        // 自动查找场景中的所有放置点
        if (placementPoints.Count == 0)
        {
            TowerPlacementPoint[] points = FindObjectsOfType<TowerPlacementPoint>();
            placementPoints = new List<TowerPlacementPoint>(points);
        }
        
        // 添加到字典
        foreach (TowerPlacementPoint point in placementPoints)
        {
            if (!string.IsNullOrEmpty(point.pointID))
            {
                pointsDict[point.pointID] = point;
            }
            
            // 添加到网格字典
            gridPointsDict[point.gridPosition] = point;
            
            // 添加到分组字典
            if (!string.IsNullOrEmpty(point.placementGroupID))
            {
                if (!groupsDict.ContainsKey(point.placementGroupID))
                {
                    groupsDict[point.placementGroupID] = new List<TowerPlacementPoint>();
                }
                
                groupsDict[point.placementGroupID].Add(point);
            }
        }
        
        Debug.Log($"已初始化 {placementPoints.Count} 个塔放置点");
    }
    
    /// <summary>
    /// 获取指定位置的放置点
    /// </summary>
    public TowerPlacementPoint GetPlacementPoint(Vector3Int gridPosition)
    {
        if (gridPointsDict.TryGetValue(gridPosition, out TowerPlacementPoint point))
        {
            return point;
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取指定ID的放置点
    /// </summary>
    public TowerPlacementPoint GetPlacementPoint(string pointID)
    {
        if (pointsDict.TryGetValue(pointID, out TowerPlacementPoint point))
        {
            return point;
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取指定组的所有放置点
    /// </summary>
    public List<TowerPlacementPoint> GetGroupPoints(string groupID)
    {
        if (groupsDict.TryGetValue(groupID, out List<TowerPlacementPoint> points))
        {
            return points;
        }
        
        return new List<TowerPlacementPoint>();
    }
    
    /// <summary>
    /// 检查指定位置是否可以放置塔
    /// </summary>
    public bool CanPlaceTowerAt(Vector3Int gridPosition)
    {
        if (usePresetPositionsOnly)
        {
            // 使用预设位置，检查是否有对应的放置点且可用
            TowerPlacementPoint point = GetPlacementPoint(gridPosition);
            return point != null && point.isEnabled && !point.isOccupied;
        }
        else
        {
            // 不使用预设位置，使用默认检查逻辑
            TowerManager towerManager = TowerManager.Instance;
            return towerManager != null && towerManager.CanPlaceTower(gridPosition);
        }
    }
    
    /// <summary>
    /// 在指定位置放置塔
    /// </summary>
    public void PlaceTowerAt(Vector3Int gridPosition, BaseTower tower)
    {
        TowerPlacementPoint point = GetPlacementPoint(gridPosition);
        if (point != null)
        {
            point.OccupyPoint(tower);
        }
    }
    
    /// <summary>
    /// 移除指定位置的塔
    /// </summary>
    public void RemoveTowerAt(Vector3Int gridPosition)
    {
        TowerPlacementPoint point = GetPlacementPoint(gridPosition);
        if (point != null)
        {
            point.ReleasePoint();
        }
    }
    
    /// <summary>
    /// 启用指定组的所有放置点
    /// </summary>
    public void EnableGroup(string groupID)
    {
        List<TowerPlacementPoint> points = GetGroupPoints(groupID);
        foreach (TowerPlacementPoint point in points)
        {
            point.EnablePoint();
        }
    }
    
    /// <summary>
    /// 禁用指定组的所有放置点
    /// </summary>
    public void DisableGroup(string groupID)
    {
        List<TowerPlacementPoint> points = GetGroupPoints(groupID);
        foreach (TowerPlacementPoint point in points)
        {
            point.DisablePoint();
        }
    }
    
    /// <summary>
    /// 选择放置点
    /// </summary>
    public void SelectPoint(TowerPlacementPoint point)
    {
        if (selectedPoint != point)
        {
            // 取消之前的选择
            if (selectedPoint != null)
            {
                // TODO: 取消之前选择点的高亮效果
            }
            
            selectedPoint = point;
            
            // 通知TowerManager选择了放置点
            TowerManager towerManager = TowerManager.Instance;
            if (towerManager != null)
            {
                towerManager.OnPlacementPointSelected(point);
            }
        }
    }
    
    /// <summary>
    /// 取消选择
    /// </summary>
    public void DeselectPoint()
    {
        if (selectedPoint != null)
        {
            // TODO: 取消选择点的高亮效果
            
            selectedPoint = null;
            
            // 通知TowerManager取消了选择
            TowerManager towerManager = TowerManager.Instance;
            if (towerManager != null)
            {
                towerManager.OnPlacementPointDeselected();
            }
        }
    }
    
    /// <summary>
    /// 获取最近的可用放置点
    /// </summary>
    public TowerPlacementPoint GetNearestAvailablePoint(Vector3 worldPosition)
    {
        // 空检查
        if (placementPoints == null || placementPoints.Count == 0)
        {
            Debug.LogWarning("放置点列表为空或未初始化");
            return null;
        }
        
        TowerPlacementPoint nearestPoint = null;
        float minDistance = float.MaxValue;
        
        foreach (TowerPlacementPoint point in placementPoints)
        {
            // 添加空检查
            if (point == null)
            {
                Debug.LogWarning("放置点列表中存在空项");
                continue;
            }
            
            if (point.isEnabled && !point.isOccupied)
            {
                float distance = Vector3.Distance(worldPosition, point.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = point;
                }
            }
        }
        
        return nearestPoint;
    }
    
    /// <summary>
    /// 高亮所有可用的放置点
    /// </summary>
    public void HighlightAvailablePoints(bool highlight)
    {
        foreach (TowerPlacementPoint point in placementPoints)
        {
            if (point.isEnabled && !point.isOccupied)
            {
                // TODO: 实现高亮效果
                // 可以使用粒子效果、闪烁效果等
            }
        }
    }
    
    /// <summary>
    /// 创建一个新的放置点
    /// </summary>
    public TowerPlacementPoint CreatePlacementPoint(Vector3 worldPosition, Vector3Int gridPosition, string pointID = "")
    {
        GameObject pointObj = new GameObject($"PlacementPoint_{pointID}");
        pointObj.transform.position = worldPosition;
        
        TowerPlacementPoint point = pointObj.AddComponent<TowerPlacementPoint>();
        point.pointID = string.IsNullOrEmpty(pointID) ? $"Point_{placementPoints.Count}" : pointID;
        point.gridPosition = gridPosition;
        
        // 添加视觉指示器
        GameObject indicatorObj = new GameObject("PlacementIndicator");
        indicatorObj.transform.SetParent(pointObj.transform);
        indicatorObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer indicator = indicatorObj.AddComponent<SpriteRenderer>();
        indicator.sprite = Resources.Load<Sprite>("UI/PlacementIndicator");
        indicator.color = point.availableColor;
        indicator.sortingOrder = -1;
        
        point.placementIndicator = indicator;
        
        // 添加到列表和字典
        placementPoints.Add(point);
        pointsDict[point.pointID] = point;
        gridPointsDict[point.gridPosition] = point;
        
        return point;
    }
} 