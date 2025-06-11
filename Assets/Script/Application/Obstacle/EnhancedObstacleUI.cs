using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro; // 添加TextMeshPro命名空间
using System.Collections.Generic;

// 添加类型别名，简化代码
using ObstacleType = EnhancedObstacleManager.ObstacleType;

/// <summary>
/// 增强版的障碍物UI，支持显示障碍物组和类型信息
/// </summary>
public class EnhancedObstacleUI : MonoBehaviour
{
    [Header("UI元素")]
    public GameObject obstaclePanel;    // 障碍物信息面板
    public TMP_Text titleText;          // 标题文本(TMP)
    public TMP_Text infoText;           // 信息文本(TMP)
    public TMP_Text costText;           // 成本文本(TMP)
    public Button clearButton;          // 清除按钮（金币足够时显示）
    public Button insufficientButton;   // 金币不足时显示的按钮
    public Image typeIcon;              // 障碍物类型图标

    [Header("箭头指示器设置")]
    public GameObject arrowIndicator;   // 箭头指示器对象
    public float arrowHoverHeight = 1f; // 箭头悬浮高度
    public float arrowHorizontalOffset = 0f; // 水平偏移量
    public bool useCanvasUI = false;    // 是否使用Canvas UI显示箭头
    public Canvas uiCanvas;             // UI画布引用
    public RectTransform arrowUIPrefab; // 箭头UI预制体
    private RectTransform arrowUIInstance; // 箭头UI实例
    private bool arrowFixed = false;    // 箭头是否已固定
    private ObstacleGroup currentHoverGroup; // 当前悬浮的障碍物组
    
    [Header("组显示设置")]
    public GameObject groupInfoPanel;   // 组信息面板
    public TMP_Text groupNameText;      // 组名称文本(TMP)
    public TMP_Text groupProgressText;  // 组进度文本(TMP)
    public Image groupProgressBar;      // 组进度条
    public Toggle showGroupToggle;      // 是否显示组中所有障碍物的切换按钮
    
    // 障碍物管理器引用
    private EnhancedObstacleManager obstacleManager;
    
    // 当前选中的障碍物
    private Vector3Int selectedPosition;
    private EnhancedObstacleManager.ObstacleTypeInfo selectedTypeInfo;
    private ObstacleGroup selectedGroup;
    private int clearCost;
    
    // 高亮显示相关
    private Tilemap highlightTilemap;
    private TileBase highlightTile;
    
    // 引用清除面板
    [Header("清除面板设置")]
    public GameObject clearPanel;     // 清除面板主对象
    public GameObject regularClearBtn; // 金币足够时的清除按钮
    public GameObject unableClearBtn;  // 金币不足时的按钮
    public Button cancelButton;        // 取消按钮
    public TMP_Text typeText;         // 障碍物类型文本
    public TMP_Text costValueText;    // 成本值文本
    public TMP_Text unableClearText;  // 金币不足时显示的文本(TMP)
    private PanelFollower panelFollower; // 面板跟随组件
    
    void Start()
    {
        // 查找障碍物管理器
        obstacleManager = FindObjectOfType<EnhancedObstacleManager>();
        
        if (obstacleManager == null)
        {
            Debug.LogError("未找到EnhancedObstacleManager组件！障碍物UI将无法正常工作。");
        }
        
        // 初始化箭头指示器
        SetupArrowIndicator();
        
        // 获取清除面板跟随组件
        panelFollower = clearPanel.GetComponent<PanelFollower>();
        
        // 设置取消按钮点击事件
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        
        // 强制检查一次障碍物组，确保它们被正确初始化
        CheckObstacleGroups();
        
        // 创建用于高亮显示的Tilemap
        CreateHighlightTilemap();
        
        // 初始隐藏面板
        if (obstaclePanel != null)
        {
            obstaclePanel.SetActive(false);
        }
        
        // 初始隐藏清除面板
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
            
            // 检查并添加面板跟随组件
            panelFollower = clearPanel.GetComponent<PanelFollower>();
            if (panelFollower == null)
            {
                panelFollower = clearPanel.AddComponent<PanelFollower>();
                panelFollower.panelRect = clearPanel.GetComponent<RectTransform>();
                panelFollower.offset = new Vector3(0, 1.5f, 0); // 适当的偏移量使面板位于物体上方
            }
        }
        
        if (groupInfoPanel != null)
        {
            groupInfoPanel.SetActive(false);
        }
        
        // 设置按钮的禁用视觉效果
        SetupButtonDisabledState();
        
