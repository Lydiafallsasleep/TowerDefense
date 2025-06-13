using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 塔操作面板：显示在鼠标点击塔的位置，用于升级和出售塔
/// </summary>
public class TowerOperationPanel : MonoBehaviour
{
    [Header("面板设置")]
    public GameObject panelObject;
    public RectTransform panelRectTransform;
    public float offsetY = 1.5f; // 面板显示在塔上方的Y轴偏移
    
    [Header("面板内容")]
    public TextMeshProUGUI towerNameText;
    public TextMeshProUGUI towerLevelText;
    public TextMeshProUGUI towerStatsText;
    public TextMeshProUGUI upgradeCostText;
    
    [Header("按钮")]
    public Button upgradeButton;
    public Button sellButton;
    
    private BaseTower currentTower;
    private Camera mainCamera;
    
    // 静态实例，方便访问
    public static TowerOperationPanel Instance { get; private set; }
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        mainCamera = Camera.main;
        
        // 初始化面板为隐藏状态
        HidePanel();
        
        // 设置按钮监听
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(UpgradeTower);
        
        if (sellButton != null)
            sellButton.onClick.AddListener(SellTower);
    }

    private void Start()
    {
        // 确保TowerManager知道此面板
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.towerOperationPanel = this;
        }
    }
    
    /// <summary>
    /// 显示面板在指定塔的位置
    /// </summary>
    public void ShowPanelAtTower(BaseTower tower, Vector3 clickPosition)
    {
        if (tower == null || panelObject == null)
            return;
            
        currentTower = tower;
        
        // 确保TowerManager也选中了此塔
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.OnTowerSelected(tower);
        }
        
        // 计算面板位置 - 使用点击位置
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(clickPosition);
        
        // 添加Y轴偏移
        screenPosition.y += offsetY * Screen.height / 10; // 根据屏幕高度按比例调整偏移
        
        // 显示面板
        panelObject.SetActive(true);
        
        // 设置面板位置
        if (panelRectTransform != null)
        {
            panelRectTransform.position = screenPosition;
            
            // 确保面板在屏幕内
            EnsurePanelWithinScreen();
        }
        
        // 更新面板信息
        UpdatePanelInfo();
    }
    
    /// <summary>
    /// 确保面板在屏幕边界内
    /// </summary>
    private void EnsurePanelWithinScreen()
    {
        if (panelRectTransform == null) return;
        
        // 获取面板的尺寸
        Vector2 panelSize = panelRectTransform.sizeDelta;
        Vector3 panelPosition = panelRectTransform.position;
        
        // 确保不超出屏幕右侧
        float rightEdge = panelPosition.x + panelSize.x/2;
        if (rightEdge > Screen.width)
        {
            panelPosition.x -= (rightEdge - Screen.width);
        }
        
        // 确保不超出屏幕左侧
        float leftEdge = panelPosition.x - panelSize.x/2;
        if (leftEdge < 0)
        {
            panelPosition.x += -leftEdge;
        }
        
        // 确保不超出屏幕上方
        float topEdge = panelPosition.y + panelSize.y/2;
        if (topEdge > Screen.height)
        {
            panelPosition.y -= (topEdge - Screen.height);
        }
        
        // 确保不超出屏幕下方
        float bottomEdge = panelPosition.y - panelSize.y/2;
        if (bottomEdge < 0)
        {
            panelPosition.y += -bottomEdge;
        }
        
        // 应用修正后的位置
        panelRectTransform.position = panelPosition;
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        if (panelObject != null)
            panelObject.SetActive(false);
            
        currentTower = null;
    }
    
    private void Update()
    {
        // 点击其他地方时隐藏面板
        if (Input.GetMouseButtonDown(0))
        {
            // 如果点击在UI上，忽略
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
                
            // 检查当前鼠标点击是否在面板之外
            if (panelObject.activeSelf)
            {
                Vector2 localMousePos;
                if (panelRectTransform != null && 
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        panelRectTransform, Input.mousePosition, null, out localMousePos))
                {
                    // 如果点击在面板外，隐藏面板
                    if (!panelRectTransform.rect.Contains(localMousePos))
                    {
                        HidePanel();
                    }
                }
                else
                {
                    // 如果无法确定点击位置，也隐藏面板
                    HidePanel();
                }
            }
        }
        
        // 右键点击时隐藏面板
        if (Input.GetMouseButtonDown(1) && panelObject.activeSelf)
        {
            HidePanel();
        }
        
        // 如果当前塔被销毁，隐藏面板
        if (currentTower == null && panelObject.activeSelf)
        {
            HidePanel();
        }
    }
    
    /// <summary>
    /// 更新面板上的塔信息
    /// </summary>
    private void UpdatePanelInfo()
    {
        if (currentTower == null)
            return;
            
        // 更新塔名称
        if (towerNameText != null)
            towerNameText.text = currentTower.towerName;
            
        // 更新塔等级
        if (towerLevelText != null)
            towerLevelText.text = $"等级: {currentTower.level}/{currentTower.maxLevel}";
            
        // 更新塔属性
        if (towerStatsText != null)
        {
            // 从TowerAttackSystem获取属性
            var attackSystem = currentTower.GetComponent<TowerAttackSystem>();
            if (attackSystem != null)
            {
                towerStatsText.text = $"伤害: {attackSystem.attackDamage:F1}\n射程: {attackSystem.attackRange:F1}\n攻速: {attackSystem.attackSpeed:F2}";
            }
            else
            {
                towerStatsText.text = $"伤害: {currentTower.damage:F1}\n射程: {currentTower.range:F1}\n攻速: {currentTower.fireRate:F2}";
            }
        }
        
        // 更新升级费用
        if (upgradeCostText != null)
        {
            if (currentTower.level < currentTower.maxLevel)
            {
                upgradeCostText.text = $"升级费用: {currentTower.upgradePrice}";
                upgradeCostText.gameObject.SetActive(true);
            }
            else
            {
                upgradeCostText.text = "最高等级";
                upgradeCostText.gameObject.SetActive(true);
            }
        }
        
        // 更新升级按钮状态
        if (upgradeButton != null)
        {
            bool canUpgrade = currentTower.level < currentTower.maxLevel;
            bool hasEnoughGold = false;
            
            // 检查是否有足够的金币
            if (CoinManager.Instance != null)
            {
                hasEnoughGold = CoinManager.Instance.HasEnoughCoins(currentTower.upgradePrice);
            }
            else if (TowerManager.Instance != null)
            {
                hasEnoughGold = true; // TowerManager内部会检查金币
            }
            
            upgradeButton.interactable = canUpgrade && hasEnoughGold;
        }
    }
    
    /// <summary>
    /// 升级塔
    /// </summary>
    private void UpgradeTower()
    {
        if (currentTower == null)
            return;
            
        // 调用TowerManager来升级塔
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.UpgradeSelectedTower();
            
            // 更新面板信息
            UpdatePanelInfo();
        }
        else
        {
            // 直接调用塔的升级方法
            if (currentTower.Upgrade())
            {
                // 更新面板信息
                UpdatePanelInfo();
            }
        }
    }
    
    /// <summary>
    /// 出售塔
    /// </summary>
    private void SellTower()
    {
        if (currentTower == null)
            return;
        
        BaseTower towerToSell = currentTower;
            
        // 调用TowerManager来出售塔
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.SellSelectedTower(currentTower);
        }
        else
        {
            // 直接调用塔的出售方法
            towerToSell.Sell();
        }
        
        // 隐藏面板
        HidePanel();
    }
} 