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
    
    [Header("TiledMap设置")]
    public bool generateFromTiledMap = false;  // 是否从TiledMap生成放置点
    public GameObject tiledMapObject;  // TiledMap对象
    public string towerPlacementLayerName = "TowerPlacement";  // TiledMap中标记放置点的图层名称
    public string placementTileName = "TowerSpot";  // TiledMap中表示放置点的Tile名称
    public float placementYOffset = 0.5f;  // 放置点的Y轴偏移
    
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
        
        // 从TiledMap生成放置点
        if (generateFromTiledMap && tiledMapObject != null)
        {
            GeneratePlacementPointsFromTiledMap();
        }
        // 如果没有从TiledMap生成且没有手动设置，自动查找场景中的所有放置点
        else if (placementPoints.Count == 0)
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
    /// 从TiledMap生成放置点
    /// </summary>
    private void GeneratePlacementPointsFromTiledMap()
    {
        if (tiledMapObject == null)
        {
            Debug.LogWarning("TiledMap对象未设置，无法生成放置点");
            return;
        }
        
        // 清空现有放置点
        placementPoints.Clear();
        
        // 获取TiledMap组件
        // 这里假设使用的是SuperTiled2Unity插件或类似的TiledMap插件
        var tiledMap = tiledMapObject.GetComponent<SuperTiled2Unity.SuperMap>();
        if (tiledMap == null)
        {
            Debug.LogWarning("未找到有效的TiledMap组件");
            return;
        }
        
        // 查找塔放置图层
        SuperTiled2Unity.SuperLayer towerLayer = null;
        foreach (var layer in tiledMap.GetComponentsInChildren<SuperTiled2Unity.SuperLayer>())
        {
            if (layer.m_TiledName == towerPlacementLayerName)
            {
                towerLayer = layer;
                break;
            }
        }
        
        if (towerLayer == null)
        {
            Debug.LogWarning($"在TiledMap中未找到名为 {towerPlacementLayerName} 的图层");
            return;
        }
        
        // 遍历图层中的所有对象
        var objectLayer = towerLayer as SuperTiled2Unity.SuperObjectLayer;
        if (objectLayer != null)
        {
            // 如果是对象图层
            ProcessObjectLayer(objectLayer, tiledMap);
        }
        else
        {
            // 如果是瓦片图层
            var tileLayer = towerLayer as SuperTiled2Unity.SuperTileLayer;
            if (tileLayer != null)
            {
                ProcessTileLayer(tileLayer, tiledMap);
            }
        }
        
        Debug.Log($"从TiledMap生成了 {placementPoints.Count} 个塔放置点");
    }
    
    /// <summary>
    /// 处理对象图层
    /// </summary>
    private void ProcessObjectLayer(SuperTiled2Unity.SuperObjectLayer objectLayer, SuperTiled2Unity.SuperMap tiledMap)
    {
        int pointCount = 0;
        foreach (var mapObject in objectLayer.GetComponentsInChildren<SuperTiled2Unity.SuperObject>())
        {
            // 检查对象属性或名称，确定是否为放置点
            if (mapObject.m_TiledName == placementTileName || HasPlacementProperty(mapObject))
            {
                // 获取世界坐标
                Vector3 worldPos = mapObject.transform.position;
                worldPos.y += placementYOffset; // 添加Y轴偏移
                
                // 计算网格坐标
                Vector3Int gridPos = new Vector3Int(
                    Mathf.RoundToInt(worldPos.x),
                    Mathf.RoundToInt(worldPos.y),
                    Mathf.RoundToInt(worldPos.z)
                );
                
                // 创建放置点
                string pointID = $"TiledPoint_{pointCount++}";
                string groupID = GetObjectPropertyValue(mapObject, "Group");
                
                TowerPlacementPoint point = CreatePlacementPoint(worldPos, gridPos, pointID);
                point.placementGroupID = groupID;
                
                // 检查是否有禁用属性
                bool isDisabled = HasProperty(mapObject, "Disabled") && GetObjectPropertyBoolValue(mapObject, "Disabled");
                if (isDisabled)
                {
                    point.DisablePoint();
                }
            }
        }
    }
    
    /// <summary>
    /// 处理瓦片图层
    /// </summary>
    private void ProcessTileLayer(SuperTiled2Unity.SuperTileLayer tileLayer, SuperTiled2Unity.SuperMap tiledMap)
    {
        var tilemap = tileLayer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tilemap == null) return;
        
        int pointCount = 0;
        
        // 获取瓦片地图的边界
        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        
        // 遍历瓦片
        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                UnityEngine.Tilemaps.TileBase tile = tilemap.GetTile(cellPos);
                
                if (tile != null)
                {
                    // 检查是否是放置点瓦片
                    if (IsTowerPlacementTile(tile))
                    {
                        // 获取世界坐标
                        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                        worldPos.y += placementYOffset;
                        
                        // 创建放置点
                        string pointID = $"TiledPoint_{pointCount++}";
                        
                        // 计算网格坐标
                        Vector3Int gridPos = new Vector3Int(
                            Mathf.RoundToInt(worldPos.x),
                            Mathf.RoundToInt(worldPos.y),
                            Mathf.RoundToInt(worldPos.z)
                        );
                        
                        CreatePlacementPoint(worldPos, gridPos, pointID);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 检查瓦片是否是放置点瓦片
    /// </summary>
    private bool IsTowerPlacementTile(UnityEngine.Tilemaps.TileBase tile)
    {
        // 根据项目的具体实现方式检查瓦片
        // 这里提供一个简单的实现，通过瓦片名称判断
        return tile.name.Contains(placementTileName);
    }
    
    /// <summary>
    /// 检查对象是否有放置点属性
    /// </summary>
    private bool HasPlacementProperty(SuperTiled2Unity.SuperObject mapObject)
    {
        return HasProperty(mapObject, "IsTowerPlacement") && 
               GetObjectPropertyBoolValue(mapObject, "IsTowerPlacement");
    }
    
    /// <summary>
    /// 检查对象是否有指定属性
    /// </summary>
    private bool HasProperty(SuperTiled2Unity.SuperObject mapObject, string propertyName)
    {
        var customProperties = mapObject.GetComponent<SuperTiled2Unity.SuperCustomProperties>();
        return customProperties != null && customProperties.HasProperty(propertyName);
    }
    
    /// <summary>
    /// 获取对象属性布尔值
    /// </summary>
    private bool GetObjectPropertyBoolValue(SuperTiled2Unity.SuperObject mapObject, string propertyName)
    {
        var customProperties = mapObject.GetComponent<SuperTiled2Unity.SuperCustomProperties>();
        if (customProperties != null && customProperties.HasProperty(propertyName))
        {
            SuperTiled2Unity.CustomProperty property;
            if (customProperties.TryGetCustomProperty(propertyName, out property))
            {
                bool value;
                if (bool.TryParse(property.GetValueAsString(), out value))
                {
                    return value;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// 获取对象属性字符串值
    /// </summary>
    private string GetObjectPropertyValue(SuperTiled2Unity.SuperObject mapObject, string propertyName)
    {
        var customProperties = mapObject.GetComponent<SuperTiled2Unity.SuperCustomProperties>();
        if (customProperties != null && customProperties.HasProperty(propertyName))
        {
            SuperTiled2Unity.CustomProperty property;
            if (customProperties.TryGetCustomProperty(propertyName, out property))
            {
                return property.GetValueAsString();
            }
        }
        return string.Empty;
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
        TowerPlacementPoint nearestPoint = null;
        float minDistance = float.MaxValue;
        
        foreach (TowerPlacementPoint point in placementPoints)
        {
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