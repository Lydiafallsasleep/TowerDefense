using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 障碍物分组，用于管理一组相关联的障碍物
/// </summary>
[System.Serializable]
public class ObstacleGroup
{
    public string groupName;           // 分组名称
    public int clearCost;             // 清除整组所需的金币
    public List<Vector3Int> positions; // 包含的障碍物位置
    public TileBase obstacleTile;     // 障碍物使用的瓦片
    public bool requireAllClear;      // 是否要求全部清除才算完成
    public bool useGroupCost;         // 是否使用组成本而非单个成本
    
    // 已清除的位置数量
    private int clearedCount;
    
    // 判断是否全部清除
    public bool IsFullyCleared => clearedCount >= positions.Count;
    
    // 判断是否部分清除
    public bool IsPartiallyCleared => clearedCount > 0;
    
    // 获取实际清除成本
    public int GetClearCost(int defaultCost)
    {
        // 如果使用组成本，且尚未全部清除，返回组成本
        // 否则使用每个障碍物的默认成本
        if (useGroupCost && !IsFullyCleared)
            return clearCost;
        else
            return defaultCost;
    }
    
    // 增加已清除计数
    public void AddCleared()
    {
        clearedCount++;
    }
    
    // 检查指定位置是否属于此组
    public bool ContainsPosition(Vector3Int position)
    {
        return positions.Contains(position);
    }
} 