        // 添加按钮监听
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearButtonClicked);
        }
        
        if (showGroupToggle != null)
        {
            showGroupToggle.onValueChanged.AddListener(OnShowGroupToggled);
        }
        
        // 为清除面板按钮添加监听
        if (regularClearBtn != null)
        {
            Button regularBtn = regularClearBtn.GetComponent<Button>();
            if (regularBtn != null)
            {
                // 移除可能存在的旧监听器以避免重复
                regularBtn.onClick.RemoveAllListeners();
                regularBtn.onClick.AddListener(OnPanelClearButtonClicked);
                Debug.Log($"为regularClearBtn添加了点击监听器：{regularClearBtn.name}");
            }
            else
            {
                Debug.LogError($"regularClearBtn上没有Button组件：{regularClearBtn.name}");
            }
        }
        else
        {
            Debug.LogError("regularClearBtn引用为空！无法添加监听器");
        }
        
        Debug.Log("EnhancedObstacleUI初始化完成，箭头指示器状态: " + 
                 (arrowIndicator != null ? arrowIndicator.name + " (已加载)" : "未加载"));
    }
    
    // 检查障碍物组是否正确初始化
    private void CheckObstacleGroups()
    {
        if (obstacleManager == null || obstacleManager.obstacleGroups == null) return;
        
        Debug.Log("检查障碍物组初始化状态...");
        
        int emptyGroups = 0;
        int validGroups = 0;
        
        foreach (var group in obstacleManager.obstacleGroups)
        {
            if (group == null)
            {
                Debug.LogWarning("发现空的障碍物组引用!");
                emptyGroups++;
                continue;
            }
            
            if (group.positions == null || group.positions.Count == 0)
            {
                Debug.LogWarning($"障碍物组 '{group.groupName}' 没有任何位置!");
                emptyGroups++;
            }
            else
            {
                validGroups++;
                Debug.Log($"有效的障碍物组: '{group.groupName}' 包含 {group.positions.Count} 个位置");
            }
        }
        
        Debug.Log($"障碍物组检查完成: {validGroups} 个有效组, {emptyGroups} 个空组");
        
        // 如果没有有效的障碍物组，尝试创建一个测试组
        if (validGroups == 0)
        {
            Debug.LogWarning("没有找到有效的障碍物组，将尝试创建一个测试组！");
            CreateTestObstacleGroup();
        }
    }
    
    // 创建一个测试障碍物组用于调试
    private void CreateTestObstacleGroup()
    {
        if (obstacleManager == null) return;
        
        // 查找一个有效的Tilemap用于坐标转换
        Tilemap tilemap = null;
        if (obstacleManager.obstacleTilemaps != null && obstacleManager.obstacleTilemaps.Length > 0)
        {
            tilemap = obstacleManager.obstacleTilemaps[0];
        }
        
        if (tilemap == null)
        {
            Debug.LogError("无法创建测试组：没有找到有效的Tilemap!");
            return;
        }
        
        // 获取当前场景中的一些位置作为测试
        List<Vector3Int> testPositions = new List<Vector3Int>();
        
        // 从当前位置开始，添加一个3x3的方块作为测试区域
        Vector3 centerPos = Camera.main.transform.position;
        centerPos.z = 0;
        
        Vector3Int centerCell = tilemap.WorldToCell(centerPos);
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int pos = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);
                testPositions.Add(pos);
                
                // 在该位置设置一个Tile，确保它能被检测到
                TileBase testTile = tilemap.GetTile(pos);
                if (testTile == null && obstacleManager.obstacleTypes != null && obstacleManager.obstacleTypes.Length > 0)
                {
                    // 使用第一个可用的障碍物类型的第一个tile
                    var firstType = obstacleManager.obstacleTypes[0];
                    if (firstType.tiles != null && firstType.tiles.Length > 0)
                    {
                        tilemap.SetTile(pos, firstType.tiles[0]);
                        Debug.Log($"在位置 {pos} 设置了测试瓦片");
                    }
                }
            }
        }
        
        // 创建并添加测试组
        ObstacleGroup testGroup = obstacleManager.CreateGroup("TestGroup", testPositions, 10);
        
        Debug.Log($"已创建测试障碍物组，包含 {testPositions.Count} 个位置，中心位置：{centerCell}");
        
        // 手动检查这些位置是否被正确识别
        foreach (var pos in testPositions)
        {
            ManualCheckPosition(pos);
        }
    }
    
    // 设置箭头指示器的渲染属性
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
                Debug.Log("已加载默认箭头Sprite");
            }
            else
            {
                Debug.LogWarning("无法加载默认箭头Sprite，请手动设置");
            }
        }
        
        // 设置渲染顺序
        renderer.sortingOrder = 100;
        
        // 确保箭头位于UI前面
        if (Camera.main != null)
        {
            // 调整位置，确保在相机前方
            Vector3 pos = arrowIndicator.transform.position;
            pos.z = Camera.main.transform.position.z + 1;  // 确保在相机前方
            arrowIndicator.transform.position = pos;
        }
    }
    
    // 保存鼠标点击的位置
    private Vector3 clickPosition;
    
    void Update()
    {
        // 检测鼠标悬停在障碍物上的逻辑
        if (!arrowFixed)
        {
            CheckMouseHover();
        }
        
        // 检测鼠标点击固定箭头并显示面板
        if (currentHoverGroup != null && Input.GetMouseButtonDown(0))
        {
            // 添加这个检查：如果点击在UI上则不处理障碍物选择
            if (IsPointerOverUI())
            {
                return; // 不处理点击事件，因为点击在UI元素上
            }
            
            // 原有的点击处理代码...
            clickPosition = Input.mousePosition;
            // 其余代码不变...
            
            // 记录当前悬停的障碍物组
            FixArrowToGroup();
            
            // 获取鼠标点击的世界位置
            Ray ray = Camera.main.ScreenPointToRay(clickPosition);
            RaycastHit hit;
            Vector3 worldClickPos;
            
            // 尝试获取点击的实际世界位置
            if (Physics.Raycast(ray, out hit)) {
                worldClickPos = hit.point;
                Debug.Log($"射线命中点位置: {worldClickPos}");
                
                // 使用世界位置显示面板
                ShowPanelAtWorldPosition(worldClickPos);
            }
            else {
                // 如果射线没有命中，使用屏幕位置显示
                ShowClearPanelAtMousePosition();
            }
            
            Debug.Log("已显示障碍物清除面板");
        }
        
        // 更新面板跟随逻辑
        if (clearPanel != null && clearPanel.activeSelf && panelFollower != null && panelFollower.targetObject != null)
        {
            // 面板跟随组件会自动在Update中处理位置更新
            // 可以在这里添加额外的逻辑，如果需要
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugClearPanelPosition();
        }
    }
    
    // 添加这个辅助方法检查是否点击在UI上
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    // 用于在编辑器中测试面板位置
    public void TestShowPanelAtPosition(Vector2 screenPosition)
    {
        clickPosition = screenPosition;
        ShowClearPanelAtMousePosition();
        Debug.Log($"测试面板位置: {screenPosition}");
    }
    
    // 直接设置面板位置（可从Inspector调用）
    public void SetPanelPosition(float x, float y, float z = 0)
    {
        if (clearPanel == null) return;
        
        RectTransform panelRect = clearPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // 获取面板所在的Canvas
            Canvas parentCanvas = clearPanel.GetComponentInParent<Canvas>();
            if (parentCanvas == null) {
                Debug.LogError("清除面板没有Canvas父对象!");
                return;
            }
            
            // 根据Canvas的渲染模式处理
            if (parentCanvas.renderMode == RenderMode.WorldSpace) {
                // 对于World Space Canvas，使用localPosition
                panelRect.localPosition = new Vector3(x, y, z);
                Debug.Log($"手动设置面板本地位置 (World Space): ({x}, {y}, {z})");
            }
            else {
                // 对于Screen Space Canvas，使用anchoredPosition
                panelRect.anchoredPosition = new Vector2(x, y);
                Debug.Log($"手动设置面板锚点位置 (Screen Space): ({x}, {y})");
            }
            
            // 显示面板
            clearPanel.SetActive(true);
            
            // 更新面板内容
            if (currentHoverGroup != null)
            {
                // 更新面板上的类型文本
                if (typeText != null)
                {
                    typeText.text = ExtractObstacleType(currentHoverGroup.groupName);
                }
                
                // 更新面板上的成本值
                if (costValueText != null)
                {
                    int cost = obstacleManager.GetClearCost(selectedPosition, obstacleManager.GetObstacleType(selectedPosition), currentHoverGroup);
                    costValueText.text = cost.ToString();
                }
                if (unableClearText != null)
                {
                    int cost = obstacleManager.GetClearCost(selectedPosition, obstacleManager.GetObstacleType(selectedPosition), currentHoverGroup);
                    if(CoinManager.Instance.CurrentCoins < cost)
                    {
                        unableClearText.text = "Coins insufficient!";
                    }
                    else
                    {
                        unableClearText.text = "Destroy？";
                    }
                }
                
                // 更新按钮状态
                UpdateClearPanelButtons();
            }
        }
    }
    
    // 在世界位置显示面板
    public void ShowPanelAtWorldPosition(Vector3 worldPosition)
    {
        if (clearPanel == null) return;
        
        // 显示面板
        clearPanel.SetActive(true);
        
        // 获取面板的RectTransform
        RectTransform panelRect = clearPanel.GetComponent<RectTransform>();
        
        if (panelRect != null)
        {
            // 获取Canvas
            Canvas parentCanvas = clearPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // 转换为Canvas的本地坐标
                Vector3 localPos = parentCanvas.transform.InverseTransformPoint(worldPosition);
                panelRect.localPosition = localPos;
                
                Debug.Log($"在世界位置显示面板: 世界={worldPosition}, 本地={localPos}");
            }
            else
            {
                // 如果不是World Space Canvas，则转换为屏幕位置
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
                Vector2 localPoint;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    screenPos,
                    parentCanvas.worldCamera,
                    out localPoint))
                {
                    panelRect.anchoredPosition = localPoint;
                }
            }
            
            // 更新面板内容
            UpdatePanelContent();
        }
    }
    
    // 更新面板内容
    private void UpdatePanelContent()
    {
        if (currentHoverGroup != null)
        {
            // 更新面板上的类型文本
            if (typeText != null)
            {
                typeText.text = ExtractObstacleType(currentHoverGroup.groupName);
            }
            
            // 更新面板上的成本值
            if (costValueText != null)
            {
                int cost = obstacleManager.GetClearCost(selectedPosition, obstacleManager.GetObstacleType(selectedPosition), currentHoverGroup);
                costValueText.text = cost.ToString();
            }
            
            // 更新按钮状态
            UpdateClearPanelButtons();
        }
    }
    
    // 获取屏幕点击位置对应的世界空间位置
    private Vector3 GetWorldPositionFromScreenPoint(Vector2 screenPoint)
    {
        // 使用摄像机将屏幕点转换为世界点
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        worldPoint.z = 0; // 确保Z坐标为0，这样在2D空间中可见
        return worldPoint;
    }
    
    // 在鼠标位置显示清除面板
    private void ShowClearPanelAtMousePosition()
    {
        if (clearPanel == null || currentHoverGroup == null) return;
        
        // 显示面板
        clearPanel.SetActive(true);
        
        // 使用面板跟随组件来定位面板
        if (panelFollower != null)
        {
            // 在鼠标点击位置创建一个临时目标物体
            // 使用射线从相机向鼠标点击位置发射，获取真实的世界位置
            Ray ray = Camera.main.ScreenPointToRay(clickPosition);
            RaycastHit hit;
            
            // 调试射线检测碰撞结果
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 3f); // 在场景视图中绘制射线，持续3秒
            bool didHit = Physics.Raycast(ray, out hit);
            Debug.Log($"射线检测结果: {(didHit ? "击中碰撞体" : "未击中任何碰撞体")}");
            
            if (didHit) {
                Debug.Log($"击中物体: {hit.collider.gameObject.name}, 类型: {hit.collider.GetType()}, 位置: {hit.point}");
            }
            
            GameObject targetObj;
            
            if (didHit) {
                // 如果射线击中了物体，在击中点创建目标
                targetObj = new GameObject("TempFollowTarget");
                targetObj.transform.position = hit.point;
            }
            else {
                // 如果没击中任何物体，在默认距离处创建目标
                targetObj = new GameObject("TempFollowTarget");
                targetObj.transform.position = ray.GetPoint(10f);
            }
            
            // 设置为面板跟随目标
            panelFollower.SetTarget(targetObj);
            
            Debug.Log($"面板跟随模式 - 设置跟随目标: {targetObj.transform.position}, 鼠标位置: {clickPosition}");
            
            // 面板跟随组件会自动处理位置
        }
        else
        {
            // 备用方法：如果没有面板跟随组件，使用直接定位
            RectTransform panelRect = clearPanel.GetComponent<RectTransform>();
            
            if (panelRect != null)
            {
                // 获取面板所在的Canvas
                Canvas parentCanvas = clearPanel.GetComponentInParent<Canvas>();
                if (parentCanvas == null) {
                    Debug.LogError("清除面板没有Canvas父对象!");
                    return;
                }
                
                // 根据Canvas的渲染模式处理
                if (parentCanvas.renderMode == RenderMode.WorldSpace) {
                    // 将屏幕坐标转换为Canvas上的坐标
                    Vector2 pointerPosition = clickPosition;
                    
                    // 创建一个射线，从摄像机射向鼠标位置
                    Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
                    
                    // 创建一个平面，这个平面与Canvas平面重合
                    Plane canvasPlane = new Plane(parentCanvas.transform.forward, parentCanvas.transform.position);
                    
                    float distance;
                    if (canvasPlane.Raycast(ray, out distance)) {
                        // 计算射线与Canvas平面的交点，这就是鼠标在Canvas空间中的位置
                        Vector3 worldPos = ray.GetPoint(distance);
                        
                        // 将世界坐标转换到Canvas的本地坐标系
                        Vector3 localPos = parentCanvas.transform.InverseTransformPoint(worldPos);
                        
                        // 只改变ClearPanel的位置，不改变Canvas位置
                        panelRect.localPosition = localPos;
                        
                        Debug.Log($"World Space Canvas - 设置面板本地位置: 鼠标位置={pointerPosition}, 世界交点={worldPos}, 本地位置={localPos}");
                    } else {
                        Debug.LogWarning("无法在Canvas平面上找到鼠标交点");
                    }
                }
                else {
                    // Screen Space Canvas处理方式
                    // 直接将鼠标屏幕坐标转换为Canvas的本地坐标
                    Vector2 localPoint;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentCanvas.GetComponent<RectTransform>(),
                        clickPosition,
                        parentCanvas.worldCamera,
                        out localPoint))
                    {
                        // 只设置ClearPanel的锚点位置，不改变Canvas位置
                        panelRect.anchoredPosition = localPoint;
                        Debug.Log($"Screen Space Canvas - 设置面板位置: {localPoint}, 点击位置: {clickPosition}");
                    } else {
                        Debug.LogWarning("无法将屏幕点转换为Canvas本地坐标");
                    }
                }
            }
        }
        
        // 更新面板上的类型文本
        if (typeText != null)
        {
            // 获取障碍物类型
            ObstacleType obstacleType = obstacleManager.GetObstacleType(selectedPosition);
            EnhancedObstacleManager.ObstacleTypeInfo typeInfo = obstacleManager.GetObstacleTypeInfo(obstacleType);
            
            // 如果有类型信息并且有显示名称，优先使用显示名称
            if (typeInfo != null && !string.IsNullOrEmpty(typeInfo.displayName))
            {
                typeText.text = typeInfo.displayName;
            }
            // 否则使用组名提取类型
            else if (currentHoverGroup != null)
            {
                typeText.text = ExtractObstacleType(currentHoverGroup.groupName);
            }
            // 最后使用枚举值
            else
            {
                // 将枚举值转换为更友好的名称
                switch(obstacleType)
                {
                    case ObstacleType.Forest: typeText.text = "Forest"; break;
                    case ObstacleType.House: typeText.text = "House"; break;
                    case ObstacleType.Field: typeText.text = "Field"; break;
                    case ObstacleType.Mountain: typeText.text = "Mountain"; break;
                    case ObstacleType.Building: typeText.text = "Building"; break;
                    case ObstacleType.Rubble: typeText.text = "Rubble"; break;
                    case ObstacleType.Trees: typeText.text = "Trees"; break;
                    case ObstacleType.Water: typeText.text = "Water"; break;
                    default: typeText.text = "Obstacle"; break;
                }
            }
            
            Debug.Log($"设置障碍物类型文本为: {typeText.text}，原始类型: {obstacleType}");
        }
        
        // 获取当前选中位置
        Vector3Int cellPos = selectedPosition;
        
        // 获取清除成本
        int cost = obstacleManager.GetClearCost(cellPos, obstacleManager.GetObstacleType(cellPos), currentHoverGroup);
        
        // 设置clearCost字段
        clearCost = cost;
        Debug.LogWarning($"[关键修复] ShowClearPanelAtMousePosition 设置clearCost={cost}");
        
        // 更新面板上的成本值
        if (costValueText != null)
        {
            costValueText.text = cost.ToString();
        }
        
        // 更新按钮状态
        UpdateClearPanelButtons();
    }
    
    // 创建用于高亮显示的Tilemap
    private void CreateHighlightTilemap()
    {
        GameObject highlightObj = new GameObject("ObstacleHighlight");
        highlightObj.transform.SetParent(transform);
        
        Grid grid = FindObjectOfType<Grid>();
        if (grid != null)
        {
            highlightObj.transform.position = grid.transform.position;
            highlightTilemap = highlightObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = highlightObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 10; // 确保渲染在最上层
            
            // 加载高亮瓦片
            highlightTile = Resources.Load<TileBase>("Tiles/HighlightTile");
            if (highlightTile == null)
            {
                Debug.LogWarning("未找到高亮瓦片，将无法高亮显示障碍物组");
            }
        }
    }
    
    // 显示障碍物信息
    public void ShowObstacleInfo(Vector3Int position, EnhancedObstacleManager.ObstacleTypeInfo typeInfo, ObstacleGroup group, int cost)
    {
        if (obstaclePanel == null) return;
        
        selectedPosition = position;
        selectedTypeInfo = typeInfo;
        selectedGroup = group;
        clearCost = cost;
        
        // 显示面板
        obstaclePanel.SetActive(true);
        
        // 更新标题和信息
        if (titleText != null)
        {
            titleText.text = typeInfo != null ? typeInfo.displayName : "OBSTACLE";
        }
        
        if (infoText != null)
        {
            string info = $"位置: ({position.x}, {position.y})";
            infoText.text = info;
        }
        
        // 更新成本
        if (costText != null)
        {
            // 添加金币图标
            costText.text = $"清除成本: {cost}";
        }
        
        // 更新unableClearText
        if (unableClearText != null)
        {
            bool enoughGold = false;
            if (CoinManager.Instance != null)
                enoughGold = CoinManager.Instance.HasEnoughCoins(cost);
            else if (TowerManager.Instance != null)
                enoughGold = TowerManager.Instance.currentGold >= cost;
                
            unableClearText.text = enoughGold ? "Destroy?" : "Coins insufficient!";
        }
        
        // 更新图标
        if (typeIcon != null && typeInfo != null && typeInfo.icon != null)
        {
            typeIcon.sprite = typeInfo.icon;
            typeIcon.gameObject.SetActive(true);
        }
        else if (typeIcon != null)
        {
            typeIcon.gameObject.SetActive(false);
        }
        
        // 更新组信息
        UpdateGroupInfo(group);
        
        // 检查金币是否足够，并更新按钮状态
        UpdateClearButtonState();
    }
    
    // 提取组名称的主要类型（不包含数字）
    private string ExtractObstacleType(string groupName)
    {
        if (string.IsNullOrEmpty(groupName)) return "Obstacle";
        
        // 检查常见的障碍物类型前缀
        string[] prefixes = { "Forest", "House", "Field", "Mountain", "Building", "Rubble", "Trees", "Water" };
        
        foreach (var prefix in prefixes)
        {
            if (groupName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) || 
                groupName.ToLower().Contains(prefix.ToLower()))
            {
                // 确保返回首字母大写的类型名
                return char.ToUpper(prefix[0]) + prefix.Substring(1).ToLower();
            }
        }
        return groupName; // 如果不匹配任何已知类型，返回原始名称
    }
    
    // 更新组信息显示
    private void UpdateGroupInfo(ObstacleGroup group)
    {
        if (groupInfoPanel == null) return;
        
        if (group != null)
        {
            groupInfoPanel.SetActive(true);
            
            // 显示清除面板
            if (clearPanel != null)
            {
                clearPanel.SetActive(true);
                
                // 更新面板中的类型文本
                if (typeText != null)
                {
                    // 获取障碍物类型
                    ObstacleType obstacleType = obstacleManager.GetObstacleType(selectedPosition);
                    EnhancedObstacleManager.ObstacleTypeInfo typeInfo = obstacleManager.GetObstacleTypeInfo(obstacleType);
                    
                    // 如果有类型信息并且有显示名称，优先使用显示名称
                    if (typeInfo != null && !string.IsNullOrEmpty(typeInfo.displayName))
                    {
                        typeText.text = typeInfo.displayName;
                    }
                    // 否则使用组名提取类型
                    else
                    {
                        typeText.text = ExtractObstacleType(group.groupName);
                    }
                    
                    Debug.Log($"在UpdateGroupInfo中设置障碍物类型文本为: {typeText.text}");
                }
                
                // 更新面板中的成本值
                if (costValueText != null)
                {
                    costValueText.text = clearCost.ToString();
                }
                
                // 更新清除按钮状态
                UpdateClearPanelButtons();
            }
            
            if (groupNameText != null)
            {
                // 只显示障碍物类型，不显示数字
                groupNameText.text = ExtractObstacleType(group.groupName);
            }
            
            if (groupProgressText != null)
            {
                float progress = 0;
                if (group.positions.Count > 0)
                {
                    // 计算已清除的比例
                    int clearedCount = 0;
                    foreach (Vector3Int pos in group.positions)
                    {
                        if (obstacleManager.IsClearedObstacle(pos))
                        {
                            clearedCount++;
                        }
                    }
                    progress = (float)clearedCount / group.positions.Count;
                    
                    groupProgressText.text = $"进度: {clearedCount}/{group.positions.Count}";
                }
                
                // 更新进度条
                if (groupProgressBar != null)
                {
                    groupProgressBar.fillAmount = progress;
                }
            }
            
            // 切换按钮默认不选中
            if (showGroupToggle != null)
            {
                showGroupToggle.isOn = false;
            }
        }
        else
        {
            groupInfoPanel.SetActive(false);
        }
    }
    
    // 更新清除按钮状态
    private void UpdateClearButtonState()
    {
        if (clearButton == null) return;
        
        // 优先使用CoinManager检查金币是否足够
        bool enoughGold = false;
        
        // 如果存在CoinManager，使用它来检查金币
        if (CoinManager.Instance != null)
        {
            enoughGold = CoinManager.Instance.HasEnoughCoins(clearCost);
        }
        // 兼容旧版本：如果没有CoinManager，使用TowerManager
        else if (TowerManager.Instance != null)
        {
            enoughGold = TowerManager.Instance.currentGold >= clearCost;
        }
        Debug.Log("金币足够?: " + enoughGold);
        // 修改：不隐藏按钮，使用interactable属性显示禁用状态
        // 始终显示清除按钮，但根据金币是否足够设置交互状态
        clearButton.gameObject.SetActive(true);
        clearButton.interactable = enoughGold;
        
        // 更新按钮的提示信息
        UpdateButtonTooltip(clearButton, enoughGold);
        
        // 如果有专门的"不可用"按钮，则不再使用它
        if (insufficientButton != null)
        {
            insufficientButton.gameObject.SetActive(false);
        }
        
        // 同时更新清除面板按钮
        UpdateClearPanelButtons();
    }
    
    // 更新清除面板上的按钮
    private void UpdateClearPanelButtons()
    {
        // 优先使用CoinManager检查金币是否足够
        bool enoughGold = false;
        
        // 如果存在CoinManager，使用它来检查金币
        if (CoinManager.Instance != null)
        {
            Debug.Log($"[EnhancedObstacleUI] 检查是否有足够金币，clearCost={clearCost}，当前金币={CoinManager.Instance.CurrentCoins}");
            // 使用HasEnoughCoins而非TrySpendCoins，避免实际扣除金币
            enoughGold = CoinManager.Instance.HasEnoughCoins(clearCost);
            Debug.Log($"[EnhancedObstacleUI] 金币是否足够={enoughGold}，clearCost={clearCost}");
            
            // 更新unableClearText文本显示
            if (unableClearText != null)
            {
                unableClearText.text = enoughGold ? "Destroy?" : "Coins insufficient!";
            }
        }
        // 兼容旧版本：使用TowerManager
        else if (TowerManager.Instance != null)
        {
            enoughGold = TowerManager.Instance.currentGold >= clearCost;
            Debug.Log($"[EnhancedObstacleUI] 通过TowerManager检查金币: {TowerManager.Instance.currentGold}/{clearCost}, 足够: {enoughGold}");
            
            // 更新unableClearText文本显示
            if (unableClearText != null)
            {
                unableClearText.text = enoughGold ? "Destroy?" : "Coins insufficient!";
            }
        }
        
        // 修改：不隐藏按钮，而是使用interactable属性来显示禁用状态
        if (regularClearBtn != null)
        {
            // 始终显示按钮
            regularClearBtn.SetActive(true);
            
            // 根据金币是否足够设置按钮的交互状态
            Button regularBtn = regularClearBtn.GetComponent<Button>();
            if (regularBtn != null)
            {
                regularBtn.interactable = enoughGold;
                
                // 更新按钮的提示信息
                UpdateButtonTooltip(regularBtn, enoughGold);
                
                Debug.Log($"清除按钮状态：激活={regularClearBtn.activeSelf}, 可交互={regularBtn.interactable}");
            }
            else
            {
                Debug.LogError("regularClearBtn上没有Button组件");
            }
        }
        
        // 如果还有专门的"不可用"按钮，则不再使用它
        if (unableClearBtn != null)
        {
            unableClearBtn.SetActive(false);
        }
    }
    
    // 更新按钮的提示信息
    private void UpdateButtonTooltip(Button button, bool enoughGold)
    {
        if (button == null) return;
        
        // 先尝试获取现有的提示组件
        ButtonTooltip tooltip = button.GetComponent<ButtonTooltip>();
        
        // 如果没有提示组件且金币不足，添加一个
        if (tooltip == null && !enoughGold)
        {
            tooltip = button.gameObject.AddComponent<ButtonTooltip>();
        }
        
        // 如果现在有提示组件（新添加的或已有的）
        if (tooltip != null)
        {
            if (!enoughGold)
            {
                // 设置金币不足的提示信息
                int currentCoins = 0;
                if (CoinManager.Instance != null)
                {
                    currentCoins = CoinManager.Instance.CurrentCoins;
                }
                else if (TowerManager.Instance != null)
                {
                    currentCoins = TowerManager.Instance.currentGold;
                }
                
                tooltip.tooltipText = $"金币不足! 需要 {clearCost} 金币，当前只有 {currentCoins} 金币。";
                tooltip.enabled = true;
            }
            else
            {
                // 金币充足时禁用提示
                tooltip.enabled = false;
            }
        }
    }
    
    // 按钮提示组件（需要添加到工程中）
    [System.Serializable]
    public class ButtonTooltip : MonoBehaviour
    {
        public string tooltipText = "";
        public GameObject tooltipPrefab;
        private GameObject tooltipInstance;
        
        // 如果没有提供Prefab，将创建一个简单的文本提示
        private void OnEnable()
        {
            // 为按钮添加事件触发器
            UnityEngine.EventSystems.EventTrigger trigger = GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // 清除旧事件
            trigger.triggers.Clear();
            
            // 添加指针进入事件
            UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { ShowTooltip(); });
            trigger.triggers.Add(entryEnter);
            
            // 添加指针离开事件
            UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { HideTooltip(); });
            trigger.triggers.Add(entryExit);
        }
        
        // 显示提示
        private void ShowTooltip()
        {
            if (string.IsNullOrEmpty(tooltipText)) return;
            
            if (tooltipPrefab != null)
            {
                // 使用预设的提示UI
                tooltipInstance = Instantiate(tooltipPrefab, transform);
                
                // 尝试设置文本
                TMP_Text tooltipTextComp = tooltipInstance.GetComponentInChildren<TMP_Text>();
                if (tooltipTextComp != null)
                {
                    tooltipTextComp.text = tooltipText;
                }
                else
                {
                    // 回退到传统Text
                    Text oldText = tooltipInstance.GetComponentInChildren<Text>();
                    if (oldText != null)
                    {
                        oldText.text = tooltipText;
                    }
                }
            }
            else
            {
                // 创建简单的文本提示
                CreateSimpleTooltip();
            }
        }
        
        // 创建简单的文本提示
        private void CreateSimpleTooltip()
        {
            // 如果已经有实例，先销毁
            if (tooltipInstance != null)
            {
                Destroy(tooltipInstance);
            }
            
            // 创建一个简单的UI对象
            GameObject tooltipObj = new GameObject("SimpleTooltip");
            tooltipInstance = tooltipObj;
            
            // 设置为当前按钮的子对象并定位
            tooltipInstance.transform.SetParent(transform);
            
            // 添加UI组件
            RectTransform rect = tooltipObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 50); // 位于按钮上方
            rect.sizeDelta = new Vector2(200, 50);
            
            // 添加背景图片
            Image bg = tooltipObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            // 添加文本对象
            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipObj.transform);
            
            // 设置文本
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tooltipText;
            text.color = Color.white;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            
            // 设置文本区域
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            // 确保提示显示在最上层
            Canvas canvas = tooltipObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            tooltipObj.AddComponent<CanvasRenderer>();
        }
        
        // 隐藏提示
        private void HideTooltip()
        {
            if (tooltipInstance != null)
            {
                Destroy(tooltipInstance);
                tooltipInstance = null;
            }
        }
        
        private void OnDisable()
        {
            // 确保组件禁用时提示也被销毁
            HideTooltip();
        }
        
        private void OnDestroy()
        {
            // 确保组件销毁时提示也被销毁
            HideTooltip();
        }
    }
    
    // 清除按钮点击事件
    public void OnClearButtonClicked()
    {
        if (obstacleManager != null)
        {
            bool success = obstacleManager.ClearObstacle();
            if (success)
            {
                // 清除成功，面板会自动关闭
                ClearGroupHighlight();
                
                // 重置箭头状态
                ResetArrowFixedState();
            }
            else
            {
                // 清除失败，更新按钮状态
                UpdateClearButtonState();
            }
        }
    }
    
    // 面板上的清除按钮点击事件
    public void OnPanelClearButtonClicked()
    {
        Debug.Log("OnPanelClearButtonClicked 被调用");
        
        // 阻止事件继续传播
        UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.Use(); // 标记事件已使用
        
        if (obstacleManager == null)
        {
            Debug.LogError("obstacleManager为空，无法执行清除操作");
            return;
        }
        
        // 重要调试：检查clearCost是否正确
        Debug.LogWarning($"[关键检查] 清除按钮点击时，clearCost值为 {clearCost}");
        
        // 检查金币是否足够
        bool canClear = false;
        
        // 优先使用CoinManager
        if (CoinManager.Instance != null)
        {
            bool enoughGold = CoinManager.Instance.HasEnoughCoins(clearCost);
            // 更新按钮状态文本
            if (unableClearText != null)
            {
                unableClearText.text = enoughGold ? "Destroy?" : "Coins insufficient!";
            }
            
            Debug.Log($"[EnhancedObstacleUI] 开始清除障碍物，clearCost={clearCost}，当前金币={CoinManager.Instance.CurrentCoins}");
            
            // 检查clearCost是否为0
            if (clearCost <= 0)
            {
                Debug.LogError($"[严重错误] clearCost值为 {clearCost}，可能导致金币错误扣除！");
                
                // 尝试刷新当前选中障碍物的清除成本
                if (selectedPosition != null && selectedGroup != null)
                {
                    int cost = obstacleManager.GetClearCost(selectedPosition, obstacleManager.GetObstacleType(selectedPosition), selectedGroup);
                    Debug.LogWarning($"[尝试修复] 重新获取的clearCost={cost}");
                    
                    // 更新clearCost
                    clearCost = cost;
                }
            }
            
            canClear = CoinManager.Instance.TrySpendCoins(clearCost);
            Debug.Log($"[EnhancedObstacleUI] 清除结果={canClear}，clearCost={clearCost}，当前金币={CoinManager.Instance.CurrentCoins}");
            if (canClear)
            {
                // 尝试清除障碍物
                obstacleManager.ClearObstacle(selectedPosition, selectedGroup);
                
                // 清除成功，隐藏面板
                if (clearPanel != null)
                {
                    clearPanel.SetActive(false);
                }
                
                // 清除高亮
                ClearGroupHighlight();
                
                // 重置箭头状态
                ResetArrowFixedState();
                
                Debug.Log($"[EnhancedObstacleUI] 使用CoinManager成功清除障碍物，花费金币: {clearCost}");
            }
            else
            {
                Debug.Log($"[EnhancedObstacleUI] 金币不足，无法清除障碍物，需要: {clearCost}, 当前: {CoinManager.Instance.CurrentCoins}");
                // 更新面板按钮状态
                UpdateClearPanelButtons();
            }
        }
        // 兼容旧版本：使用原有逻辑
        else
        {
            bool success = obstacleManager.ClearObstacle();
            if (success)
            {
                // 清除成功，隐藏面板
                if (clearPanel != null)
                {
                    clearPanel.SetActive(false);
                }
                
                // 清除高亮
                ClearGroupHighlight();
                
                // 重置箭头状态
                ResetArrowFixedState();
                
                Debug.Log("通过面板成功清除障碍物");
            }
            else
            {
                // 清除失败，更新面板按钮状态
                UpdateClearPanelButtons();
            }
        }
    }
    
    // 显示/隐藏组中的所有障碍物
    public void OnShowGroupToggled(bool show)
    {
        if (selectedGroup == null || highlightTilemap == null || highlightTile == null) return;
        
        // 清除现有的高亮
        ClearGroupHighlight();
        
        // 如果开启，则高亮显示所有同组障碍物
        if (show && selectedGroup.positions != null)
        {
            foreach (Vector3Int pos in selectedGroup.positions)
            {
                // 不高亮当前选中的障碍物
                if (pos != selectedPosition)
                {
                    highlightTilemap.SetTile(pos, highlightTile);
                }
            }
        }
    }
    
    // 清除所有高亮显示
    private void ClearGroupHighlight()
    {
        if (highlightTilemap != null)
        {
            highlightTilemap.ClearAllTiles();
        }
    }
    
    // 检测鼠标悬停
    private void CheckMouseHover()
    {
        
        if (obstacleManager == null || obstacleManager.obstacleTilemaps == null) return;
        
        // 检查是否悬停在UI上，如果是则不检测障碍物
        if (IsPointerOverUI())
        {
            return;
        }
        
        // 确保主相机存在
        if (Camera.main == null)
        {
            Debug.LogError("找不到主相机，无法进行坐标转换!");
            return;
        }
        
        // 获取鼠标位置并转换为世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 确保Z坐标为0，与2D平面对齐
        
        Debug.Log($"鼠标世界坐标: {mouseWorldPos}, 屏幕坐标: {Input.mousePosition}");
        
        // 查找有效的Tilemap用于坐标转换
        Tilemap validTilemap = null;
        foreach (var tilemap in obstacleManager.obstacleTilemaps)
        {
            if (tilemap != null)
            {
                validTilemap = tilemap;
                break;
            }
        }
        
        if (validTilemap == null)
        {
            Debug.LogError("找不到有效的Tilemap用于坐标转换!");
            return;
        }
        
        // 将世界坐标转换为单元格坐标
        Vector3Int cellPos = validTilemap.WorldToCell(mouseWorldPos);
        Debug.Log($"使用Tilemap {validTilemap.name} 转换后的单元格坐标: {cellPos}");
        
        // 检查该位置是否有瓦片
        TileBase tile = validTilemap.GetTile(cellPos);
        Debug.Log($"该位置瓦片: {(tile != null ? tile.name : "无")}");
        
        // 检查该位置是否在任何障碍物组中
        ObstacleGroup matchedGroup = null;
        if (obstacleManager.obstacleGroups != null)
        {
            foreach (var group in obstacleManager.obstacleGroups)
            {
                if (group != null && group.positions != null && group.ContainsPosition(cellPos))
                {
                    matchedGroup = group;
                    Debug.Log($"找到匹配的组: {group.groupName}，包含位置 {cellPos}");
                    break;
                }
            }
        }
        
        // 检查是否是障碍物且未被清除
        bool isObstacle = obstacleManager.IsObstacle(cellPos);
        bool isCleared = obstacleManager.IsClearedObstacle(cellPos);
        
        Debug.Log($"IsObstacle结果: {isObstacle}, IsClearedObstacle结果: {isCleared}");
        
        // 如果有匹配的组但IsObstacle返回false，检查原因
        if (matchedGroup != null && !isObstacle)
        {
            Debug.LogWarning($"发现不一致! 位置 {cellPos} 在组 {matchedGroup.groupName} 中, 但IsObstacle返回false");
            Debug.Log("检查IsObstacle方法实现...");
            
            // 手动检查IsObstacle的条件
            ManualCheckPosition(cellPos);
        }
        
        // 检查是否悬停在障碍物上且未被清除
        if (isObstacle && !isCleared)
        {
            Debug.Log("找到有效障碍物!");
            
            // 获取障碍物所在组
            ObstacleGroup group = obstacleManager.FindObstacleGroup(cellPos);
            
            if (group != null && group != currentHoverGroup)
            {
                currentHoverGroup = group;
                ShowArrowIndicator(group);
            }
            
            return;
        }
        
        // 如果鼠标不在任何障碍物上，且箭头未固定，则隐藏箭头
        if (currentHoverGroup != null && !arrowFixed)
        {
            currentHoverGroup = null;
            HideArrowIndicator();
        }
    }
    
    // 手动检查指定位置是否是障碍物
    private void ManualCheckPosition(Vector3Int pos)
    {
        Debug.Log($"=== 手动检查位置 {pos} ===");
        
        // 检查所有Tilemap
        if (obstacleManager.obstacleTilemaps != null)
        {
            Debug.Log($"检查 {obstacleManager.obstacleTilemaps.Length} 个Tilemap图层");
            foreach (var tilemap in obstacleManager.obstacleTilemaps)
            {
                if (tilemap == null) continue;
                
                TileBase tile = tilemap.GetTile(pos);
                Debug.Log($"图层 {tilemap.name} 在位置 {pos} 的瓦片: {(tile != null ? tile.name : "无")}");
            }
        }
        
        // 检查是否在任何障碍物组中
        bool inAnyGroup = false;
        if (obstacleManager.obstacleGroups != null)
        {
            foreach (var group in obstacleManager.obstacleGroups)
            {
                if (group != null && group.positions != null && group.ContainsPosition(pos))
                {
                    inAnyGroup = true;
                    Debug.Log($"位置 {pos} 属于组 {group.groupName}");
                }
            }
        }
        
        if (!inAnyGroup)
        {
            Debug.Log($"位置 {pos} 不属于任何障碍物组");
        }
        
        // 检查IsObstacle方法结果
        bool isObstacle = obstacleManager.IsObstacle(pos);
        bool isCleared = obstacleManager.IsClearedObstacle(pos);
        Debug.Log($"IsObstacle({pos}) = {isObstacle}, IsClearedObstacle({pos}) = {isCleared}");
        Debug.Log("=== 手动检查结束 ===");
    }
    
    // 显示箭头指示器
    private void ShowArrowIndicator(ObstacleGroup group)
    {
        if (group == null || group.positions.Count == 0) return;
        
        // 计算组的中心位置
        Vector3 center = Vector3.zero;
        foreach (Vector3Int pos in group.positions)
        {
            // 获取世界坐标
            Vector3 worldPos = obstacleManager.obstacleTilemaps[0].GetCellCenterWorld(pos);
            center += worldPos;
        }
        center /= group.positions.Count;
        
        // 根据设置选择使用世界空间箭头或UI箭头
        if (useCanvasUI)
        {
            ShowUIArrow(center);
        }
        else if (arrowIndicator != null)
        {
            ShowWorldSpaceArrow(center);
        }
        else
        {
            Debug.LogError("没有可用的箭头指示器!");
        }
        
        Debug.Log($"显示箭头: 组名: {group.groupName}, 位置: {center}");
    }

    // 显示世界空间箭头
    private void ShowWorldSpaceArrow(Vector3 position)
    {
        // 设置箭头位置在组的上方
        Vector3 arrowPos = position;
        arrowPos.y += arrowHoverHeight;
        arrowPos.x += arrowHorizontalOffset;
        
        // 调整Z坐标确保在相机视野内
        arrowPos.z = 0;
        
        // 应用位置
        arrowIndicator.transform.position = arrowPos;
        
        // 确保箭头始终朝向前方
        arrowIndicator.transform.rotation = Quaternion.identity;
        
        // 显示箭头并添加调试信息
        arrowIndicator.SetActive(true);
        
        // 添加调试信息
        Debug.Log($"世界空间箭头显示在位置: {arrowPos}, 箭头激活状态: {arrowIndicator.activeInHierarchy}");
        
        // 确保箭头渲染组件可见
        SpriteRenderer arrowRenderer = arrowIndicator.GetComponent<SpriteRenderer>();
        if (arrowRenderer != null)
        {
            // 确保有适当的Sprite
            if (arrowRenderer.sprite == null)
            {
                Debug.LogError("箭头没有设置Sprite!");
            }
            
            // 设置更高的渲染顺序
            arrowRenderer.sortingOrder = 100;
            
            // 确保颜色有不透明度
            Color color = arrowRenderer.color;
            color.a = 1.0f;
            arrowRenderer.color = color;
        }
        else
        {
            Debug.LogError("箭头没有SpriteRenderer组件!");
        }
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
        if (Camera.main != null)
        {
            // 调整Y轴位置和X轴位置
            worldPosition.y += arrowHoverHeight;
            worldPosition.x += arrowHorizontalOffset;
            
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.GetComponent<RectTransform>(), 
                screenPoint, 
                uiCanvas.worldCamera, 
                out localPoint);
            
            // 设置UI箭头位置
            arrowUIInstance.anchoredPosition = localPoint;
            
            Debug.Log($"UI箭头显示在屏幕位置: {screenPoint}, Canvas本地位置: {localPoint}");
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
        
        // 同时隐藏清除面板
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }
    
    // 固定箭头到障碍物组
    public void FixArrowToGroup()
    {
        if (currentHoverGroup == null) return;
        
        // 记录位置与组
        // 使用组中的第一个位置
        if (currentHoverGroup.positions != null && currentHoverGroup.positions.Count > 0)
        {
            selectedPosition = currentHoverGroup.positions[0];
        }
        else
        {
            // 如果组中没有位置，使用当前选中位置
            Debug.LogWarning("障碍物组中没有位置信息!");
        }
        
        selectedGroup = currentHoverGroup;
        
        // 固定箭头
        arrowFixed = true;
        
        // 更新组信息面板
        UpdateGroupInfo(currentHoverGroup);
        
        // 如果有面板跟随组件，设置跟随目标
        if (panelFollower != null && currentHoverGroup != null && currentHoverGroup.positions.Count > 0)
        {
            // 计算组的中心位置
            Vector3 centerPos = Vector3.zero;
            int validPositions = 0;
            
            // 如果组中有多个位置，计算它们的中心
            foreach (Vector3Int cellPos in currentHoverGroup.positions)
            {
                if (obstacleManager != null && obstacleManager.obstacleTilemaps != null && 
                    obstacleManager.obstacleTilemaps.Length > 0 && obstacleManager.obstacleTilemaps[0] != null)
                {
                    Vector3 worldPos = obstacleManager.obstacleTilemaps[0].GetCellCenterWorld(cellPos);
                    centerPos += worldPos;
                    validPositions++;
                }
            }
            
            if (validPositions > 0)
            {
                centerPos /= validPositions;
            }
            else
            {
                // 如果无法计算中心，使用第一个位置
                Vector3Int firstCellPos = currentHoverGroup.positions[0];
                if (obstacleManager != null && obstacleManager.obstacleTilemaps != null && 
                    obstacleManager.obstacleTilemaps.Length > 0 && obstacleManager.obstacleTilemaps[0] != null)
                {
                    centerPos = obstacleManager.obstacleTilemaps[0].GetCellCenterWorld(firstCellPos);
                }
            }
            
            // 创建一个空物体作为跟随目标
            GameObject targetObj = new GameObject("PanelTarget");
            targetObj.transform.position = centerPos;
            
            // 设置为面板跟随目标
            panelFollower.SetTarget(targetObj);
            
            // 使用新的Canvas跟随模式
            panelFollower.moveEntireCanvas = true;  // 移动整个Canvas，而不仅是面板
            panelFollower.offset = new Vector3(200f, 2f, 0);  // 设置适当的偏移，X轴偏移200，使Canvas位于障碍物组右上方
            
            // 如果不移动整个Canvas，使用屏幕空间偏移
            if (!panelFollower.moveEntireCanvas)
            {
                panelFollower.useScreenOffset = true;
                panelFollower.screenOffsetX = 200f;  // 屏幕空间X轴也设置为200
                panelFollower.screenOffsetY = 100f;
                panelFollower.keepOnScreen = true;
            }
            
            Debug.Log($"面板跟随目标设置为组中心点: {centerPos}, 组包含 {validPositions} 个位置, 移动整个Canvas: {panelFollower.moveEntireCanvas}");
        }
        
        Debug.Log($"箭头已固定到组: {currentHoverGroup.groupName}");
    }
    
    // 重置箭头固定状态
    public void ResetArrowFixedState()
    {
        arrowFixed = false;
        selectedGroup = null;
        
        // 恢复箭头正常悬停显示
        HideArrowIndicator();
        
        // 隐藏组信息面板
        if (groupInfoPanel != null)
        {
            groupInfoPanel.SetActive(false);
        }
        
        // 隐藏清除面板
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
        
        // 清除面板跟随目标
        if (panelFollower != null)
        {
            if (panelFollower.targetObject != null)
            {
                Destroy(panelFollower.targetObject); // 销毁临时目标物体
            }
            panelFollower.SetTarget(null);
        }
        
        Debug.Log("已重置箭头固定状态");
    }
    
    public void HidePanel()
    {
        if (obstaclePanel != null)
        {
            obstaclePanel.SetActive(false);
        }
        
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
        
        // 清理面板跟随目标
        if (panelFollower != null && panelFollower.targetObject != null)
        {
            Destroy(panelFollower.targetObject);
            panelFollower.SetTarget(null);
        }
        
        // 清除高亮
        ClearGroupHighlight();
        
        // 重置箭头状态
        ResetArrowFixedState();
    }
    
    // 取消按钮点击处理
    public void OnCancelButtonClicked()
    {
        Debug.Log("OnCancelButtonClicked 被调用");
        
        // 阻止事件继续传播
        UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.Use(); // 标记事件已使用
        
        // 重置面板
        ResetArrowFixedState();
        
        // 隐藏清除面板
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
        
        // 清理面板跟随目标
        if (panelFollower != null && panelFollower.targetObject != null)
        {
            Destroy(panelFollower.targetObject);
            panelFollower.SetTarget(null);
        }
        
        // 清除高亮显示
        ClearGroupHighlight();
        
        // 通知障碍物管理器取消选择
        if (obstacleManager != null)
        {
            obstacleManager.DeselectObstacle();
        }
        
        Debug.Log("取消操作，已重置并隐藏面板");
    }
    
    void OnDestroy()
    {
        // 清除事件监听
        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(OnClearButtonClicked);
        }
        
        if (showGroupToggle != null)
        {
            showGroupToggle.onValueChanged.RemoveListener(OnShowGroupToggled);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        }
    }

    public void DebugClearPanelPosition()
    {
        // 创建一个测试点击
        clickPosition = Input.mousePosition;
        Debug.Log($"测试点击位置: {clickPosition}");
        
        // 显示面板并输出详细日志
        ShowClearPanelAtMousePosition();
        
        // 输出面板最终位置
        if (clearPanel != null)
        {
            Debug.Log($"面板最终位置: {clearPanel.transform.position}, 本地位置: {clearPanel.transform.localPosition}");
        }
    }

    // 设置按钮的禁用视觉效果
    private void SetupButtonDisabledState()
    {
        // 设置主界面清除按钮的禁用样式
        if (clearButton != null)
        {
            // 获取按钮的颜色块
            ColorBlock colors = clearButton.colors;
            // 确保禁用状态的颜色有足够的透明度和灰度
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            clearButton.colors = colors;
            
            // 如果按钮有文本组件，设置禁用时的文本颜色
            TMP_Text buttonText = clearButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                // 可以添加一个禁用时的文本颜色变化效果
                // 这需要自定义实现
            }
        }
        
        // 设置面板中清除按钮的禁用样式
        if (regularClearBtn != null)
        {
            Button regularBtn = regularClearBtn.GetComponent<Button>();
            if (regularBtn != null)
            {
                ColorBlock colors = regularBtn.colors;
                colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                regularBtn.colors = colors;
                
                // 如果有文本组件，也可以设置禁用状态的文本样式
                TMP_Text buttonText = regularBtn.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    // 可以添加禁用时的文本效果
                }
            }
        }
    }
} 