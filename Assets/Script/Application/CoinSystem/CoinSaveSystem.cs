using UnityEngine;
using System;

public class CoinSaveSystem : MonoBehaviour
{
    [Header("存储设置")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 60f; // 60秒自动保存一次
    
    private const string COINS_SAVE_KEY = "player_coins";
    private float timeSinceLastSave;
    
    private void Start()
    {
        // 加载保存的金币数据
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
    
    // 在游戏退出时保存
    private void OnApplicationQuit()
    {
        SaveCoins();
    }
    
    // 当游戏暂停时保存
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCoins();
        }
    }
    
    // 保存金币数据
    public void SaveCoins()
    {
        if (CoinManager.Instance == null) return;
        
        PlayerPrefs.SetInt(COINS_SAVE_KEY, CoinManager.Instance.CurrentCoins);
        PlayerPrefs.Save();
        
        Debug.Log($"[CoinSaveSystem] 金币数据已保存: {CoinManager.Instance.CurrentCoins}");
    }
    
    // 加载金币数据
    public void LoadCoins()
    {
        if (CoinManager.Instance == null) return;
        
        if (PlayerPrefs.HasKey(COINS_SAVE_KEY))
        {
            int savedCoins = PlayerPrefs.GetInt(COINS_SAVE_KEY);
            
            // 找到CoinManager并设置起始金币
            CoinManager.Instance.SetStartingCoins(savedCoins);
            
            Debug.Log($"[CoinSaveSystem] 已加载金币数据: {savedCoins}");
        }
        else
        {
            Debug.Log("[CoinSaveSystem] 未找到保存的金币数据，使用默认值");
        }
    }
    
    // 清除保存的数据
    public void ClearSavedData()
    {
        if (PlayerPrefs.HasKey(COINS_SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(COINS_SAVE_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("[CoinSaveSystem] 已清除保存的金币数据");
        }
    }
}