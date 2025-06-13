using UnityEngine;
using System;

public class CoinManager : Singleton<CoinManager>
{
    [Header("Coin Settings")]
    [SerializeField] private int startingCoins = 200;
    [SerializeField] private int maxCoins = 9999;
    
    private int _currentCoins;
    
    // Event system for notifying UI updates
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
                Debug.Log($"Coins updated: {_currentCoins}");
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
            Debug.Log($"Not enough coins! Need {amount}, current {CurrentCoins}");
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
    
    // Method for debugging
    public void ResetCoins()
    {
        CurrentCoins = startingCoins;
    }
    
    // Set starting coin amount
    public void SetStartingCoins(int amount)
    {
        startingCoins = Mathf.Max(0, amount);
        ResetCoins();
    }

    // Add the following code in any script
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) // Press M key to test
        {
            CoinManager.Instance.AddCoins(10);
        }
        if (Input.GetKeyDown(KeyCode.N)) // Press N key to test
        {
            CoinManager.Instance.TrySpendCoins(10);
        }
    }
}