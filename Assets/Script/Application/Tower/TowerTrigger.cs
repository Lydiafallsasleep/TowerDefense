using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TowerTrigger : MonoBehaviour
{
    SpriteRenderer sprite;
    TowerManager manager;
    CoinManager coinManager;

    [Header("操作面板设置")]
    public GameObject operateTower; // 操作面板游戏对象

    [Header("文本组件引用")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI upgradePriceText;
    public TextMeshProUGUI sellValueText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI rangeText;

    [Header("按钮设置")]
    public Button levelUpButton;
    public Button sellButton;
    public Button cancelButton;

    private BaseTower currentTower; // 当前选中的塔

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        manager = FindObjectOfType<TowerManager>();
        coinManager = FindObjectOfType<CoinManager>();

        // 初始时隐藏操作面板
        if (operateTower != null)
        {
            operateTower.SetActive(false);
        }

        // 添加按钮点击事件监听
        if (levelUpButton != null)
        {
            levelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        }
    }

    private void OnMouseEnter()
    {
        sprite.color = new Vector4(0.8f, 0.8f, 0.8f, 1);
    }

    private void OnMouseExit()
    {
        sprite.color = new Vector4(1, 1, 1, 1);
    }

    private void OnMouseDown()
    {
        // 获取点击的塔对象
        GameObject clickedTower = gameObject;
        Debug.Log("选中的塔是"+gameObject);

        // 尝试获取不同类型的塔脚本
        ArrowTower arrowTower = clickedTower.GetComponent<ArrowTower>();
        CannonTower cannonTower = clickedTower.GetComponent<CannonTower>();
        LaserTower laserTower = clickedTower.GetComponent<LaserTower>();

        // 激活操作面板
        if (operateTower != null)
        {
            operateTower.SetActive(true);

            // 根据获取到的脚本类型处理不同的塔
            if (arrowTower != null)
            {
                currentTower = arrowTower;
                UpdateTowerInfoUI(arrowTower);
                if (manager != null) manager.OnTowerSelected(arrowTower);
            }
            else if (cannonTower != null)
            {
                currentTower = cannonTower;
                UpdateTowerInfoUI(cannonTower);
                if (manager != null) manager.OnTowerSelected(cannonTower);
            }
            else if (laserTower != null)
            {
                currentTower = laserTower;
                UpdateTowerInfoUI(laserTower);
                if (manager != null) manager.OnTowerSelected(laserTower);
            }
            else
            {
                Debug.LogWarning("点击的对象不是有效的塔类型");
                operateTower.SetActive(false);
            }
        }
    }

    // 更新操作面板UI信息
    private void UpdateTowerInfoUI(BaseTower tower)
    {
        if (nameText != null) nameText.text = $"{tower.towerName}";
        if (levelText != null) levelText.text = $"{tower.level}";
        if (upgradePriceText != null) upgradePriceText.text = $"{tower.upgradePrice}";
        if (sellValueText != null) sellValueText.text = $"{tower.sellValue}";
        if (damageText != null) damageText.text = $"{tower.damage}";
        if (rangeText != null) rangeText.text = $"{tower.range}";

        // 更新升级按钮状态
        if (levelUpButton != null)
        {
            bool canUpgrade = coinManager != null &&
                            coinManager.HasEnoughCoins(tower.upgradePrice) &&
                            tower.canUpgrade;
            levelUpButton.interactable = canUpgrade;
        }
    }

    // 升级按钮点击处理
    private void OnLevelUpButtonClicked()
    {
        Debug.Log("OnLevelUp");
        if (currentTower == null) return;
        Debug.Log("OnLevelUp");
        // 检查金币是否足够
        if (coinManager != null && coinManager.HasEnoughCoins(currentTower.upgradePrice))
        {

            // 扣除金币
            bool spent = coinManager.TrySpendCoins(currentTower.upgradePrice);
            if (!spent)
            {
                Debug.Log("金币扣除失败");
                return;
            }

            // 执行升级
            bool upgraded = currentTower.Upgrade();

            if (upgraded)
            {
                // 升级成功后更新UI
                UpdateTowerInfoUI(currentTower);

                // 可以在这里添加升级特效或音效
                Debug.Log($"{currentTower.towerName} 升级成功！");

                // 通知管理器塔已升级
                if (manager != null)
                {
                    manager.OnTowerUpgraded(currentTower);
                }
            }
        }
        else
        {
            Debug.Log("金币不足，无法升级");
            // 可以在这里添加金币不足的提示效果
        }
    }

    // 出售按钮点击处理
    private void OnSellButtonClicked()
    {
        if (currentTower == null) return;

        // 获得出售金额
        int sellValue = currentTower.sellValue;

        // 增加金币
        if (coinManager != null)
        {
            coinManager.AddCoins(sellValue);
        }

        // 通知管理器塔被出售
        if (manager != null)
        {
            manager.OnTowerSold(currentTower);
        }

        // 关闭操作面板
        if (operateTower != null)
        {
            operateTower.SetActive(false);
        }

        // 销毁塔对象
        Destroy(currentTower.gameObject);

        Debug.Log($"{currentTower.towerName} 已出售，获得 {sellValue} 金币");

        // 重置当前塔引用
        currentTower = null;
    }

    private void OnCancelButtonClick()
        {
        if (operateTower != null)
        {
            operateTower.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 移除按钮事件监听，防止内存泄漏
        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveListener(OnLevelUpButtonClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnCancelButtonClick);
        }

        if (sellButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelButtonClick);
        }
    }
}