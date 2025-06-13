using UnityEngine;

public class CoinDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private int addAmount = 50;
    [SerializeField] private int spendAmount = 20;
    
    [Header("Hotkey Settings")]
    [SerializeField] private KeyCode addCoinsKey = KeyCode.F1;
    [SerializeField] private KeyCode spendCoinsKey = KeyCode.F2;
    [SerializeField] private KeyCode resetCoinsKey = KeyCode.F3;
    
    private void Update()
    {
        if (!enableDebug) return;
        
        // Check if CoinManager exists
        if (CoinManager.Instance == null) return;
        
        // Add coins
        if (Input.GetKeyDown(addCoinsKey))
        {
            CoinManager.Instance.AddCoins(addAmount);
            Debug.Log($"[CoinDebugger] Added {addAmount} coins");
        }
        
        // Try to spend coins
        if (Input.GetKeyDown(spendCoinsKey))
        {
            bool success = CoinManager.Instance.TrySpendCoins(spendAmount);
            Debug.Log($"[CoinDebugger] Tried to spend {spendAmount} coins: {(success ? "Success" : "Failed")}");
        }
        
        // Reset coins
        if (Input.GetKeyDown(resetCoinsKey))
        {
            CoinManager.Instance.ResetCoins();
            Debug.Log("[CoinDebugger] Coins have been reset");
        }
    }
    
    // Methods that can be called directly from the Inspector
    public void AddCoinsFromInspector()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(addAmount);
        }
    }
    
    public void SpendCoinsFromInspector()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.TrySpendCoins(spendAmount);
        }
    }
    
    public void ResetCoinsFromInspector()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.ResetCoins();
        }
    }
}