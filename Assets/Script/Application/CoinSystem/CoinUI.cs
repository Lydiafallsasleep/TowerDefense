using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("UI引用")]
    public Text coinText; // 传统UGUI Text
    public TextMeshProUGUI coinTMP; // TextMeshPro支持
    
    [Header("动画设置")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Color positiveChangeColor = Color.green;
    [SerializeField] private Color negativeChangeColor = Color.red;
    
    private int lastCoinAmount;
    private Coroutine animationCoroutine;
    
    private void Start()
    {
        // 注册金币变更事件
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
            
            // 初始显示
            UpdateCoinDisplay(CoinManager.Instance.CurrentCoins);
        }
        else
        {
            Debug.LogError("找不到CoinManager实例！");
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册事件
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
        }
    }
    
    private void UpdateCoinDisplay(int amount)
    {
        // 更新UI文本
        string coinString = amount.ToString();
        
        if (coinText != null)
        {
            coinText.text = coinString;
        }
        
        if (coinTMP != null)
        {
            coinTMP.text = coinString;
        }
        
        // 如果启用了动画效果，并且不是初始值
        if (animateChanges && lastCoinAmount != 0)
        {
            AnimateCoinChange(lastCoinAmount, amount);
        }
        
        lastCoinAmount = amount;
    }
    
    private void AnimateCoinChange(int oldValue, int newValue)
    {
        // 如果有正在进行的动画，停止它
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 设置颜色
        Color targetColor = newValue > oldValue ? positiveChangeColor : negativeChangeColor;
        
        // 应用颜色
        if (coinText != null)
        {
            coinText.color = targetColor;
        }
        
        if (coinTMP != null)
        {
            coinTMP.color = targetColor;
        }
        
        // 恢复原色
        Invoke("ResetTextColor", animationDuration);
    }
    
    private void ResetTextColor()
    {
        // 恢复默认颜色
        if (coinText != null)
        {
            coinText.color = Color.white;
        }
        
        if (coinTMP != null)
        {
            coinTMP.color = Color.white;
        }
    }
    
    // 用于手动更新显示
    public void RefreshDisplay()
    {
        if (CoinManager.Instance != null)
        {
            UpdateCoinDisplay(CoinManager.Instance.CurrentCoins);
        }
    }
}