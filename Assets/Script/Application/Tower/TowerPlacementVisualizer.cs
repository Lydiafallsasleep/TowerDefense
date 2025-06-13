using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 添加UI命名空间

/// <summary>
/// 管理塔放置点的可见性，当鼠标悬停时显示放置点
/// </summary>
public class TowerPlacementVisualizer : MonoBehaviour
{
    [Header("参考")]
    public TowerPlacementManager placementManager; // 塔放置管理器
    public TowerManager towerManager; // 塔管理器引用
    
    [Header("设置")]
    public float hoverCheckRadius = 15.0f; // 鼠标悬停检测半径
    public float hoverYOffset = 0.5f;     // 悬停显示时的Y轴偏移
    public float showAnimationSpeed = 8f; // 显示动画速度
    public float hideAnimationSpeed = 5f; // 隐藏动画速度
    public Sprite placementIndicatorSprite; // 自定义的放置点指示器Sprite
    
    [Header("选择指示器")]
    public GameObject selectionCursorPrefab; // 选中位置时显示的光标预制体
    public Vector3 cursorOffset = new Vector3(0, 0.5f, 0); // 光标相对于放置点的偏移量
    public float blinkSpeed = 2.0f; // 闪烁速度
    public float minAlpha = 0.5f; // 最小透明度
    public float maxAlpha = 1.0f; // 最大透明度
    private GameObject selectionCursor; // 当前显示的选择光标实例
    private TowerPlacementPoint selectedPoint; // 当前选中的放置点
    private float blinkTimer = 0f; // 闪烁计时器
    
    [Header("调试设置")]
    public bool showDebugInfo = true;      // 是否显示调试信息
    public bool logDebugInfo = true;       // 是否记录调试信息
    public GUIStyle debugTextStyle;        // 调试文本样式
    
    [Header("塔建造按钮")]
    public Button arrowTowerButton; // 箭塔按钮
    public Button cannonTowerButton; // 炮塔按钮
    public Button laserTowerButton; // 激光塔按钮
    public GameObject buildButtonsPanel; // 建造按钮面板
    
    // 所有放置点的可视化对象
    private Dictionary<TowerPlacementPoint, GameObject> pointVisuals = new Dictionary<TowerPlacementPoint, GameObject>();
    
    // 当前悬停的放置点
    private TowerPlacementPoint currentHoverPoint;
    
    // 存储原始位置
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    
    // 调试信息
    private string debugText = "";
    private Vector3 mouseWorldPosition;
    private TowerPlacementPoint nearestPoint;
    private float distanceToNearest;
    
    void Start()
    {
        // 查找塔放置管理器（如果未指定）
        if (placementManager == null)
        {
            placementManager = FindObjectOfType<TowerPlacementManager>();
            
            if (placementManager == null)
            {
                Debug.LogError("TowerPlacementVisualizer: 未找到TowerPlacementManager!");
                return;
            }
        }
        
        // 查找塔管理器（如果未指定）
        if (towerManager == null)
        {
            towerManager = FindObjectOfType<TowerManager>();
            
            if (towerManager == null)
            {
                Debug.LogError("TowerPlacementVisualizer: 未找到TowerManager!");
                return;
            }
        }
        
        // 初始化塔建造按钮
        InitializeTowerButtons();
        
        // 初始化调试文本样式
        if (debugTextStyle == null)
        {
            debugTextStyle = new GUIStyle();
            debugTextStyle.normal.textColor = Color.white;
            debugTextStyle.fontSize = 16;
            debugTextStyle.fontStyle = FontStyle.Bold;
            debugTextStyle.alignment = TextAnchor.UpperLeft;
            debugTextStyle.wordWrap = true;
        }
        
        // 加载默认的指示器精灵(如果未指定)
        if (placementIndicatorSprite == null)
        {
            placementIndicatorSprite = Resources.Load<Sprite>("UI/PlacementIndicator");
            if (placementIndicatorSprite == null)
            {
                Debug.LogWarning("未找到放置指示器精灵，请在Inspector中指定或确保Resources/UI/PlacementIndicator存在");
            }
        }
        
        // 初始化所有放置点的可视化
        InitializePointVisuals();
        
        // 初始化选择光标
        InitializeSelectionCursor();
        
        // 确保建造按钮面板一直可见
        if (buildButtonsPanel != null)
        {
            buildButtonsPanel.SetActive(true);
        }
        
        // 获取TowerManager实例
        if (towerManager == null)
        {
            towerManager = TowerManager.Instance;
        }
    }
    
