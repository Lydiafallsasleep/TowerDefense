using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 定义塔的放置点，可以在编辑器中预设
/// </summary>
public class TowerPlacementPoint : MonoBehaviour
{
    [Header("基本设置")]
    public bool isOccupied = false;        // 该点是否已被占用
    public bool isEnabled = true;          // 该点是否可用
    public string pointID;                 // 点的唯一ID
    public Vector3Int gridPosition;        // 对应的网格位置（如果基于网格）
    
    [Header("视觉效果")]
    public SpriteRenderer placementIndicator;  // 放置点的视觉指示器
    public Color availableColor = Color.green;
    public Color occupiedColor = Color.red;
    public Color disabledColor = Color.gray;
    
    [Header("分组设置")]
    public string placementGroupID;         // 点所属的组ID
    public int unlockLevel = 0;             // 需要解锁该点的关卡等级
    
    // 该点上放置的塔
    private BaseTower placedTower;

    private void Start()
    {
        UpdateVisualState();
    }
    
    /// <summary>
    /// 更新视觉状态
    /// </summary>
    public void UpdateVisualState()
    {
        if (placementIndicator != null)
        {
            if (!isEnabled)
            {
                placementIndicator.color = disabledColor;
            }
            else if (isOccupied)
            {
                placementIndicator.color = occupiedColor;
            }
            else
            {
                placementIndicator.color = availableColor;
            }
        }
    }
    
    /// <summary>
    /// 占用该点
    /// </summary>
    public void OccupyPoint(BaseTower tower)
    {
        isOccupied = true;
        placedTower = tower;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 释放该点
    /// </summary>
    public void ReleasePoint()
    {
        isOccupied = false;
        placedTower = null;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 启用该点
    /// </summary>
    public void EnablePoint()
    {
        isEnabled = true;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 禁用该点
    /// </summary>
    public void DisablePoint()
    {
        isEnabled = false;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 获取放置的塔
    /// </summary>
    public BaseTower GetPlacedTower()
    {
        return placedTower;
    }

    // 在场景中绘制辅助线，帮助开发者查看
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : (isEnabled ? Color.green : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 绘制ID
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, pointID);
    }
} 