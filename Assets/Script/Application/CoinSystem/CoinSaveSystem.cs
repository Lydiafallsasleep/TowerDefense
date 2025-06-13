using UnityEngine;
using System;

public class CoinSaveSystem : MonoBehaviour
{
    [Header("Storage Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 60f; // Auto save every 60 seconds
    
    private const string COINS_SAVE_KEY = "player_coins";
    private float timeSinceLastSave;
    
    private void Start()
    {
        // Load saved coin data
        LoadCoins();
    }
    
    private void Update()
    {
        if (autoSave)
        {
            timeSinceLastSave += Time.deltaTime;
            
            if (timeSinceLastSave >= autoSaveInterval)
            {
                SaveCoins();
                timeSinceLastSave = 0f;
            }
        }
    }
    
    // Save on game exit
    private void OnApplicationQuit()
    {
        SaveCoins();
    }
    
    // Save when game is paused
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCoins();
        }
    }
    
    // Save coin data
    public void SaveCoins()
    {
        if (CoinManager.Instance == null) return;
        
        PlayerPrefs.SetInt(COINS_SAVE_KEY, CoinManager.Instance.CurrentCoins);
        PlayerPrefs.Save();
        
        Debug.Log($"[CoinSaveSystem] Coin data saved: {CoinManager.Instance.CurrentCoins}");
    }
    
    // Load coin data
    public void LoadCoins()
    {
        if (CoinManager.Instance == null) return;
        
        if (PlayerPrefs.HasKey(COINS_SAVE_KEY))
        {
            int savedCoins = PlayerPrefs.GetInt(COINS_SAVE_KEY);
            
            // Find CoinManager and set starting coins
            CoinManager.Instance.SetStartingCoins(savedCoins);
            
            Debug.Log($"[CoinSaveSystem] Coin data loaded: {savedCoins}");
        }
        else
        {
            Debug.Log("[CoinSaveSystem] No saved coin data found, using default value");
        }
    }
    
    // Clear saved data
    public void ClearSavedData()
    {
        if (PlayerPrefs.HasKey(COINS_SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(COINS_SAVE_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("[CoinSaveSystem] Saved coin data cleared");
        }
    }
}