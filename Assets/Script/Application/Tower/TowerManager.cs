using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;

public enum TowerType
{
    Cannon,
    Arrow,
    Laser
}

public class TowerManager : Singleton<TowerManager>
{
    [Header("塔预制体")]
    public GameObject cannonTowerPrefab;
    public GameObject arrowTowerPrefab;
    public GameObject laserTowerPrefab;
    
    [Header("放置设置")]
    public Tilemap placementTilemap; // 用于确定可放置区域
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;
    
    [Header("资源")]
    public int currentGold = 300; // 起始金币
    
    [Header("UI引用")]
    public Text goldText;
    public GameObject buildPanel;
    public GameObject upgradePanel;
    public Text towerInfoText;
    public Button upgradeButton;
    public Button sellButton;
    public Text notificationText; // 通知文本
    private float notificationDuration = 3f; // 通知显示时间

    [Header("范围可视化")]
    public bool showRangeOnSelect = true;
    public GameObject rangeIndicatorPrefab;
    
    // 记录已建造的塔
    private Dictionary<Vector3Int, BaseTower> builtTowers = new Dictionary<Vector3Int, BaseTower>();
    
    // 当前选中的塔类型
    private TowerType selectedTowerType = TowerType.Cannon;
    
    // 当前选中的塔(用于升级或出售)
    private BaseTower selectedTower;
    
    // 是否使用预设放置点
    public bool usePresetPlacementPoints = false;
    
    // 当前选中的放置点
    private TowerPlacementPoint selectedPlacementPoint;
    
    // 塔的预览
    private GameObject towerPreview;
    private SpriteRenderer previewRenderer;
    
    // 范围指示器
    private GameObject currentRangeIndicator;
    
