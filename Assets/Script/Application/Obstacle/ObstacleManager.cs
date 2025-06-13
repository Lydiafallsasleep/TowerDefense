using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 管理游戏中的障碍物，包括障碍物消除和位置解锁
/// </summary>
public class ObstacleManager : Singleton<ObstacleManager>
{
    [Header("障碍物设置")]
    public Tilemap[] obstacleTilemaps;  // 障碍物所在的多个Tilemap
    public Tilemap obstacleTilemap;     // 向后兼容 - 主障碍物图层
    public TileBase obstacleTile;       // 障碍物使用的Tile
    public TileBase clearableTile;      // 可清除的障碍物Tile
    
    [Tooltip("是否在所有图层中寻找障碍物")]
    public bool searchAllLayers = true;
    
    [Header("解锁设置")]
    public int clearCost = 50;        // 清除障碍物的金币消耗
    public GameObject clearEffect;    // 清除特效
    
    [Header("UI连接")]
    public ObstacleUI obstacleUI;
    
    // 记录已清除的障碍物位置
    private HashSet<Vector3Int> clearedObstacles = new HashSet<Vector3Int>();
    
    // 当前选中的障碍物位置
    private Vector3Int? selectedObstaclePos = null;
    
    // 障碍物被清除时的委托事件
    public delegate void ObstacleClearedHandler(Vector3Int position);
    public event ObstacleClearedHandler OnObstacleCleared;
    
    void Start()
    {
        // 自动查找所有Tilemap层并添加到数组中
        if ((obstacleTilemaps == null || obstacleTilemaps.Length == 0) && searchAllLayers)
        {
            List<Tilemap> allTilemaps = new List<Tilemap>();
            
            // 查找场景中所有Tilemap
            Tilemap[] sceneTilemaps = FindObjectsOfType<Tilemap>();
            
            foreach (Tilemap tm in sceneTilemaps)
            {
                // 忽略明确不是障碍物的图层(比如路径层)
                if (!tm.name.ToLower().Contains("path") && 
                    !tm.name.ToLower().Contains("road") && 
                    !tm.name.ToLower().Contains("placement"))
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
        
        // 向后兼容：如果设置了单个obstacleTilemap但没有设置数组
        if ((obstacleTilemaps == null || obstacleTilemaps.Length == 0) && obstacleTilemap != null)
        {
            obstacleTilemaps = new Tilemap[] { obstacleTilemap };
        }
        
        // 检查设置是否有效
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0)
        {
            Debug.LogWarning("未找到障碍物Tilemap，障碍物系统将不可用");
        }
        
        // 查找障碍物UI，如果未直接设置
        if (obstacleUI == null)
        {
            obstacleUI = FindObjectOfType<ObstacleUI>();
        }
    }
    
    void Update()
    {
        // 检测鼠标点击，选中障碍物
        if (Input.GetMouseButtonDown(0))
        {
            // 如果点击在UI上则跳过
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
            
            SelectObstacleAtMousePosition();
        }
    }
    
    // 选中鼠标位置的障碍物
    public void SelectObstacleAtMousePosition()
    {
        if (obstacleTilemap == null) return;
        
        // 获取鼠标位置
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 将世界坐标转换为格子坐标
        Vector3Int cellPos = obstacleTilemap.WorldToCell(mouseWorldPos);
        
        // 检查是否是可清除的障碍物
        if (IsObstacle(cellPos) && !IsClearedObstacle(cellPos) && IsClearableObstacle(cellPos))
        {
            selectedObstaclePos = cellPos;
            Debug.Log($"选中了障碍物，位置：{cellPos}，清除费用：{clearCost}金币");
            
            // 通知UI更新
            UpdateObstacleUI(cellPos);
        }
        else
        {
            // 点击非障碍物区域，取消选择
            if (selectedObstaclePos.HasValue)
            {
                selectedObstaclePos = null;
                HideObstacleUI();
            }
        }
    }
    
    // 检查指定位置是否有障碍物（任何图层）
    public bool IsObstacle(Vector3Int position)
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return false;
        
        foreach (Tilemap tilemap in obstacleTilemaps)
        {
            if (tilemap != null && tilemap.GetTile(position) != null)
                return true;
        }
        
        return false;
    }
    
    // 获取指定位置的所有障碍物图层
    public List<Tilemap> GetObstacleTilemapsAtPosition(Vector3Int position)
    {
        List<Tilemap> tilemapsWithObstacle = new List<Tilemap>();
        
        if (obstacleTilemaps == null) return tilemapsWithObstacle;
        
        foreach (Tilemap tilemap in obstacleTilemaps)
        {
            if (tilemap != null && tilemap.GetTile(position) != null)
            {
                tilemapsWithObstacle.Add(tilemap);
            }
        }
        
        return tilemapsWithObstacle;
    }
    
    // 检查指定位置是否是已清除的障碍物
    public bool IsClearedObstacle(Vector3Int position)
    {
        return clearedObstacles.Contains(position);
    }
    
