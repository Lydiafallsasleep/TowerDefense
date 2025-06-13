using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("UI References")]
    public Text coinText; // Traditional UGUI Text
    public TextMeshProUGUI coinTMP; // TextMeshPro support
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Color positiveChangeColor = Color.green;
    [SerializeField] private Color negativeChangeColor = Color.red;
    
    private int lastCoinAmount;
    private Coroutine animationCoroutine;
    
    private void Start()
    {
        // Register coin change event
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
            
            // Initial display
            UpdateCoinDisplay(CoinManager.Instance.CurrentCoins);
        }
        else
        {
            Debug.LogError("CoinManager instance not found!");
        }
    }
    
    private void OnDestroy()
    {
        // Unregister event
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
        }
    }
    
    private void UpdateCoinDisplay(int amount)
    {
        // Update UI text
        string coinString = amount.ToString();
        
        if (coinText != null)
        {
            coinText.text = coinString;
        }
        
        if (coinTMP != null)
        {
            coinTMP.text = coinString;
        }
        
        // If animation is enabled and not initial value
        if (animateChanges && lastCoinAmount != 0)
        {
            AnimateCoinChange(lastCoinAmount, amount);
        }
        
        lastCoinAmount = amount;
    }
    
    private void AnimateCoinChange(int oldValue, int newValue)
    {
        // If there's an ongoing animation, stop it
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // Set color
        Color targetColor = newValue > oldValue ? positiveChangeColor : negativeChangeColor;
        
        // Apply color
        if (coinText != null)
        {
            coinText.color = targetColor;
        }
        
        if (coinTMP != null)
        {
            coinTMP.color = targetColor;
        }
        
        // Reset color
        Invoke("ResetTextColor", animationDuration);
    }
    
    private void ResetTextColor()
    {
        // Restore default color
        if (coinText != null)
        {
            coinText.color = Color.white;
        }
        
        if (coinTMP != null)
        {
            coinTMP.color = Color.white;
        }
    }
    
    // For manually updating display
    public void RefreshDisplay()
    {
        if (CoinManager.Instance != null)
        {
            UpdateCoinDisplay(CoinManager.Instance.CurrentCoins);
        }
    }
}