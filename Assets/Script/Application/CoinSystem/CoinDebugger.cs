using UnityEngine;

public class CoinDebugger : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private int addAmount = 50;
    [SerializeField] private int spendAmount = 20;
    
    [Header("快捷键设置")]
    [SerializeField] private KeyCode addCoinsKey = KeyCode.F1;
    [SerializeField] private KeyCode spendCoinsKey = KeyCode.F2;
    [SerializeField] private KeyCode resetCoinsKey = KeyCode.F3;
    
    private void Update()
    {
        if (!enableDebug) return;
        
        // 检查CoinManager是否存在
        if (CoinManager.Instance == null) return;
        
        // 增加金币
        if (Input.GetKeyDown(addCoinsKey))
        {
            CoinManager.Instance.AddCoins(addAmount);
            Debug.Log($"[CoinDebugger] 添加了 {addAmount} 金币");
        }
        
        // 尝试消费金币
        if (Input.GetKeyDown(spendCoinsKey))
        {
            bool success = CoinManager.Instance.TrySpendCoins(spendAmount);
            Debug.Log($"[CoinDebugger] 尝试消费 {spendAmount} 金币: {(success ? "成功" : "失败")}");
        }
        
        // 重置金币
        if (Input.GetKeyDown(resetCoinsKey))
        {
            CoinManager.Instance.ResetCoins();
            Debug.Log("[CoinDebugger] 金币已重置");
        }
    }
    
    // 在Inspector中可以直接调用的方法
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