    void Start()
    {
        // 初始化塔预制体
        InitializeTowerPrefabs();
        
        // 初始化预览
        CreateTowerPreview();
        
        // 初始化UI
        UpdateGoldDisplay();
        ShowBuildPanel();
        
        // 如果使用预设放置点，高亮可用的放置点
        if (usePresetPlacementPoints)
        {
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null && placementManager.highlightAvailablePoints)
            {
                placementManager.HighlightAvailablePoints(true);
            }
        }
    }
    
    void Update()
    {
        // 更新预览位置
        UpdateTowerPreview();
        
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
        
        // 取消选择
        if (Input.GetMouseButtonDown(1) && (selectedTower != null || selectedPlacementPoint != null))
        {
            DeselectTower();
            DeselectPlacementPoint();
        }
    }
    
    // 初始化塔预制体，确保它们存在
    void InitializeTowerPrefabs()
    {
        Debug.Log("TowerManager: 初始化塔预制体");
        
        // 尝试从Resources加载预制体
        if (cannonTowerPrefab == null)
            cannonTowerPrefab = Resources.Load<GameObject>("tower/CannonTower");
        
        if (arrowTowerPrefab == null)
            arrowTowerPrefab = Resources.Load<GameObject>("tower/ArrowTower");
        
        if (laserTowerPrefab == null)
            laserTowerPrefab = Resources.Load<GameObject>("tower/LaserTower");
        
        // 检查是否成功加载
        bool allPrefabsLoaded = cannonTowerPrefab != null && arrowTowerPrefab != null && laserTowerPrefab != null;
        
        if (!allPrefabsLoaded)
        {
            Debug.LogWarning("预制体加载失败，将使用运行时构建的预制体");
            
            // 检查场景中是否有SimpleTowerBuilder
            SimpleTowerBuilder builder = FindObjectOfType<SimpleTowerBuilder>();
            if (builder == null)
            {
                Debug.Log("创建SimpleTowerBuilder");
                GameObject builderObj = new GameObject("SimpleTowerBuilder");
                builder = builderObj.AddComponent<SimpleTowerBuilder>();
                builder.saveToResources = true;
            }
            
            // 等待一帧，让SimpleTowerBuilder初始化
            Invoke("LoadBuiltPrefabs", 0.2f);
        }
    }
    
    // 从Builder创建的预制体中加载
    void LoadBuiltPrefabs()
    {
        if (cannonTowerPrefab == null)
            cannonTowerPrefab = Resources.Load<GameObject>("tower/CannonTower");
        
        if (arrowTowerPrefab == null)
            arrowTowerPrefab = Resources.Load<GameObject>("tower/ArrowTower");
        
        if (laserTowerPrefab == null)
            laserTowerPrefab = Resources.Load<GameObject>("tower/LaserTower");
        
        if (cannonTowerPrefab == null || arrowTowerPrefab == null || laserTowerPrefab == null)
        {
            Debug.LogError("无法加载塔预制体，塔防系统将无法正常工作！");
        }
        else
        {
            Debug.Log("塔预制体加载完成！");
        }
    }
    
    void CreateTowerPreview()
    {
        towerPreview = new GameObject("TowerPreview");
        previewRenderer = towerPreview.AddComponent<SpriteRenderer>();
        
        // 根据当前选择的塔类型设置预览精灵
        UpdatePreviewSprite();
        
        // 设置半透明
        Color color = previewRenderer.color;
        color.a = 0.5f;
        previewRenderer.color = color;
    }
    
    void UpdatePreviewSprite()
    {
        if (previewRenderer != null)
        {
            GameObject prefab = GetTowerPrefab(selectedTowerType);
            if (prefab != null)
            {
                SpriteRenderer towerRenderer = prefab.GetComponent<SpriteRenderer>();
                if (towerRenderer != null)
                {
                    previewRenderer.sprite = towerRenderer.sprite;
                }
            }
        }
    }
    
    void UpdateTowerPreview()
    {
        // 如果有选中的放置点，预览固定在该点上
        if (usePresetPlacementPoints && selectedPlacementPoint != null)
        {
            towerPreview.transform.position = selectedPlacementPoint.transform.position;
            
            // 检查是否可以放置塔
            bool canPlace = CanPlaceTowerAtPoint(selectedPlacementPoint);
            Color color = canPlace ? validPlacementColor : invalidPlacementColor;
            color.a = 0.5f; // 半透明
            previewRenderer.color = color;
            return;
        }

        // 获取鼠标位置
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 将世界坐标转换为格子坐标
        Vector3Int cellPos = placementTilemap.WorldToCell(mouseWorldPos);
        
        // 将格子坐标转换回世界坐标（居中）
        Vector3 cellWorldPos = placementTilemap.GetCellCenterWorld(cellPos);
        
        // 更新预览位置
        towerPreview.transform.position = cellWorldPos;
        
        // 检查是否可以放置塔
        bool canPlaceTower = usePresetPlacementPoints ? 
            TowerPlacementManager.Instance.CanPlaceTowerAt(cellPos) : 
            CanPlaceTower(cellPos);
        
        // 更新预览颜色
        Color previewColor = canPlaceTower ? validPlacementColor : invalidPlacementColor;
        previewColor.a = 0.5f; // 半透明
        previewRenderer.color = previewColor;
    }
    
    void HandleMouseClick()
    {
        // 获取鼠标位置
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 将世界坐标转换为格子坐标
        Vector3Int cellPos = placementTilemap.WorldToCell(mouseWorldPos);
        
        // 如果使用预设放置点模式
        if (usePresetPlacementPoints)
        {
            HandlePresetPlacementClick(mouseWorldPos, cellPos);
            return;
        }
        
        // 默认基于网格的放置逻辑
        // 检查该位置是否已经有塔
        if (builtTowers.ContainsKey(cellPos))
        {
            // 已有塔，选中它
            SelectTower(builtTowers[cellPos]);
            return;
        }
        else if (selectedTower != null)
        {
            // 如果已经选中了一个塔，并且点击了空地，取消选择
            DeselectTower();
            return;
        }
        
        // 新建塔
        if (CanPlaceTower(cellPos))
        {
            BuildTower(cellPos);
        }
    }
    
    // 处理预设放置点的点击
    private void HandlePresetPlacementClick(Vector3 mouseWorldPos, Vector3Int cellPos)
    {
        TowerPlacementManager placementManager = TowerPlacementManager.Instance;
        if (placementManager == null) return;
        
        // 查找最近的放置点
        TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
        float clickDistance = Vector3.Distance(mouseWorldPos, nearestPoint != null ? nearestPoint.transform.position : Vector3.positiveInfinity);
        
        // 先检查是否点击了已有的塔
        if (builtTowers.ContainsKey(cellPos))
        {
            // 已有塔，选中它
            SelectTower(builtTowers[cellPos]);
            return;
        }
        
        // 如果点击了足够近的可用放置点
        if (nearestPoint != null && clickDistance < 1.0f)
        {
            // 选中该放置点
            SelectPlacementPoint(nearestPoint);
            return;
        }
        else if (selectedTower != null)
        {
            // 如果已经选中了一个塔，并且点击了空地，取消选择
            DeselectTower();
            return;
        }
        else if (selectedPlacementPoint != null)
        {
            // 如果已经选中了一个放置点，并且点击了其他地方，取消选择
            DeselectPlacementPoint();
            return;
        }
    }
    
    // 选择放置点
    public void SelectPlacementPoint(TowerPlacementPoint point)
    {
        if (selectedPlacementPoint != point)
        {
            // 取消之前的选择
            DeselectPlacementPoint();
            
            selectedPlacementPoint = point;
            
            // 高亮显示该放置点
            // TODO: 实现高亮效果
            
            // 显示建造面板
            ShowBuildPanel();
            
            // 更新预览位置到放置点
            if (towerPreview != null)
            {
                towerPreview.transform.position = point.transform.position;
                
                // 检查是否可以放置塔
                bool canPlace = CanPlaceTowerAtPoint(point);
                Color color = canPlace ? validPlacementColor : invalidPlacementColor;
                color.a = 0.5f; // 半透明
                previewRenderer.color = color;
            }
        }
    }
    
    // 取消选择放置点
    public void DeselectPlacementPoint()
    {
        if (selectedPlacementPoint != null)
        {
            // 取消高亮效果
            // TODO: 移除高亮效果
            
            selectedPlacementPoint = null;
        }
    }
    
    // 检查是否可以在放置点上放置塔
    private bool CanPlaceTowerAtPoint(TowerPlacementPoint point)
    {
        if (point == null || point.isOccupied || !point.isEnabled)
        {
            return false;
        }
        
        // 特别处理ObstaclePlacementPoint
        if (point is ObstaclePlacementPoint)
        {
            ObstaclePlacementPoint obstaclePoint = (ObstaclePlacementPoint)point;
            ObstacleManager obstacleManager = ObstacleManager.Instance;
            
            // 确保障碍物已被清除
            if (obstacleManager != null && !obstacleManager.IsClearedObstacle(obstaclePoint.obstaclePosition))
            {
                return false;
            }
        }
        
        // 检查是否有足够的金币
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab != null)
        {
            BaseTower towerScript = prefab.GetComponent<BaseTower>();
            if (towerScript != null && currentGold < towerScript.buildCost)
            {
                return false; // 金币不足
            }
        }
        
        return true;
    }
    
    // 在选中的放置点上建造塔
    public void BuildTowerOnSelectedPoint()
    {
        if (selectedPlacementPoint == null || !CanPlaceTowerAtPoint(selectedPlacementPoint))
        {
            return;
        }
        
        // 获取当前选中塔的预制体
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab == null)
        {
            Debug.LogError($"塔预制体为null: {selectedTowerType}");
            return;
        }
        
        // 检查金币是否足够
        BaseTower towerScript = prefab.GetComponent<BaseTower>();
        if (towerScript != null && currentGold < towerScript.buildCost)
        {
            Debug.LogWarning("金币不足，无法建造塔！");
            return;
        }
        
        // 创建塔
        GameObject tower = Instantiate(prefab, selectedPlacementPoint.transform.position, Quaternion.identity);
        tower.SetActive(true); // 确保塔是激活的
        
        // 获取塔脚本
        BaseTower towerComponent = tower.GetComponent<BaseTower>();
        
        if (towerComponent != null)
        {
            // 扣除金币
            currentGold -= towerComponent.buildCost;
            UpdateGoldDisplay();
            
            // 记录已建造的塔 (使用格子坐标)
            Vector3Int gridPosition = selectedPlacementPoint.gridPosition;
            builtTowers[gridPosition] = towerComponent;
            
            // 更新放置点状态
            selectedPlacementPoint.OccupyPoint(towerComponent);
            
            Debug.Log($"建造{towerComponent.towerName}，花费{towerComponent.buildCost}金币，剩余{currentGold}金币");
            
            // 选中新建的塔
            SelectTower(towerComponent);
            DeselectPlacementPoint();
        }
    }
    
    void DeselectTower()
    {
        // 隐藏范围指示器
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
        
        // 清除选中状态
        selectedTower = null;
        
        // 显示建造面板
        ShowBuildPanel();
    }
    
    public bool CanPlaceTower(Vector3Int cellPos)
    {
        // 检查该位置是否已经有塔
        if (builtTowers.ContainsKey(cellPos))
        {
            return false;
        }
        
        // 检查是否是有效的放置位置（有对应的tile）
        if (!placementTilemap.HasTile(cellPos))
        {
            return false;
        }
        
        // 检查位置是否有障碍物且未清除
        ObstacleManager obstacleManager = ObstacleManager.Instance;
        if (obstacleManager != null && !obstacleManager.CanPlaceAtPosition(cellPos))
        {
            return false;  // 有障碍物，不能放置
        }
        
        // 检查是否有足够的金币
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab != null)
        {
            BaseTower towerScript = prefab.GetComponent<BaseTower>();
            if (towerScript != null)
            {
                int buildCost = towerScript.buildCost;
                bool hasEnoughGold = false;
                
                // 优先使用CoinManager检查金币
                if (CoinManager.Instance != null)
                {
                    hasEnoughGold = CoinManager.Instance.HasEnoughCoins(buildCost);
                }
                else
                {
                    hasEnoughGold = currentGold >= buildCost;
                }
                
                if (!hasEnoughGold)
                {
                    if (notificationText != null)
                    {
                        ShowNotification($"金币不足! 需要 {buildCost} 金币");
                    }
                return false; // 金币不足
                }
            }
        }
        
        return true;
    }
    
    void BuildTower(Vector3Int cellPos)
    {
        // 获取当前选中塔的预制体
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab == null)
        {
            Debug.LogError($"塔预制体为null: {selectedTowerType}");
            return;
        }
        
        // 检查金币是否足够
        BaseTower towerScript = prefab.GetComponent<BaseTower>();
        if (towerScript != null)
        {
            int buildCost = towerScript.buildCost;
            bool hasEnoughGold = false;
            
            // 优先使用CoinManager扣除金币
            if (CoinManager.Instance != null)
            {
                hasEnoughGold = CoinManager.Instance.TrySpendCoins(buildCost);
            }
            else
            {
                hasEnoughGold = currentGold >= buildCost;
                if (hasEnoughGold)
                {
                    currentGold -= buildCost;
                    UpdateGoldDisplay();
                }
            }
            
            if (!hasEnoughGold)
            {
                Debug.LogWarning($"金币不足，无法建造塔！需要 {buildCost} 金币");
                ShowNotification($"金币不足! 需要 {buildCost} 金币");
            return;
            }
        }
        
        // 将格子坐标转换回世界坐标（居中）
        Vector3 cellWorldPos = placementTilemap.GetCellCenterWorld(cellPos);
        
        // 创建塔
        GameObject tower = Instantiate(prefab, cellWorldPos, Quaternion.identity);
        tower.SetActive(true); // 确保塔是激活的
        
        // 获取塔脚本
        BaseTower towerComponent = tower.GetComponent<BaseTower>();
        
        if (towerComponent != null)
        {
            // 金币已在前面扣除，这里不需要再次扣除
            // 仅在未使用CoinManager时才需要显示更新
            if (CoinManager.Instance == null)
            {
                UpdateGoldDisplay();
            }
            
            // 记录已建造的塔
            builtTowers[cellPos] = towerComponent;
            
            Debug.Log($"建造{towerComponent.towerName}，花费{towerComponent.buildCost}金币，剩余{currentGold}金币");
            
            // 选中新建的塔
            SelectTower(towerComponent);
        }
    }
    
    void SelectTower(BaseTower tower)
    {
        // 取消之前的选择
        if (selectedTower != null && selectedTower != tower)
        {
            DeselectTower();
        }
        
        selectedTower = tower;
        Debug.Log($"已选中{tower.towerName}，等级：{tower.level}/{tower.maxLevel}");
        
        // 显示塔的范围
        if (showRangeOnSelect)
        {
            ShowTowerRange(tower);
        }
        
        // 显示升级面板
        ShowUpgradePanel();
        
        // 更新塔信息显示
        UpdateTowerInfo();
    }
    
    // 显示塔的攻击范围
    void ShowTowerRange(BaseTower tower)
    {
        // 清除现有的范围指示器
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
        }
        
        // 创建范围指示器
        if (rangeIndicatorPrefab != null)
        {
            currentRangeIndicator = Instantiate(rangeIndicatorPrefab, tower.transform.position, Quaternion.identity);
        }
        else
        {
            currentRangeIndicator = new GameObject("RangeIndicator");
            currentRangeIndicator.transform.position = tower.transform.position;
            
            SpriteRenderer rangeRenderer = currentRangeIndicator.AddComponent<SpriteRenderer>();
            rangeRenderer.sprite = Resources.Load<Sprite>("UI/Circle");
            if (rangeRenderer.sprite == null) // 如果无法加载精灵，创建一个简单的圆形
            {
                rangeRenderer.color = new Color(1f, 1f, 0f, 0.2f); // 半透明黄色
            }
            
            rangeRenderer.sortingOrder = -1; // 确保范围显示在塔下面
        }
        
        // 设置范围大小
        currentRangeIndicator.transform.localScale = new Vector3(tower.range * 2, tower.range * 2, 1);
    }
    
    // 升级选中的塔
    public void UpgradeSelectedTower()
    {
        if (selectedTower == null)
            return;
        
        int upgradeCost = selectedTower.GetUpgradeCost();
        
        bool hasEnoughGold = false;
            
        // 优先使用CoinManager扣除金币
        if (CoinManager.Instance != null)
        {
            hasEnoughGold = CoinManager.Instance.TrySpendCoins(upgradeCost);
        }
        else
        {
            hasEnoughGold = currentGold >= upgradeCost;
            if (hasEnoughGold)
        {
            currentGold -= upgradeCost;
                UpdateGoldDisplay();
            }
        }
            
        if (hasEnoughGold && selectedTower.level < selectedTower.maxLevel)
        {
            
            selectedTower.Upgrade();
            
            // 更新范围指示器大小
            if (currentRangeIndicator != null)
            {
                currentRangeIndicator.transform.localScale = new Vector3(selectedTower.range * 2, selectedTower.range * 2, 1);
            }
            
            // 更新UI信息
            UpdateTowerInfo();
            
            Debug.Log($"升级{selectedTower.towerName}到{selectedTower.level}级，花费{upgradeCost}金币，剩余{currentGold}金币");
        }
        else if (selectedTower.level >= selectedTower.maxLevel)
        {
            Debug.LogWarning($"{selectedTower.towerName}已达到最高等级！");
        }
        else
        {
            Debug.LogWarning($"金币不足，需要{upgradeCost}金币！");
        }
    }
    
    // 出售选中的塔
    public void SellSelectedTower()
    {
        if (selectedTower == null)
            return;
        
        int sellValue = selectedTower.GetSellValue();
        // 优先使用CoinManager添加金币
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(sellValue);
        }
        else
        {
        currentGold += sellValue;
            UpdateGoldDisplay();
        }
        
        // 找到塔在字典中的位置
        Vector3Int towerPos = Vector3Int.zero;
        foreach (var pair in builtTowers)
        {
            if (pair.Value == selectedTower)
            {
                towerPos = pair.Key;
                break;
            }
        }
        
        // 从字典中移除
        builtTowers.Remove(towerPos);
        
        // 如果使用预设放置点，释放对应的放置点
        if (usePresetPlacementPoints)
        {
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                placementManager.RemoveTowerAt(towerPos);
            }
        }
        
        Debug.Log($"出售{selectedTower.towerName}，获得{sellValue}金币，剩余{currentGold}金币");
        
        // 销毁塔对象
        Destroy(selectedTower.gameObject);
        
        // 清除选中状态和范围指示器
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
        
        selectedTower = null;
        
        // 显示建造面板
        ShowBuildPanel();
    }
    
    // 切换塔类型
    public void SelectTowerType(TowerType type)
    {
        selectedTowerType = type;
        Debug.Log($"已选择{type}塔");
        
        // 更新预览精灵
        UpdatePreviewSprite();
    }
    
    // 获取塔预制体
    private GameObject GetTowerPrefab(TowerType type)
    {
        switch (type)
        {
            case TowerType.Cannon:
                return cannonTowerPrefab;
            case TowerType.Arrow:
                return arrowTowerPrefab;
            case TowerType.Laser:
                return laserTowerPrefab;
            default:
                return null;
        }
    }
    
    // 增加金币（由GameManager调用）
    public void AddGold(int amount)
    {
        // 优先使用CoinManager添加金币
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
        }
        else
    {
        currentGold += amount;
            UpdateGoldDisplay();
        }
        Debug.Log($"获得{amount}金币，当前金币：{currentGold}");
    }
    
    // UI相关方法
    public void UpdateGoldDisplay()
    {
        // 如果使用CoinManager，则不需要在这里更新UI
        if (CoinManager.Instance != null)
        {
            return;
        }
        
        if (goldText != null)
        {
            goldText.text = $"金币: {currentGold}";
        }
    }
    
    private void ShowBuildPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(true);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }
    
    private void ShowUpgradePanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
            
        // 更新升级按钮状态
        if (upgradeButton != null && selectedTower != null)
        {
            int upgradeCost = selectedTower.GetUpgradeCost();
            bool hasEnoughGold = false;
            
            // 检查是否有足够的金币
            if (CoinManager.Instance != null)
            {
                hasEnoughGold = CoinManager.Instance.HasEnoughCoins(upgradeCost);
            }
            else
            {
                hasEnoughGold = currentGold >= upgradeCost;
            }
            
            upgradeButton.interactable = selectedTower.level < selectedTower.maxLevel && hasEnoughGold;
        }
    }
    
    private void UpdateTowerInfo()
    {
        if (towerInfoText != null && selectedTower != null)
        {
            towerInfoText.text = $"{selectedTower.towerName} (等级 {selectedTower.level}/{selectedTower.maxLevel})\n" +
                                 $"伤害: {selectedTower.damage}\n" +
                                 $"攻速: {selectedTower.fireRate}/秒\n" +
                                 $"范围: {selectedTower.range}";
                                 
            if (selectedTower.level < selectedTower.maxLevel)
            {
                towerInfoText.text += $"\n升级费用: {selectedTower.GetUpgradeCost()}";
            }
            else
            {
                towerInfoText.text += "\n已达最高等级";
            }
            
            towerInfoText.text += $"\n出售价值: {selectedTower.GetSellValue()}";
        }
    }
    
    // UI按钮回调
    public void OnCannonTowerButton()
    {
        SelectTowerType(TowerType.Cannon);
    }
    
    public void OnArrowTowerButton()
    {
        SelectTowerType(TowerType.Arrow);
    }
    
    public void OnLaserTowerButton()
    {
        SelectTowerType(TowerType.Laser);
    }
    
    public void OnUpgradeButtonClick()
    {
        UpgradeSelectedTower();
    }
    
    public void OnSellButtonClick()
    {
        SellSelectedTower();
    }
    
    // 处理放置点选中/取消选中事件
    public void OnPlacementPointSelected(TowerPlacementPoint point)
    {
        // 在UI中显示可以在该点建造的塔信息
        // 可以根据游戏设计添加特殊逻辑
    }

    public void OnPlacementPointDeselected()
    {
        // 在UI中隐藏建造信息
    }

    // 添加用于从UI调用的建造塔方法
    public void BuildTowerButtonClicked()
    {
        // 如果是使用预设放置点且有选中的点
        if (usePresetPlacementPoints && selectedPlacementPoint != null)
        {
            BuildTowerOnSelectedPoint();
        }
        else
        {
            // 普通放置模式下，不需要额外操作
            // 因为会在点击地图时通过HandleMouseClick处理
        }
    }

    // 显示通知
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            
            // 几秒后隐藏通知
            CancelInvoke("HideNotification");
            Invoke("HideNotification", notificationDuration);
        }
    }
    
    // 隐藏通知
    private void HideNotification()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }
} 