    // 检查指定位置是否是可清除的障碍物
    public bool IsClearableObstacle(Vector3Int position)
    {
        if (obstacleTilemaps == null || obstacleTilemaps.Length == 0) return false;
        
        foreach (Tilemap tilemap in obstacleTilemaps)
        {
            if (tilemap != null)
            {
                TileBase tile = tilemap.GetTile(position);
                if (tile != null)
                {
                    // 如果未设置专门的可清除Tile，则所有障碍物都可清除
                    if (clearableTile == null || tile == clearableTile)
                        return true;
                }
            }
        }
        
        return false;
    }
    
    // 清除当前选中的障碍物
    public bool ClearSelectedObstacle()
    {
        if (!selectedObstaclePos.HasValue) return false;
        
        Vector3Int position = selectedObstaclePos.Value;
        bool success = ClearObstacle(position);
        
        if (success)
        {
            // 清除成功，重置选择
            selectedObstaclePos = null;
            HideObstacleUI();
        }
        
        return success;
    }
    
    // 清除指定位置的障碍物
    public bool ClearObstacle(Vector3Int position)
    {
        // 1. 基本检查：位置有障碍物且未被清除
        if (!IsObstacle(position) || IsClearedObstacle(position)) 
            return false;
        
        // 1.1 获取该位置所有障碍物图层
        List<Tilemap> tilemapsWithObstacle = GetObstacleTilemapsAtPosition(position);
        if (tilemapsWithObstacle.Count == 0)
            return false;
        
        // 2. 检查并扣除金币
        bool hasEnoughCoins = false;
        
        // 优先使用CoinManager
        if (CoinManager.Instance != null)
        {
            hasEnoughCoins = CoinManager.Instance.HasEnoughCoins(clearCost);
            if (!hasEnoughCoins)
            {
                string message = $"金币不足，需要{clearCost}金币来清除此障碍物！";
                Debug.LogWarning(message);
                
                // 显示提示信息
                ShowNotification(message);
                return false;
            }
        }
        // 向后兼容：使用TowerManager
        else if (TowerManager.Instance != null)
        {
            hasEnoughCoins = TowerManager.Instance.currentGold >= clearCost;
            if (!hasEnoughCoins)
            {
                Debug.LogWarning($"金币不足，需要{clearCost}金币");
                return false;
            }
        }
        else
        {
            Debug.LogError("找不到CoinManager或TowerManager，无法扣除金币！");
            return false;
        }
        
        // 3. 隐藏所有图层中的障碍物（设置Tile为null）
        foreach (Tilemap tilemap in tilemapsWithObstacle)
        {
            tilemap.SetTile(position, null);
        }
        
        // 4. 记录已清除的障碍物位置（这样放置点可以检测到）
        clearedObstacles.Add(position);
        
        // 触发障碍物清除事件
        OnObstacleCleared?.Invoke(position);
        
        // 5. 扣除金币
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.TrySpendCoins(clearCost);
        }
        
        // 6. 查找与障碍物位置匹配的ObstaclePlacementPoint
        ObstaclePlacementPoint[] placementPoints = FindObjectsOfType<ObstaclePlacementPoint>();
        foreach (var point in placementPoints)
        {
            if (point.obstaclePosition == position)
            {
                // 启用该放置点
                point.EnablePoint();
                
                // 将放置点添加到TowerPlacementManager
                TowerPlacementManager placementManager = TowerPlacementManager.Instance;
                if (placementManager != null && !placementManager.placementPoints.Contains(point))
                {
                    placementManager.placementPoints.Add(point);
                    // 重新初始化放置点系统
                    placementManager.ReinitializePlacementPoints();
                }
                break;
            }
        }
        
        // 7. 可选：播放清除特效
        if (clearEffect != null)
        {
            // 使用第一个图层的位置来生成特效
            Vector3 worldPos = tilemapsWithObstacle[0].GetCellCenterWorld(position);
            GameObject effect = Instantiate(clearEffect, worldPos, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 显示成功清除提示
        ShowNotification($"已清除障碍物，消耗{clearCost}金币");
        
        Debug.Log($"已清除位置{position}的障碍物，影响了{tilemapsWithObstacle.Count}个图层");
        return true;
    }
    
    // 显示通知
    private void ShowNotification(string message)
    {
        // 尝试通过TowerManager显示通知
        if (TowerManager.Instance != null && TowerManager.Instance.GetType().GetMethod("ShowNotification") != null)
        {
            TowerManager.Instance.ShowNotification(message);
            return;
        }
        
        // 如果没有别的通知系统，仅输出到控制台
        Debug.Log(message);
    }
    
    // 显示障碍物UI（费用等）
    private void UpdateObstacleUI(Vector3Int position)
    {
        if (obstacleUI != null)
        {
            obstacleUI.ShowObstacleInfo(position);
        }
    }
    
    // 隐藏障碍物UI
    private void HideObstacleUI()
    {
        if (obstacleUI != null)
        {
            obstacleUI.HidePanel();
        }
    }
    
    // 检查位置是否可以放置塔（提供给TowerManager使用）
    public bool CanPlaceAtPosition(Vector3Int position)
    {
        return !IsObstacle(position) || IsClearedObstacle(position);
    }
} 