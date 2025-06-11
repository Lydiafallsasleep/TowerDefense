using UnityEngine;
using UnityEngine.UI;

public class ObstacleUI : MonoBehaviour
{
    [Header("UI元素")]
    public GameObject obstaclePanel;
    public Text costText;
    public Button clearButton;
    
    [Header("通知设置")]
    public Text notificationText;
    public float notificationDuration = 3f;
    
    private ObstacleManager obstacleManager;
    private Vector3Int? currentObstaclePos;
    
    void Start()
    {
        obstacleManager = ObstacleManager.Instance;
        HidePanel();
        
        // 添加按钮事件
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearButtonClick);
        }
    }
    
    // 显示障碍物信息
    public void ShowObstacleInfo(Vector3Int position)
    {
        if (obstacleManager == null || obstaclePanel == null) return;
        
        // 保存当前选中的障碍物位置
        currentObstaclePos = position;
        
        // 更新费用文本
        if (costText != null)
        {
            costText.text = $"清除费用: {obstacleManager.clearCost} 金币";
        }
        
        // 显示面板
        obstaclePanel.SetActive(true);
        
        // 检查金币是否足够
        bool hasEnoughCoins = false;
        
        // 优先使用CoinManager
        if (CoinManager.Instance != null)
        {
            hasEnoughCoins = CoinManager.Instance.HasEnoughCoins(obstacleManager.clearCost);
        }
        // 向后兼容：使用TowerManager
        else if (TowerManager.Instance != null)
        {
            hasEnoughCoins = TowerManager.Instance.currentGold >= obstacleManager.clearCost;
        }
        
        // 更新按钮状态
        if (clearButton != null)
        {
            clearButton.interactable = hasEnoughCoins;
            
            // 如果金币不足，显示提示
            if (!hasEnoughCoins)
            {
                ShowNotification($"金币不足！需要 {obstacleManager.clearCost} 金币清除此障碍物");
            }
        }
    }
    
    // 显示通知消息
    public void ShowNotification(string message)
    {
        // 如果有自己的通知文本
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            
            // 设置定时隐藏
            CancelInvoke("HideNotification");
            Invoke("HideNotification", notificationDuration);
            return;
        }
        
        // 尝试使用TowerManager的通知系统
        if (TowerManager.Instance != null)
        {
            try
            {
                TowerManager.Instance.ShowNotification(message);
            }
            catch (System.Exception)
            {
                Debug.Log(message);
            }
        }
        else
        {
            Debug.Log(message);
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
    
    // 隐藏面板
    public void HidePanel()
    {
        if (obstaclePanel != null)
        {
            obstaclePanel.SetActive(false);
        }
        
        currentObstaclePos = null;
    }
    
    // 清除按钮点击处理
    private void OnClearButtonClick()
    {
        if (obstacleManager == null || !currentObstaclePos.HasValue) return;
        
        Vector3Int obstaclePos = currentObstaclePos.Value;
        
        // 尝试清除障碍物
        if (obstacleManager.ClearObstacle(obstaclePos))
        {
            // 清除成功，刷新金币显示
            if (CoinManager.Instance != null)
            {
                // CoinManager会自动更新UI
            }
            else if (TowerManager.Instance != null)
            {
                TowerManager.Instance.UpdateGoldDisplay();
            }
            
            // 显示提示，告诉玩家可以建塔
            ShowNotification("障碍物已清除，可以在此位置建造防御塔！");
            
            // 查找对应的障碍物放置点，高亮显示它
            HighlightObstaclePlacementPoint(obstaclePos);
            
            // 隐藏面板
            HidePanel();
        }
    }
    
    // 高亮显示与障碍物对应的放置点
    private void HighlightObstaclePlacementPoint(Vector3Int obstaclePos)
    {
        // 查找所有ObstaclePlacementPoint
        ObstaclePlacementPoint[] placementPoints = FindObjectsOfType<ObstaclePlacementPoint>();
        
        foreach (var point in placementPoints)
        {
            if (point.obstaclePosition == obstaclePos)
            {
                // 找到对应位置的放置点，高亮显示
                StartCoroutine(PulseEffect(point));
                
                // 可选：自动选择该放置点
                TowerManager towerManager = TowerManager.Instance;
                if (towerManager != null)
                {
                    towerManager.SelectPlacementPoint(point);
                }
                
                break;
            }
        }
    }
    
    // 产生脉冲效果以吸引玩家注意
    private System.Collections.IEnumerator PulseEffect(ObstaclePlacementPoint point)
    {
        if (point == null || point.placementIndicator == null) yield break;
        
        SpriteRenderer renderer = point.placementIndicator;
        Color originalColor = renderer.color;
        float duration = 2.0f; // 脉冲效果持续2秒
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // 颜色在原始颜色和明亮颜色之间脉动
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            renderer.color = Color.Lerp(originalColor, Color.white, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 恢复原始颜色
        renderer.color = originalColor;
    }
    
    // 这个方法可以被ObstacleManager调用，用于显示UI
    public void OnObstacleSelected(Vector3Int position)
    {
        ShowObstacleInfo(position);
    }
} 