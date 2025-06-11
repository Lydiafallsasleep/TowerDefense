using UnityEngine;
using System;

public class CoinManager : Singleton<CoinManager>
{
    [Header("金币设置")]
    [SerializeField] private int startingCoins = 200;
    [SerializeField] private int maxCoins = 9999;
    
    private int _currentCoins;
    
    // 事件系统，用于通知UI更新
    public event Action<int> OnCoinsChanged;
    
    public int CurrentCoins
    {
        get { return _currentCoins; }
        private set
        {
            int newValue = Mathf.Clamp(value, 0, maxCoins);
            if (_currentCoins != newValue)
            {
                _currentCoins = newValue;
                OnCoinsChanged?.Invoke(_currentCoins);
                Debug.Log($"金币更新：{_currentCoins}");
            }
        }
    }
    
    protected override void Awake()
    {
        base.Awake();
        InitializeCoins();
    }
    
    private void InitializeCoins()
    {
        CurrentCoins = startingCoins;
    }
    
    public bool HasEnoughCoins(int amount)
    {
        return CurrentCoins >= amount;
    }
    
    public bool TrySpendCoins(int amount)
    {
        if (!HasEnoughCoins(amount))
        {
            Debug.Log($"金币不足！需要{amount}，当前{CurrentCoins}");
            return false;
        }
        
        CurrentCoins -= amount;
        return true;
    }
    
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        CurrentCoins += amount;
    }
    
    // 用于调试的方法
    public void ResetCoins()
    {
        CurrentCoins = startingCoins;
    }
    
    // 设置起始金币数量
    public void SetStartingCoins(int amount)
    {
        startingCoins = Mathf.Max(0, amount);
        ResetCoins();
    }

       // 在任何脚本中添加以下代码
   void Update()
   {
       if (Input.GetKeyDown(KeyCode.M)) // 按M键测试
       {
           CoinManager.Instance.AddCoins(10);
       }
       if (Input.GetKeyDown(KeyCode.N)) // 按M键测试
       {
           CoinManager.Instance.TrySpendCoins(10);
       }
   }
}