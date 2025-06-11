using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 增强版障碍物管理器，支持多种类型、多个障碍物和分组管理
/// </summary>
public class EnhancedObstacleManager : MonoBehaviour
{
    [System.Serializable]
    public enum ObstacleType
    {
        Default,    // 默认障碍物
        Forest,     // 森林
        House,      // 房屋
        Field,      // 田地
        Mountain,   // 山地
        Building,   // 建筑
        Rubble,     // 兼容旧版：碎石
        Trees,      // 兼容旧版：树木
        Water       // 兼容旧版：水面
    }
    
    [System.Serializable]
    public class ObstacleTypeInfo
    {
        public ObstacleType type;
        public string displayName;
        public int clearCost;
        public TileBase[] tiles;
        public Sprite icon;
        public GameObject clearEffect;
    }
    
    [Header("障碍物设置")]
    public Tilemap[] obstacleTilemaps;  // 支持多个Tilemap
    public Tilemap obstacleTilemap
    {
        get
        {
            return (obstacleTilemaps != null && obstacleTilemaps.Length > 0) ? obstacleTilemaps[0] : null;
        }
        set
        {
            if (obstacleTilemaps == null || obstacleTilemaps.Length == 0)
            {
                obstacleTilemaps = new Tilemap[] { value };
            }
            else
            {
                obstacleTilemaps[0] = value;
            }
        }
    }
    public bool searchAllLayers = true;  // 是否自动搜索所有图层
    public ObstacleTypeInfo[] obstacleTypes;
    public int defaultClearCost = 50;
    
    [Header("障碍物分组")]
    public List<ObstacleGroup> obstacleGroups;
    
    [Header("UI设置")]
    public ObstacleUI obstacleUI;           // 基础UI
    public EnhancedObstacleUI enhancedUI;   // 增强版UI
    
    // 记录已清除的障碍物
    private HashSet<Vector3Int> clearedObstacles = new HashSet<Vector3Int>();
    
    // 当前选中的障碍物
    private Vector3Int? selectedPosition;
    private ObstacleType? selectedType;
    private ObstacleGroup selectedGroup;
    
    void Start()
    {
        // 自动查找所有Tilemap层
        if ((obstacleTilemaps == null || obstacleTilemaps.Length == 0) && searchAllLayers)
        {
            List<Tilemap> allTilemaps = new List<Tilemap>();
            
            // 查找场景中所有Tilemap
            Tilemap[] sceneTilemaps = FindObjectsOfType<Tilemap>();
            
            foreach (Tilemap tm in sceneTilemaps)
            {
                // 忽略明确不是障碍物的图层
                if (!tm.name.ToLower().Contains("path") && 
                    !tm.name.ToLower().Contains("road") && 
                    !tm.name.ToLower().Contains("placement") &&
                    !tm.name.ToLower().Contains("foundation"))
                {
                    allTilemaps.Add(tm);
                }
            }
            
            obstacleTilemaps = allTilemaps.ToArray();
            
            if (obstacleTilemaps.Length > 0)
            {
                Debug.Log($"自动找到 {obstacleTilemaps.Length} 个障碍物图层");
            }
        }
        
        // 自动查找UI
        if (obstacleUI == null)
        {
            obstacleUI = FindObjectOfType<ObstacleUI>();
        }
        
        // 自动查找增强版UI
        if (enhancedUI == null)
        {
            enhancedUI = FindObjectOfType<EnhancedObstacleUI>();
        }
        
        // 初始化障碍物组
        if (obstacleGroups != null)
        {
            Debug.Log($"发现 {obstacleGroups.Count} 个障碍物组");
            
            foreach (var group in obstacleGroups)
            {
                Debug.Log($"初始化障碍物组: {group.groupName}, 包含 {(group.positions != null ? group.positions.Count : 0)} 个障碍物");
                
                // 详细记录每个位置
                if (group.positions != null && group.positions.Count > 0)
                {
                    string posStr = "";
                    foreach (var pos in group.positions)
                    {
                        posStr += $"({pos.x},{pos.y}) ";
                        
                        // 尝试将障碍物组的位置标记到tilemap上
                        // 检查该位置是否已经有瓦片
                        bool hasTile = false;
                        foreach (var tilemap in obstacleTilemaps)
                        {
                            if (tilemap != null && tilemap.GetTile(pos) != null)
                            {
                                hasTile = true;
                                break;
                            }
                        }
                        
                        // 如果没有瓦片但有组定义，打印警告
                        if (!hasTile)
                        {
                            Debug.LogWarning($"组 {group.groupName} 的位置 {pos} 在Tilemap中没有瓦片!");
                        }
                    }
                    Debug.Log($"组 {group.groupName} 的位置: {posStr}");
                }
            }
        }
        
        // 生成基于图层名称的障碍物组（可选）
        if (searchAllLayers && (obstacleGroups == null || obstacleGroups.Count == 0))
        {
            AutoCreateGroupsFromTilemapLayers();
        }
        
        // 同步障碍物组位置和Tilemap
        SyncGroupPositionsWithTilemap();
    }
    
