using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 塔防放置点UI管理器，负责显示悬浮提示和视觉效果
/// </summary>
public class TowerPlacementUI : MonoBehaviour
{
    [Header("箭头指示器设置")]
    public GameObject arrowIndicator;   // 箭头指示器对象
    public float arrowHoverHeight = 1f; // 箭头悬浮高度
    public float arrowHorizontalOffset = 0f; // 水平偏移量
    
    [Header("UI设置")]
    public Canvas uiCanvas;             // UI画布引用
    public RectTransform arrowUIPrefab; // 箭头UI预制体（如果使用Canvas UI）
    public bool useCanvasUI = false;    // 是否使用Canvas UI显示箭头
    
    // 私有变量
    private RectTransform arrowUIInstance; // 箭头UI实例
    private TowerPlacementManager placementManager;
    private TowerPlacementPoint currentHoverPoint; // 当前悬浮的放置点
    private Camera mainCamera;
    private float maxRaycastDistance = 100f; // 最大射线检测距离
    
    private void Start()
    {
        // 获取引用
        placementManager = TowerPlacementManager.Instance;
        mainCamera = Camera.main;
        
        // 确保有箭头指示器
        SetupArrowIndicator();
        
        // 默认隐藏箭头
        HideArrowIndicator();
        
        if (placementManager == null)
        {
            Debug.LogError("未找到TowerPlacementManager！塔放置UI将无法正常工作。");
        }
    }
    
    private void Update()
    {
        // 检测鼠标悬停在放置点上的逻辑
        CheckMouseHover();
    }
    
    // 检查鼠标悬停
    private void CheckMouseHover()
    {
        // 检查是否有UI元素遮挡
        if (IsPointerOverUI())
        {
            HideArrowIndicator();
            return;
        }
        
        // 从鼠标位置发射射线
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // 检查射线是否击中物体
        if (Physics.Raycast(ray, out hit, maxRaycastDistance))
        {
            // 尝试获取击中物体上的TowerPlacementPoint组件
            TowerPlacementPoint point = hit.collider.GetComponent<TowerPlacementPoint>();
            
            // 如果没有直接获取到，尝试从父物体获取
            if (point == null)
            {
                point = hit.collider.GetComponentInParent<TowerPlacementPoint>();
            }
            
            // 找到可用的放置点
            if (point != null && point.isEnabled && !point.isOccupied)
            {
                // 显示箭头指示器
                ShowArrowIndicator(point);
                return;
            }
        }
        
        // 如果没有找到可用的放置点，尝试使用网格坐标查找
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos != Vector3.zero && placementManager != null)
        {
            // 获取最近的可用放置点
            TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
            if (nearestPoint != null)
            {
                // 计算距离
                float distance = Vector3.Distance(mouseWorldPos, nearestPoint.transform.position);
                if (distance < 1.0f) // 可以调整这个阈值
                {
                    // 显示箭头指示器
                    ShowArrowIndicator(nearestPoint);
                    return;
                }
            }
            // 如果没有找到可用的放置点或距离太远，记录日志（可选）
            // Debug.Log("未找到可用的放置点，或放置点距离太远");
        }
        
        // 如果没有找到可用的放置点，隐藏箭头
        HideArrowIndicator();
    }
    
    // 获取鼠标世界坐标
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        
        // 如果射线没有击中任何物体，尝试与一个假想的平面相交
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;
        
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);
        }
        
        return Vector3.zero;
    }
    
    // 设置箭头指示器
    private void SetupArrowIndicator()
    {
        if (arrowIndicator == null) return;
        
        SpriteRenderer renderer = arrowIndicator.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = arrowIndicator.AddComponent<SpriteRenderer>();
            Debug.Log("为箭头指示器添加了SpriteRenderer组件");
        }
        
        // 确保有一个默认图像
        if (renderer.sprite == null)
        {
            // 尝试加载一个默认箭头sprite
            Sprite arrowSprite = Resources.Load<Sprite>("Sprites/Arrow");
            if (arrowSprite != null)
            {
                renderer.sprite = arrowSprite;
            }
            else
            {
                Debug.LogWarning("无法加载默认箭头Sprite，请手动设置");
            }
        }
        
        // 设置渲染顺序
        renderer.sortingOrder = 100;
        
        // 固定箭头朝向（向左）
        arrowIndicator.transform.rotation = Quaternion.Euler(0, 0, 90);
    }
    
    // 显示箭头指示器
    private void ShowArrowIndicator(TowerPlacementPoint point)
    {
        if (point == null) return;
        
        // 更新当前悬停的放置点
        currentHoverPoint = point;
        
        // 获取放置点的位置
        Vector3 position = point.transform.position;
        
        // 根据设置选择使用世界空间箭头或UI箭头
        if (useCanvasUI)
        {
            ShowUIArrow(position);
        }
        else if (arrowIndicator != null)
        {
            ShowWorldSpaceArrow(position);
        }
    }
    
    // 显示世界空间箭头
    private void ShowWorldSpaceArrow(Vector3 position)
    {
        // 设置箭头位置在放置点的上方
        Vector3 arrowPos = position;
        arrowPos.y += arrowHoverHeight;
        arrowPos.x += arrowHorizontalOffset;
        
        // 调整Z坐标确保在相机视野内
        arrowPos.z = 0;
        
        // 应用位置
        arrowIndicator.transform.position = arrowPos;
        
        // 确保箭头旋转正确（向左指向）
        arrowIndicator.transform.rotation = Quaternion.Euler(0, 0, 90);
        
        // 显示箭头
        arrowIndicator.SetActive(true);
    }
    
    // 显示UI箭头
    private void ShowUIArrow(Vector3 worldPosition)
    {
        // 检查UI画布和预制体
        if (uiCanvas == null || arrowUIPrefab == null)
        {
            Debug.LogError("未设置UI画布或箭头UI预制体!");
            return;
        }
        
        // 如果箭头UI实例不存在，创建一个
        if (arrowUIInstance == null)
        {
            arrowUIInstance = Instantiate(arrowUIPrefab, uiCanvas.transform);
        }
        
        // 设置箭头UI为激活状态
        arrowUIInstance.gameObject.SetActive(true);
        
        // 将世界坐标转换为屏幕坐标，再转换为Canvas上的坐标
        if (mainCamera != null)
        {
            // 调整Y轴位置和X轴位置
            worldPosition.y += arrowHoverHeight;
            worldPosition.x += arrowHorizontalOffset;
            
            Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.GetComponent<RectTransform>(), 
                screenPoint, 
                uiCanvas.worldCamera, 
                out localPoint);
            
            // 设置UI箭头位置
            arrowUIInstance.anchoredPosition = localPoint;
            
            // 确保UI箭头旋转正确
            arrowUIInstance.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    // 隐藏箭头指示器
    private void HideArrowIndicator()
    {
        // 隐藏世界空间箭头
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(false);
        }
        
        // 隐藏UI箭头
        if (arrowUIInstance != null)
        {
            arrowUIInstance.gameObject.SetActive(false);
        }
        
        // 清除当前悬停的放置点
        currentHoverPoint = null;
    }
    
    // 检查指针是否在UI元素上
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}