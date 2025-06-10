using UnityEngine;
using UnityEngine.Tilemaps;
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
    
    // 记录已建造的塔
    private Dictionary<Vector3Int, BaseTower> builtTowers = new Dictionary<Vector3Int, BaseTower>();
    
    // 当前选中的塔类型
    private TowerType selectedTowerType = TowerType.Cannon;
    
    // 当前选中的塔(用于升级或出售)
    private BaseTower selectedTower;
    
    // 塔的预览
    private GameObject towerPreview;
    private SpriteRenderer previewRenderer;
    
    void Start()
    {
        // 初始化预览
        CreateTowerPreview();
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
        bool canPlaceTower = CanPlaceTower(cellPos);
        
        // 更新预览颜色
        Color color = canPlaceTower ? validPlacementColor : invalidPlacementColor;
        color.a = 0.5f; // 半透明
        previewRenderer.color = color;
    }
    
    void HandleMouseClick()
    {
        // 获取鼠标位置
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 将世界坐标转换为格子坐标
        Vector3Int cellPos = placementTilemap.WorldToCell(mouseWorldPos);
        
        // 检查该位置是否已经有塔
        if (builtTowers.ContainsKey(cellPos))
        {
            // 已有塔，选中它
            SelectTower(builtTowers[cellPos]);
            return;
        }
        
        // 新建塔
        if (CanPlaceTower(cellPos))
        {
            BuildTower(cellPos);
        }
    }
    
    bool CanPlaceTower(Vector3Int cellPos)
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
    
    void BuildTower(Vector3Int cellPos)
    {
        // 获取当前选中塔的预制体
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab == null)
            return;
        
        // 检查金币是否足够
        BaseTower towerScript = prefab.GetComponent<BaseTower>();
        if (towerScript != null && currentGold < towerScript.buildCost)
        {
            Debug.LogWarning("金币不足，无法建造塔！");
            return;
        }
        
        // 将格子坐标转换回世界坐标（居中）
        Vector3 cellWorldPos = placementTilemap.GetCellCenterWorld(cellPos);
        
        // 创建塔
        GameObject tower = Instantiate(prefab, cellWorldPos, Quaternion.identity);
        
        // 获取塔脚本
        BaseTower towerComponent = tower.GetComponent<BaseTower>();
        
        if (towerComponent != null)
        {
            // 扣除金币
            currentGold -= towerComponent.buildCost;
            
            // 记录已建造的塔
            builtTowers[cellPos] = towerComponent;
            
            Debug.Log($"建造{towerComponent.towerName}，花费{towerComponent.buildCost}金币，剩余{currentGold}金币");
        }
    }
    
    void SelectTower(BaseTower tower)
    {
        selectedTower = tower;
        Debug.Log($"已选中{tower.towerName}，等级：{tower.level}/{tower.maxLevel}");
        // 此处可以触发UI显示，显示塔的详情和升级/出售按钮
    }
    
    // 升级选中的塔
    public void UpgradeSelectedTower()
    {
        if (selectedTower == null)
            return;
        
        int upgradeCost = selectedTower.GetUpgradeCost();
        
        if (currentGold >= upgradeCost && selectedTower.level < selectedTower.maxLevel)
        {
            currentGold -= upgradeCost;
            selectedTower.Upgrade();
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
        currentGold += sellValue;
        
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
        
        Debug.Log($"出售{selectedTower.towerName}，获得{sellValue}金币，剩余{currentGold}金币");
        
        // 销毁塔对象
        Destroy(selectedTower.gameObject);
        
        // 清除选中状态
        selectedTower = null;
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
        currentGold += amount;
        Debug.Log($"获得{amount}金币，当前金币：{currentGold}");
    }
} 