    void Update()
    {
        // 处理鼠标点击，选择障碍物
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }
    
    // 处理鼠标点击
    private void HandleMouseClick()
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return;
        
        // 添加这个检查：如果点击在UI上则不处理障碍物选择
        if (IsPointerOverUI())
        {
            return; // 不处理点击事件，因为点击在UI元素上
        }
        
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 检查所有图层
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap == null) continue;
            
            Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);
            
            // 检查是否点击了障碍物
            if (IsObstacle(cellPos) && !IsClearedObstacle(cellPos))
            {
                SelectObstacle(cellPos);
                return; // 找到一个就退出
            }
        }
        
        // 如果点击空白处，取消选择
        if (selectedPosition.HasValue)
        {
            DeselectObstacle();
        }
    }
    
    // 检查指针是否在UI元素上
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    // 选择障碍物
    public void SelectObstacle(Vector3Int position)
    {
        // 先尝试获取障碍物类型
        ObstacleType type = GetObstacleType(position);
        
        // 查找该障碍物所属的组
        ObstacleGroup group = FindObstacleGroup(position);
        
        selectedPosition = position;
        selectedType = type;
        selectedGroup = group;
        
        // 获取障碍物成本
        int cost = GetClearCost(position, type, group);
        
        Debug.Log($"选中了障碍物，位置: {position}, 类型: {type}, 组: {(group != null ? group.groupName : "无")}, 清除成本: {cost}");
        
        // 更新UI - 优先使用增强版UI，如果没有则使用基础UI
        if (enhancedUI != null)
        {
            enhancedUI.ShowObstacleInfo(position, GetObstacleTypeInfo(type), group, cost);
        }
        else if (obstacleUI != null)
        {
            obstacleUI.ShowObstacleInfo(position);
        }
    }
    
    // 取消选择障碍物
    public void DeselectObstacle()
    {
        selectedPosition = null;
        selectedType = null;
        selectedGroup = null;
        
        // 隐藏UI
        if (enhancedUI != null)
        {
            enhancedUI.HidePanel();
        }
        else if (obstacleUI != null)
        {
            obstacleUI.HidePanel();
        }
    }
    
    // 判断位置是否有障碍物（检查所有图层）
    public bool IsObstacle(Vector3Int position)
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return false;
        
        // 1. 检查所有图层是否有瓦片
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap != null && tilemap.GetTile(position) != null)
                return true;
        }
        
        // 2. 检查是否在任何障碍物组中
        if (obstacleGroups != null && obstacleGroups.Count > 0)
        {
            foreach (var group in obstacleGroups)
            {
                if (group != null && group.positions != null && group.positions.Contains(position))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // 判断位置是否是已清除的障碍物
    public bool IsClearedObstacle(Vector3Int position)
    {
        return clearedObstacles.Contains(position);
    }
    
    // 清除障碍物
    public bool ClearObstacle()
    {
        if (!selectedPosition.HasValue) return false;
        
        Vector3Int position = selectedPosition.Value;
        ObstacleType type = selectedType.HasValue ? selectedType.Value : ObstacleType.Default;
        
        // 获取清除成本
        int clearCost = GetClearCost(position, type, selectedGroup);
        
        // 检查金币是否足够
        bool hasEnoughGold = false;
        
        // 优先使用CoinManager
        if (CoinManager.Instance != null)
        {
            hasEnoughGold = CoinManager.Instance.HasEnoughCoins(clearCost);
            if (!hasEnoughGold)
            {
                Debug.LogWarning($"[EnhancedObstacleManager] 金币不足，需要{clearCost}金币来清除障碍物，当前：{CoinManager.Instance.CurrentCoins}");
            return false;
        }
        
            // 使用CoinManager扣除金币
            CoinManager.Instance.TrySpendCoins(clearCost);
        }
        // 兼容旧版：使用TowerManager
        else
        {
            TowerManager towerManager = TowerManager.Instance;
            if (towerManager != null)
            {
                if (towerManager.currentGold < clearCost)
                {
                    Debug.LogWarning($"[EnhancedObstacleManager] 金币不足，需要{clearCost}金币来清除障碍物，当前：{towerManager.currentGold}");
                    return false;
                }
                
                // 扣除金币
            towerManager.currentGold -= clearCost;
            towerManager.UpdateGoldDisplay();
            }
        }
        
        // 如果存在障碍物组，清除整个组
        if (selectedGroup != null && selectedGroup.positions != null && selectedGroup.positions.Count > 0)
        {
            // 清除组中所有位置的障碍物
            foreach (Vector3Int pos in selectedGroup.positions)
            {
                // 清除所有图层中此位置的瓦片
                foreach (var tilemap in obstacleTilemaps)
                {
                    if (tilemap != null && tilemap.GetTile(pos) != null)
                    {
                        tilemap.SetTile(pos, null);
                    }
                }
                
                // 记录已清除的位置
                clearedObstacles.Add(pos);
            }
            
            // 将组标记为完全清除
            while (!selectedGroup.IsFullyCleared)
            {
                selectedGroup.AddCleared();
            }
            
            // 查找并禁用属于该组的所有Tilemap图层
            foreach (var tilemap in obstacleTilemaps)
            {
                if (tilemap != null && IsGroupTilemap(tilemap, selectedGroup.groupName))
                {
                    // 禁用此图层
                    tilemap.gameObject.SetActive(false);
                    Debug.Log($"已禁用图层 {tilemap.name}，因为它属于已清除的障碍物组 {selectedGroup.groupName}");
                }
            }
            
            // 从障碍物组列表中移除此组
            if (obstacleGroups != null && obstacleGroups.Contains(selectedGroup))
            {
                obstacleGroups.Remove(selectedGroup);
                Debug.Log($"已从障碍物管理器中删除障碍物组 {selectedGroup.groupName}");
            }
            
            // 在整个组的中心位置播放特效
            if (selectedGroup.positions.Count > 0)
            {
                // 计算组的中心位置
                Vector3 centerWorldPos = Vector3.zero;
                foreach (Vector3Int pos in selectedGroup.positions)
                {
                    // 使用第一个有效的Tilemap获取世界坐标
                    foreach (var tilemap in obstacleTilemaps)
                    {
                        if (tilemap != null)
                        {
                            centerWorldPos += tilemap.GetCellCenterWorld(pos);
                            break;
                        }
                    }
                }
                centerWorldPos /= selectedGroup.positions.Count;
                
                // 在整个组的中心位置播放特效
                PlayEffectAtWorldPosition(centerWorldPos, type);
            }
            
            Debug.Log($"障碍物组 {selectedGroup.groupName} 已完全清除! 包含 {selectedGroup.positions.Count} 个位置，花费: {clearCost}金币");
        }
        // 如果没有组或组为空，只清除单个位置
        else
        {
            // 清除所有图层中的障碍物
            foreach (var tilemap in obstacleTilemaps)
            {
                if (tilemap != null && tilemap.GetTile(position) != null)
                {
                    tilemap.SetTile(position, null);
                }
            }
            
            // 记录已清除的位置
            clearedObstacles.Add(position);
            
            // 播放清除特效
            PlayClearEffect(position, type);
            
            Debug.Log($"已清除位置 {position} 的障碍物，类型: {type}, 花费: {clearCost}金币");
        }
        
        // 清除选择
        DeselectObstacle();
        
        return true;
    }
    
    // 清除特定位置的障碍物（由UI调用）
    public bool ClearObstacle(Vector3Int position, ObstacleGroup group = null)
    {
        ObstacleType type = GetObstacleType(position);
        
        // 获取清除成本（这里不需要检查金币，因为调用方已经处理）
        int clearCost = GetClearCost(position, type, group);
        
        // 如果存在障碍物组，清除整个组
        if (group != null && group.positions != null && group.positions.Count > 0)
        {
            // 清除组中所有位置的障碍物
            foreach (Vector3Int pos in group.positions)
            {
                // 清除所有图层中此位置的瓦片
                foreach (var tilemap in obstacleTilemaps)
                {
                    if (tilemap != null && tilemap.GetTile(pos) != null)
                    {
                        tilemap.SetTile(pos, null);
                    }
                }
                
                // 记录已清除的位置
                clearedObstacles.Add(pos);
            }
            
            // 将组标记为完全清除
            while (!group.IsFullyCleared)
            {
                group.AddCleared();
            }
            
            // 查找并禁用属于该组的所有Tilemap图层
            foreach (var tilemap in obstacleTilemaps)
            {
                if (tilemap != null && IsGroupTilemap(tilemap, group.groupName))
                {
                    // 禁用此图层
                    tilemap.gameObject.SetActive(false);
                    Debug.Log($"已禁用图层 {tilemap.name}，因为它属于已清除的障碍物组 {group.groupName}");
                }
            }
            
            // 从障碍物组列表中移除此组
            if (obstacleGroups != null && obstacleGroups.Contains(group))
            {
                obstacleGroups.Remove(group);
                Debug.Log($"已从障碍物管理器中删除障碍物组 {group.groupName}");
            }
            
            // 播放清除特效(在组的中心位置)
            if (group.positions.Count > 0)
            {
                // 计算组的中心位置
                Vector3 centerWorldPos = Vector3.zero;
                foreach (Vector3Int pos in group.positions)
                {
                    // 使用第一个有效的Tilemap获取世界坐标
                    foreach (var tilemap in obstacleTilemaps)
                    {
                        if (tilemap != null)
                        {
                            centerWorldPos += tilemap.GetCellCenterWorld(pos);
                            break;
                        }
                    }
                }
                centerWorldPos /= group.positions.Count;
                
                // 在整个组的中心位置播放特效
                PlayEffectAtWorldPosition(centerWorldPos, type);
            }
            
            Debug.Log($"障碍物组 {group.groupName} 已完全清除! 包含 {group.positions.Count} 个位置，花费: {clearCost}金币");
        }
        // 如果没有组，只清除单个位置
        else
        {
            // 清除所有图层中的障碍物
            foreach (var tilemap in obstacleTilemaps)
            {
                if (tilemap != null && tilemap.GetTile(position) != null)
                {
                    tilemap.SetTile(position, null);
                }
            }
            
            // 记录已清除的位置
            clearedObstacles.Add(position);
            
            // 播放清除特效
            PlayClearEffect(position, type);
            
            Debug.Log($"已清除位置 {position} 的障碍物，类型: {type}, 花费: {clearCost}金币");
        }
        
        return true;
    }
    
    // 判断Tilemap是否属于指定的障碍物组
    private bool IsGroupTilemap(Tilemap tilemap, string groupName)
    {
        if (tilemap == null || string.IsNullOrEmpty(groupName)) return false;
        
        string tilemapName = tilemap.name.ToLower();
        string groupKey = groupName.ToLower();
        
        // 如果Tilemap名称包含组名，则认为它属于此组
        // 例如：groupName为"Forest1"，tilemap名称为"Forest1"、"Forest1.1"、"Forest1_trees"等都会被认为是此组的图层
        return tilemapName == groupKey || 
               tilemapName.StartsWith(groupKey + ".") || 
               tilemapName.StartsWith(groupKey + "_");
    }
    
    // 在指定的世界位置播放特效
    private void PlayEffectAtWorldPosition(Vector3 worldPosition, ObstacleType type)
    {
        ObstacleTypeInfo typeInfo = GetObstacleTypeInfo(type);
        
        if (typeInfo != null && typeInfo.clearEffect != null)
        {
            GameObject effect = Instantiate(typeInfo.clearEffect, worldPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    // 播放清除特效
    private void PlayClearEffect(Vector3Int position, ObstacleType type)
    {
        ObstacleTypeInfo typeInfo = GetObstacleTypeInfo(type);
        
        if (typeInfo != null && typeInfo.clearEffect != null)
        {
            // 获取世界坐标（使用第一个有效的Tilemap）
            Vector3 worldPos = Vector3.zero;
            foreach (var tilemap in obstacleTilemaps)
            {
                if (tilemap != null)
                {
                    worldPos = tilemap.GetCellCenterWorld(position);
                    break;
                }
            }
            
            GameObject effect = Instantiate(typeInfo.clearEffect, worldPos, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    // 获取障碍物类型（检查所有图层）
    public ObstacleType GetObstacleType(Vector3Int position)
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return ObstacleType.Default;
        
        // 先从图层名称推断类型
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap == null || tilemap.GetTile(position) == null) continue;
            
            string tilemapName = tilemap.name.ToLower();
            
            // 根据图层名称推断类型
            if (tilemapName.Contains("forest")) return ObstacleType.Forest;
            if (tilemapName.Contains("house")) return ObstacleType.House;
            if (tilemapName.Contains("field")) return ObstacleType.Field;
            if (tilemapName.Contains("mountain")) return ObstacleType.Mountain;
            if (tilemapName.Contains("build")) return ObstacleType.Building;
        }
        
        // 如果无法从图层名称推断，则检查Tile类型
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap == null) continue;
            
            TileBase tile = tilemap.GetTile(position);
            if (tile == null) continue;
            
            // 查找匹配的类型
            foreach (var typeInfo in obstacleTypes)
            {
                if (typeInfo.tiles != null)
                {
                    foreach (var typeTile in typeInfo.tiles)
                    {
                        if (typeTile == tile)
                        {
                            return typeInfo.type;
                        }
                    }
                }
            }
        }
        
        return ObstacleType.Default;
    }
    
    // 获取障碍物类型信息
    public ObstacleTypeInfo GetObstacleTypeInfo(ObstacleType type)
    {
        foreach (var typeInfo in obstacleTypes)
        {
            if (typeInfo.type == type)
            {
                return typeInfo;
            }
        }
        
        return null;
    }
    
    // 查找障碍物所属的组
    public ObstacleGroup FindObstacleGroup(Vector3Int position)
    {
        if (obstacleGroups == null) return null;
        
        foreach (var group in obstacleGroups)
        {
            if (group.ContainsPosition(position))
            {
                return group;
            }
        }
        
        return null;
    }
    
    // 获取清除成本
    public int GetClearCost(Vector3Int position, ObstacleType type, ObstacleGroup group)
    {
        // 如果有组，且使用组成本
        if (group != null && group.useGroupCost)
        {
            return group.GetClearCost(defaultClearCost);
        }
        
        // 否则使用类型成本
        ObstacleTypeInfo typeInfo = GetObstacleTypeInfo(type);
        if (typeInfo != null && typeInfo.clearCost > 0)
        {
            return typeInfo.clearCost;
        }
        
        // 默认成本
        return defaultClearCost;
    }
    
    // 判断是否可以在指定位置放置塔
    public bool CanPlaceAtPosition(Vector3Int position)
    {
        return !IsObstacle(position) || IsClearedObstacle(position);
    }
    
    // 创建障碍物组
    public ObstacleGroup CreateGroup(string name, List<Vector3Int> positions, int groupCost, bool useGroupCost = true)
    {
        ObstacleGroup group = new ObstacleGroup
        {
            groupName = name,
            positions = positions,
            clearCost = groupCost,
            useGroupCost = useGroupCost
        };
        
        if (obstacleGroups == null)
        {
            obstacleGroups = new List<ObstacleGroup>();
        }
        
        obstacleGroups.Add(group);
        return group;
    }
    
    // 自动生成基于图层的障碍物组
    public void AutoCreateGroupsFromTilemapLayers()
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return;
        
        // 为每个对应相同障碍物的Tilemap对创建一个组
        Dictionary<string, List<Vector3Int>> layerGroups = new Dictionary<string, List<Vector3Int>>();
        
        // 提取数字标识符，例如从"House1.1"和"House1.2"中提取"House1"
        foreach (var tilemap in obstacleTilemaps)
        {
            if (tilemap == null) continue;
            
            string name = tilemap.name;
            string groupKey = ExtractGroupKey(name);
            
            if (!string.IsNullOrEmpty(groupKey))
            {
                if (!layerGroups.ContainsKey(groupKey))
                {
                    layerGroups[groupKey] = new List<Vector3Int>();
                }
                
                // 收集该Tilemap中的所有Tile位置
                BoundsInt bounds = tilemap.cellBounds;
                TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
                
                for (int x = 0; x < bounds.size.x; x++)
                {
                    for (int y = 0; y < bounds.size.y; y++)
                    {
                        for (int z = 0; z < bounds.size.z; z++)
                        {
                            Vector3Int pos = new Vector3Int(x + bounds.x, y + bounds.y, z + bounds.z);
                            TileBase tile = tilemap.GetTile(pos);
                            
                            if (tile != null)
                            {
                                layerGroups[groupKey].Add(pos);
                            }
                        }
                    }
                }
            }
        }
        
        // 创建障碍物组
        if (obstacleGroups == null)
        {
            obstacleGroups = new List<ObstacleGroup>();
        }
        
        foreach (var entry in layerGroups)
        {
            if (entry.Value.Count > 0)
            {
                // 根据不同类型设置不同的清除成本
                int cost = defaultClearCost;
                if (entry.Key.ToLower().Contains("house"))
                    cost = 80;
                else if (entry.Key.ToLower().Contains("forest"))
                    cost = 60;
                else if (entry.Key.ToLower().Contains("mountain"))
                    cost = 120;
                else if (entry.Key.ToLower().Contains("building"))
                    cost = 100;
                
                CreateGroup(entry.Key, entry.Value, cost, true);
                Debug.Log($"自动创建障碍物组: {entry.Key}，包含 {entry.Value.Count} 个位置，清除成本: {cost}");
            }
        }
    }
    
    // 从图层名称提取组标识符
    private string ExtractGroupKey(string layerName)
    {
        // 例如：House1.1、House1.2 -> House1
        // 或者：Forest3 -> Forest3
        
        if (string.IsNullOrEmpty(layerName)) return string.Empty;
        
        // 常见障碍物前缀
        string[] prefixes = { "House", "Forest", "Field", "Mountain", "Building" };
        
        foreach (var prefix in prefixes)
        {
            if (layerName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                // 提取数字部分，直到"."字符
                int endIndex = layerName.IndexOf('.');
                if (endIndex > 0)
                {
                    return layerName.Substring(0, endIndex);
                }
                else
                {
                    // 检查是否有数字后缀
                    for (int i = prefix.Length; i < layerName.Length; i++)
                    {
                        if (!char.IsDigit(layerName[i]))
                        {
                            return layerName.Substring(0, i);
                        }
                    }
                    
                    // 整个名称可能就是一个有效的组
                    return layerName;
                }
            }
        }
        
        return layerName; // 如果没有匹配的前缀，返回原始名称
    }
    
    // 兼容旧版Editor脚本的方法封装
    public void AutoCreateGroups()
    {
        AutoCreateGroupsFromTilemapLayers();
    }

    // 兼容旧版Editor脚本的方法（带前缀参数）
    public void AutoCreateGroups(string groupPrefix)
    {
        // 目前忽略前缀，直接调用内部方法
        AutoCreateGroupsFromTilemapLayers();
    }
    
    // 同步障碍物组位置和Tilemap
    private void SyncGroupPositionsWithTilemap()
    {
        if (obstacleGroups == null || obstacleTilemaps == null || obstacleTilemaps.Length == 0) return;
        
        Debug.Log("开始同步障碍物组位置和Tilemap...");
        
        // 获取默认使用的Tilemap
        Tilemap targetTilemap = obstacleTilemaps[0];
        
        // 创建组名到Tile类型的映射
        Dictionary<string, TileBase> groupTileMap = new Dictionary<string, TileBase>();
        
        // 首先尝试从组名称推断Tile类型
        foreach (var group in obstacleGroups)
        {
            if (group == null) continue;
            
            // 如果组已经有设置的obstacleTile，直接使用它
            if (group.obstacleTile != null)
            {
                groupTileMap[group.groupName] = group.obstacleTile;
                continue;
            }
            
            // 否则尝试通过组名匹配类型
            string groupLower = group.groupName.ToLower();
            ObstacleType typeToUse = ObstacleType.Default;
            
            if (groupLower.Contains("forest"))
                typeToUse = ObstacleType.Forest;
            else if (groupLower.Contains("house"))
                typeToUse = ObstacleType.House;
            else if (groupLower.Contains("field"))
                typeToUse = ObstacleType.Field;
            else if (groupLower.Contains("mountain"))
                typeToUse = ObstacleType.Mountain;
            else if (groupLower.Contains("building"))
                typeToUse = ObstacleType.Building;
            
            // 查找对应类型的Tile
            if (obstacleTypes != null)
            {
                foreach (var typeInfo in obstacleTypes)
                {
                    if (typeInfo.type == typeToUse && typeInfo.tiles != null && typeInfo.tiles.Length > 0)
                    {
                        groupTileMap[group.groupName] = typeInfo.tiles[0];
                        Debug.Log($"为组 {group.groupName} 分配了类型 {typeToUse} 的Tile");
                        break;
                    }
                }
            }
        }
        
        // 获取默认使用的Tile作为后备
        TileBase defaultTile = null;
        
        // 尝试从obstacleTypes中获取默认Tile
        if (obstacleTypes != null && obstacleTypes.Length > 0)
        {
            foreach (var typeInfo in obstacleTypes)
            {
                if (typeInfo.type == ObstacleType.Default && typeInfo.tiles != null && typeInfo.tiles.Length > 0)
                {
                    defaultTile = typeInfo.tiles[0];
                    break;
                }
            }
        }
        
        // 如果仍没找到默认Tile，检查现有Tilemap
        if (defaultTile == null)
        {
            Debug.LogWarning("未找到默认Tile类型，将不会自动填充没有明确指定Tile的障碍物组");
        }
        
        int syncCount = 0;
        
        // 遍历所有组，确保位置在Tilemap中有对应的瓦片
        foreach (var group in obstacleGroups)
        {
            if (group == null || group.positions == null) continue;
            
            // 获取该组应该使用的Tile
            TileBase tileForThisGroup = null;
            
            // 1. 首先检查组对象中是否直接设置了瓦片
            if (group.obstacleTile != null)
            {
                tileForThisGroup = group.obstacleTile;
            }
            // 2. 然后检查我们是否已经为该组分配了瓦片
            else if (groupTileMap.ContainsKey(group.groupName))
            {
                tileForThisGroup = groupTileMap[group.groupName];
            }
            // 3. 最后使用默认瓦片
            else if (defaultTile != null)
            {
                tileForThisGroup = defaultTile;
            }
            else
            {
                // 如果没有找到任何瓦片可用，跳过这个组
                Debug.LogWarning($"组 {group.groupName} 没有可用的Tile，跳过同步");
                continue;
            }
            
            foreach (var pos in group.positions)
            {
                bool hasTile = false;
                
                // 检查该位置是否已经有瓦片
                foreach (var tilemap in obstacleTilemaps)
                {
                    if (tilemap != null && tilemap.GetTile(pos) != null)
                    {
                        hasTile = true;
                        break;
                    }
                }
                
                // 如果没有瓦片，添加该组对应的瓦片
                if (!hasTile && tileForThisGroup != null)
                {
                    targetTilemap.SetTile(pos, tileForThisGroup);
                    syncCount++;
                    Debug.Log($"为组 {group.groupName} 的位置 {pos} 添加了瓦片");
                }
            }
        }
        
        Debug.Log($"同步完成，共添加了 {syncCount} 个瓦片");
    }
}