    void Update()
    {
        // 检测鼠标悬停
        CheckMouseHover();
        
        // 检测鼠标点击
        CheckMouseClick();
        
        // 更新可视化对象的动画
        UpdateVisuals();
        
        // 更新光标闪烁效果
        UpdateCursorBlink();
        
        // 更新调试信息
        if (showDebugInfo || logDebugInfo)
        {
            UpdateDebugInfo();
        }
        
        // 更新建造按钮状态
        UpdateBuildButtonsState();
    }
    
    // 在屏幕上绘制调试信息
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.Label(new Rect(10, 10, Screen.width / 3, Screen.height / 2), debugText, debugTextStyle);
    }
    
    // 初始化选择光标
    private void InitializeSelectionCursor()
    {
        // 如果没有指定预制体，尝试加载默认的
        if (selectionCursorPrefab == null)
        {
            selectionCursorPrefab = Resources.Load<GameObject>("UI/SelectionCursor");
            
            // 如果仍然没有找到，创建一个简单的光标
            if (selectionCursorPrefab == null)
            {
                Debug.LogWarning("未找到选择光标预制体，将创建一个简单的光标");
                
                // 创建简单的光标游戏对象
                GameObject simpleCursor = new GameObject("SimpleCursor");
                SpriteRenderer renderer = simpleCursor.AddComponent<SpriteRenderer>();
                
                // 创建一个简单的十字形状
                Texture2D texture = new Texture2D(64, 64);
                Color[] colors = new Color[64 * 64];
                
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        // 创建十字形状
                        if ((x >= 28 && x <= 36) || (y >= 28 && y <= 36))
                        {
                            colors[y * 64 + x] = new Color(1f, 0.5f, 0f, 0.8f); // 橙色半透明
                        }
                        else
                        {
                            colors[y * 64 + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(colors);
                texture.Apply();
                
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
                renderer.sprite = sprite;
                renderer.sortingOrder = 20; // 确保显示在最上层
                
                // 创建临时预制体
                GameObject tempCursor = new GameObject("TempCursor");
                tempCursor.SetActive(false);
                DontDestroyOnLoad(tempCursor);
                
                // 将simpleCursor设为tempCursor的子对象
                simpleCursor.transform.SetParent(tempCursor.transform);
                
                // 保存为预制体引用
                selectionCursorPrefab = tempCursor;
            }
            else
            {
                Debug.Log("已从Resources/UI/SelectionCursor加载选择光标预制体");
            }
        }
        else
        {
            Debug.Log($"已使用Inspector中设置的选择光标预制体: {selectionCursorPrefab.name}");
        }
    }
    
    // 初始化所有放置点的可视化
    private void InitializePointVisuals()
    {
        if (placementManager == null || placementManager.placementPoints == null) return;
        
        Debug.Log($"TowerPlacementVisualizer: 初始化 {placementManager.placementPoints.Count} 个放置点的可视化");
        
        foreach (TowerPlacementPoint point in placementManager.placementPoints)
        {
            if (point == null || !point.isEnabled) continue;
            
            // 检查是否已有视觉指示器
            GameObject visual = null;
            
            if (point.placementIndicator != null)
            {
                // 使用现有的指示器
                visual = point.placementIndicator.gameObject;
            }
            else
            {
                // 创建新的视觉指示器
                visual = CreateVisualIndicator(point);
            }
            
            if (visual != null)
            {
                // 添加到字典
                pointVisuals[point] = visual;
                
                // 存储原始位置
                originalPositions[visual] = visual.transform.position;
                
                // 初始隐藏
                visual.SetActive(false);
            }
        }
    }
    
    // 创建视觉指示器
    private GameObject CreateVisualIndicator(TowerPlacementPoint point)
    {
        // 创建指示器物体
        GameObject indicator = new GameObject($"Indicator_{point.pointID}");
        indicator.transform.position = point.transform.position;
        indicator.transform.SetParent(point.transform);
        
        // 添加精灵渲染器
        SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
        
        // 使用自定义Sprite或加载默认的
        if (placementIndicatorSprite != null)
        {
            renderer.sprite = placementIndicatorSprite;
        }
        else
        {
            // 创建一个简单的圆形精灵作为后备方案
            Debug.LogWarning("未找到放置指示器精灵，使用默认圆形");
            
            // 设置默认颜色
            renderer.color = point.availableColor;
            
            // 添加圆形精灵
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
                    if (distanceFromCenter < 60)
                    {
                        colors[y * 128 + x] = Color.white;
                    }
                    else
                    {
                        colors[y * 128 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            renderer.sprite = sprite;
        }
        
        // 设置排序层级
        renderer.sortingOrder = 10;
        
        // 设置指示器引用
        point.placementIndicator = renderer;
        
        return indicator;
    }
    
    // 检测鼠标悬停
    private void CheckMouseHover()
    {
        // 检查是否有UI遮挡
        if (IsPointerOverUI())
        {
            // 如果鼠标在UI上，取消当前悬停
            if (currentHoverPoint != null)
            {
                if (pointVisuals.TryGetValue(currentHoverPoint, out GameObject visual))
                {
                    visual.SetActive(false);
                }
                
                currentHoverPoint = null;
            }
            
            // 更新调试信息
            debugText = "鼠标在UI上，无法检测放置点";
            
            return;
        }
        
        // 获取鼠标位置 - 使用相机转换
        Vector3 mousePosition = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePosition);
        mouseWorldPos.z = 0; // 确保z坐标为0，与2D平面对齐
        
        // 保存鼠标世界坐标用于调试
        mouseWorldPosition = mouseWorldPos;
        
        // 找到最近的放置点
        nearestPoint = FindNearestPlacementPoint(mouseWorldPosition);
        distanceToNearest = nearestPoint != null ? 
            Vector3.Distance(mouseWorldPosition, nearestPoint.transform.position) : 
            float.MaxValue;
        
        // 当点击鼠标时输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            LogPlacementPointInfo();
        }
        
        // 如果找到了有效的放置点，并且与当前悬停点不同
        if (nearestPoint != null && nearestPoint != currentHoverPoint)
        {
            // 隐藏之前的悬停点
            if (currentHoverPoint != null && pointVisuals.TryGetValue(currentHoverPoint, out GameObject oldVisual))
            {
                oldVisual.SetActive(false);
            }
            
            // 显示新的悬停点
            if (pointVisuals.TryGetValue(nearestPoint, out GameObject visual))
            {
                visual.SetActive(true);
                
                // 确保指示器位于放置点中心
                visual.transform.position = nearestPoint.transform.position + new Vector3(0, hoverYOffset, 0);
            }
            
            // 更新当前悬停点
            currentHoverPoint = nearestPoint;
        }
        // 如果鼠标离开了当前悬停点的范围
        else if (nearestPoint == null && currentHoverPoint != null)
        {
            // 隐藏当前悬停点
            if (pointVisuals.TryGetValue(currentHoverPoint, out GameObject visual))
            {
                visual.SetActive(false);
            }
            
            // 清除当前悬停点
            currentHoverPoint = null;
        }
    }
    
    // 检测鼠标点击
    private void CheckMouseClick()
    {
        // 如果点击位于UI上，直接忽略，防止误清除选中
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 如果点击了鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            // 如果当前有悬停的放置点，且不是UI点击
            if (nearestPoint != null && !IsPointerOverUI())
            {
                // 显示选择光标
                ShowSelectionCursor(mouseWorldPosition, nearestPoint);
                
                // 记录选中的放置点
                selectedPoint = nearestPoint;
                
                // 显示建造按钮面板
                //ShowBuildButtonsPanel();
                
                // 通知塔管理器选中了放置点
                if (towerManager != null)
                {
                    towerManager.SelectPlacementPoint(selectedPoint);
                }
                
                // 在控制台输出选中信息
                Debug.Log($"选中了放置点: {nearestPoint.pointID}, 位置: {nearestPoint.transform.position}，是否可用{nearestPoint.isEnabled},{nearestPoint.isOccupied}");
            }
            else if (!IsPointerOverUI())
            {
                // 如果点击了空白区域，隐藏选择光标和建造按钮
                HideSelectionCursor();
                //HideBuildButtonsPanel();
                selectedPoint = null;
                
                // 通知塔管理器取消选中
                if (towerManager != null)
                {
                    towerManager.DeselectPlacementPoint();
                }
            }
        }
    }
    
    // 显示选择光标
    private void ShowSelectionCursor(Vector3 position, TowerPlacementPoint point)
    {
        // 如果已经有光标实例，先销毁它
        HideSelectionCursor();
        
        // 创建新的光标实例
        if (selectionCursorPrefab != null)
        {
            // 使用放置点的位置加上偏移量
            Vector3 placementPosition = point.transform.position + cursorOffset;
            
            // 实例化预设的光标
            selectionCursor = Instantiate(selectionCursorPrefab, placementPosition, Quaternion.identity);
            selectionCursor.name = "SelectionCursor";
            selectionCursor.SetActive(true); // 确保光标被激活
            
            // 确保所有子对象也被激活
            foreach (Transform child in selectionCursor.transform)
            {
                child.gameObject.SetActive(true);
            }
            
            // 确保光标显示在正确的层级
            SpriteRenderer renderer = selectionCursor.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 15; // 确保显示在悬停指示器之上
            }
            else
            {
                // 如果主对象没有SpriteRenderer，查找子对象
                SpriteRenderer[] childRenderers = selectionCursor.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer childRenderer in childRenderers)
                {
                    childRenderer.sortingOrder = 15;
                }
            }
            
            Debug.Log($"选择光标已显示在放置点 {point.pointID} 位置: {placementPosition}，使用预制体: {selectionCursorPrefab.name}，应用偏移量: {cursorOffset}");
        }
        else
        {
            Debug.LogError("无法创建选择光标：未找到预制体");
        }
    }
    
    // 隐藏选择光标
    private void HideSelectionCursor()
    {
        if (selectionCursor != null)
        {
            Destroy(selectionCursor);
            selectionCursor = null;
        }
    }
    
    // 查找最近的放置点
    private TowerPlacementPoint FindNearestPlacementPoint(Vector3 worldPosition)
    {
        if (placementManager == null || placementManager.placementPoints == null) return null;
        
        TowerPlacementPoint nearestPoint = null;
        float nearestDistance = hoverCheckRadius; // 使用设置的检测半径(15)
        
        foreach (TowerPlacementPoint point in placementManager.placementPoints)
        {
            if (point == null || !point.isEnabled) continue;
            
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
    
    // 更新可视化对象的动画
    private void UpdateVisuals()
    {
        foreach (var pair in pointVisuals)
        {
            TowerPlacementPoint point = pair.Key;
            GameObject visual = pair.Value;
            
            if (point == null || visual == null) continue;
            
            // 如果是当前悬停的点，显示动画
            if (point == currentHoverPoint && visual.activeSelf)
            {
                // 获取原始位置
                if (originalPositions.TryGetValue(visual, out Vector3 originalPos))
                {
                    // 计算目标位置（上浮）
                    Vector3 targetPos = new Vector3(
                        point.transform.position.x,
                        point.transform.position.y + hoverYOffset,
                        point.transform.position.z
                    );
                    
                    // 平滑移动到目标位置
                    visual.transform.position = Vector3.Lerp(
                        visual.transform.position, 
                        targetPos, 
                        Time.deltaTime * showAnimationSpeed
                    );
                }
            }
            // 如果不是当前悬停的点，但仍然可见（正在隐藏），恢复到原始位置
            else if (visual.activeSelf)
            {
                // 获取原始位置
                if (originalPositions.TryGetValue(visual, out Vector3 originalPos))
                {
                    // 平滑移动回原始位置
                    visual.transform.position = Vector3.Lerp(
                        visual.transform.position, 
                        originalPos, 
                        Time.deltaTime * hideAnimationSpeed
                    );
                    
                    // 如果已经非常接近原始位置，隐藏
                    if (Vector3.Distance(visual.transform.position, originalPos) < 0.01f)
                    {
                        visual.SetActive(false);
                    }
                }
            }
        }
    }
    
    // 在控制台输出放置点信息
    private void LogPlacementPointInfo()
    {
        string text = $"鼠标位置: {mouseWorldPosition.x:F2}, {mouseWorldPosition.y:F2}, {mouseWorldPosition.z:F2}";
        
        if (nearestPoint != null)
        {
            // 计算XY平面距离
            float dx = mouseWorldPosition.x - nearestPoint.transform.position.x;
            float dy = mouseWorldPosition.y - nearestPoint.transform.position.y;
            float xyDistance = Mathf.Sqrt(dx * dx + dy * dy);
            
            bool isOnPlacementPoint = xyDistance <= hoverCheckRadius;
            
            text += $"\n放置点: {nearestPoint.pointID}" +
                    $"\nXY距离: {xyDistance:F2}" +
                    $"\n3D距离: {distanceToNearest:F2}" +
                    $"\n状态: {(nearestPoint.isOccupied ? "已占用" : nearestPoint.isEnabled ? "可用" : "已禁用")}" +
                    $"\n判断: {(isOnPlacementPoint ? "鼠标在放置点上" : "鼠标不在放置点上(超出检测半径)")}";
        }
        else
        {
            text += "\n没有找到附近的放置点";
        }
        
        Debug.Log(text);
    }
    
    // 检查鼠标是否在UI上
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    // 更新调试信息
    private void UpdateDebugInfo()
    {
        string text = $"鼠标世界坐标: {mouseWorldPosition.x:F2}, {mouseWorldPosition.y:F2}, {mouseWorldPosition.z:F2}\n";
        
        if (nearestPoint != null)
        {
            // 计算XY平面距离
            float dx = mouseWorldPosition.x - nearestPoint.transform.position.x;
            float dy = mouseWorldPosition.y - nearestPoint.transform.position.y;
            float xyDistance = Mathf.Sqrt(dx * dx + dy * dy);
            
            text += $"最近放置点: {nearestPoint.pointID}\n";
            text += $"XY距离: {xyDistance:F2}\n";
            text += $"3D距离: {distanceToNearest:F2}\n";
            text += $"状态: {(nearestPoint.isOccupied ? "已占用" : nearestPoint.isEnabled ? "可用" : "已禁用")}\n";
            
            if (xyDistance <= hoverCheckRadius)
            {
                text += "结果: 鼠标在放置点上\n";
            }
            else
            {
                text += "结果: 鼠标不在放置点上\n";
            }
        }
        else
        {
            text += "没有找到附近的放置点\n";
        }
        
        if (selectedPoint != null)
        {
            text += $"已选中放置点: {selectedPoint.pointID}\n";
        }
        
        debugText = text;
    }
    
    // 获取当前选中的放置点
    public TowerPlacementPoint GetSelectedPoint()
    {
        return selectedPoint;
    }
    
    // 清除当前选择
    public void ClearSelection()
    {
        selectedPoint = null;
        HideSelectionCursor();
        
        // 通知塔管理器取消选中
        if (towerManager != null)
        {
            towerManager.DeselectPlacementPoint();
        }
    }
    
    // 更新光标闪烁效果
    private void UpdateCursorBlink()
    {
        if (selectionCursor == null) return;
        
        // 更新闪烁计时器
        blinkTimer += Time.deltaTime * blinkSpeed;
        
        // 计算当前透明度值 (使用正弦波在minAlpha和maxAlpha之间变化)
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(blinkTimer) + 1) * 0.5f);
        
        // 更新所有渲染器的透明度
        SpriteRenderer[] renderers = selectionCursor.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
    }
    
    // 初始化塔建造按钮
    private void InitializeTowerButtons()
    {
        if (buildButtonsPanel == null)
        {
            Debug.LogWarning("未设置建造按钮面板，请在Inspector中指定");
        }
        
        // 设置按钮点击事件
        if (arrowTowerButton != null)
        {
            arrowTowerButton.onClick.AddListener(() => BuildTower(TowerType.Arrow));
        }
        else
        {
            Debug.LogWarning("未设置箭塔按钮，请在Inspector中指定");
        }
        
        if (cannonTowerButton != null)
        {
            cannonTowerButton.onClick.AddListener(() => BuildTower(TowerType.Cannon));
        }
        else
        {
            Debug.LogWarning("未设置炮塔按钮，请在Inspector中指定");
        }
        
        if (laserTowerButton != null)
        {
            laserTowerButton.onClick.AddListener(() => BuildTower(TowerType.Laser));
        }
        else
        {
            Debug.LogWarning("未设置激光塔按钮，请在Inspector中指定");
        }
    }
    
    // 建造塔
    private void BuildTower(TowerType towerType)
    {
        Debug.Log("[TowerPlacementVisualizer] BuildTower: 准备建造塔类型 " + towerType + " 在点 " + 
            (selectedPoint != null ? selectedPoint.pointID : "null"));

        if (selectedPoint != null && towerManager != null)
        {
            Debug.Log("[TowerPlacementVisualizer] BuildTower: 选择塔类型 " + towerType);
            // 确保TowerManager也知道选中了哪个点
            towerManager.SelectPlacementPoint(selectedPoint);
            
            // 选择塔类型
            towerManager.SelectTowerType(towerType);
            
            Debug.Log("[TowerPlacementVisualizer] BuildTower: 调用 BuildTowerOnSelectedPoint()");
            // 建造塔
            towerManager.BuildTowerOnSelectedPoint();
            
            Debug.Log("[TowerPlacementVisualizer] BuildTower: 清理UI，隐藏光标");
            // 隐藏选择光标，但保持建造按钮面板可见
            HideSelectionCursor();
            selectedPoint = null;
        }
        else
        {
            Debug.LogWarning("[TowerPlacementVisualizer] BuildTower: 无法建造 - " + 
                (selectedPoint == null ? "selectedPoint为null" : "towerManager为null"));
        }
    }
    
    // 检查是否有足够的金币建造指定类型的塔
    private bool HasEnoughGoldForTower(TowerType towerType)
    {
        int cost = 0;
        switch (towerType)
        {
            case TowerType.Arrow:
                GameObject arrowPrefab = towerManager.arrowTowerPrefab;
                if (arrowPrefab != null)
                {
                    BaseTower tower = arrowPrefab.GetComponent<BaseTower>();
                    if (tower != null)
                    {
                        cost = tower.cost;
                    }
                }
                break;
            case TowerType.Cannon:
                GameObject cannonPrefab = towerManager.cannonTowerPrefab;
                if (cannonPrefab != null)
                {
                    BaseTower tower = cannonPrefab.GetComponent<BaseTower>();
                    if (tower != null)
                    {
                        cost = tower.cost;
                    }
                }
                break;
            case TowerType.Laser:
                GameObject laserPrefab = towerManager.laserTowerPrefab;
                if (laserPrefab != null)
                {
                    BaseTower tower = laserPrefab.GetComponent<BaseTower>();
                    if (tower != null)
                    {
                        cost = tower.cost;
                    }
                }
                break;
        }
        if (CoinManager.Instance != null)
        {
            return CoinManager.Instance.HasEnoughCoins(cost);
        }
        return false;
    }
    
    // 更新建造按钮状态
    private void UpdateBuildButtonsState()
    {
        if (buildButtonsPanel != null && towerManager != null)
        {
            bool canBuildAtPoint = selectedPoint != null && towerManager.CanPlaceTowerAtPoint(selectedPoint);
            
            // 箭塔按钮
            if (arrowTowerButton != null)
            {
                arrowTowerButton.interactable = canBuildAtPoint && HasEnoughGoldForTower(TowerType.Arrow);
            }
            
            // 炮塔按钮
            if (cannonTowerButton != null)
            {
                cannonTowerButton.interactable = canBuildAtPoint && HasEnoughGoldForTower(TowerType.Cannon);
            }
            
            // 激光塔按钮
            if (laserTowerButton != null)
            {
                laserTowerButton.interactable = canBuildAtPoint && HasEnoughGoldForTower(TowerType.Laser);
            }
        }
    }
} 