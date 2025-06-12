using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 波次管理系统，控制敌人分波次生成，并随波次增加敌人属性
/// </summary>
public class WaveManager : Singleton<WaveManager>
{
    [Header("波次设置")]
    public int currentWave = 0;
    public int totalWaves = 10;
    public int enemiesPerWave = 10;
    public float timeBetweenWaves = 10f;
    public float timeBetweenEnemies = 1f;
    
    [Header("难度设置")]
    public float healthIncreasePerWave = 0.1f; // 每波次敌人血量增加10%
    public float eliteHealthIncrease = 0.2f; // 精英敌人额外增加20%血量
    public int eliteWaveFrequency = 5; // 每5波出现精英敌人
    
    [Header("UI引用")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timerText;
    public GameObject waveCompleteBanner;
    public float bannerDisplayTime = 3f;
    
    [Header("音效")]
    public AudioClip waveStartSound;
    public AudioClip waveCompleteSound;
    public AudioClip finalWaveSound;
    
    // 状态
    private bool isWaveInProgress = false;
    private bool isSpawning = false;
    private float waveTimer = 0f;
    private int enemiesRemaining = 0;
    private int enemiesSpawned = 0;
    private bool isGameOver = false;
    private bool isAutoStartWaves = true;
    
    // 引用
    private EnemySpawner enemySpawner;
    private GameManager gameManager;
    private AudioSource audioSource;
    
    void Start()
    {
        // 获取引用
        enemySpawner = FindObjectOfType<EnemySpawner>();
        gameManager = GameManager.Instance;
        
        // 添加音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (waveStartSound != null || waveCompleteSound != null || finalWaveSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 初始化UI
        UpdateWaveUI();
        UpdateTimerUI();
        
        // 隐藏波次完成横幅
        if (waveCompleteBanner != null)
        {
            waveCompleteBanner.SetActive(false);
        }
        
        // 开始第一波
        StartCoroutine(StartFirstWaveAfterDelay(2f));
    }
    
    void Update()
    {
        if (isGameOver) return;
        
        // 如果当前没有波次进行中，更新倒计时
        if (!isWaveInProgress && currentWave < totalWaves && currentWave > 0)
        {
            waveTimer -= Time.deltaTime;
            
            if (waveTimer <= 0f && isAutoStartWaves)
            {
                StartNextWave();
            }
            
            UpdateTimerUI();
        }
        
        // 检查当前波次是否完成
        if (isWaveInProgress && !isSpawning && enemiesRemaining <= 0)
        {
            WaveCompleted();
        }
    }
    
    /// <summary>
    /// 延迟开始第一波
    /// </summary>
    private IEnumerator StartFirstWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextWave();
    }
    
    /// <summary>
    /// 开始下一波敌人
    /// </summary>
    public void StartNextWave()
    {
        if (isWaveInProgress || isGameOver || currentWave >= totalWaves)
            return;
        
        currentWave++;
        
        // 更新GameManager中的波次
        if (gameManager != null)
        {
            gameManager.SetCurrentWave(currentWave);
        }
        
        isWaveInProgress = true;
        enemiesRemaining = enemiesPerWave;
        enemiesSpawned = 0;
        
        // 更新UI
        UpdateWaveUI();
        
        // 播放波次开始音效
        PlayWaveSound();
        
        // 开始生成敌人
        StartCoroutine(SpawnEnemiesInWave());
        
        Debug.Log($"[WaveManager] 开始第 {currentWave} 波，敌人数量: {enemiesPerWave}");
    }
    
    /// <summary>
    /// 生成当前波次的敌人
    /// </summary>
    private IEnumerator SpawnEnemiesInWave()
    {
        isSpawning = true;
        
        // 计算当前波次的敌人血量倍率
        float healthMultiplier = 1f + (healthIncreasePerWave * (currentWave - 1));
        
        for (int i = 0; i < enemiesPerWave; i++)
        {
            // 检查是否为精英敌人（每eliteWaveFrequency波中的最后一个敌人）
            bool isEliteEnemy = (currentWave % eliteWaveFrequency == 0) && (i == enemiesPerWave - 1);
            
            // 精英敌人有额外的血量加成
            float enemyHealthMultiplier = healthMultiplier;
            if (isEliteEnemy)
            {
                enemyHealthMultiplier += eliteHealthIncrease;
            }
            
            // 生成敌人
            SpawnEnemy(enemyHealthMultiplier, isEliteEnemy);
            
            enemiesSpawned++;
            
            // 等待一段时间再生成下一个敌人
            yield return new WaitForSeconds(timeBetweenEnemies);
        }
        
        isSpawning = false;
    }
    
    /// <summary>
    /// 生成单个敌人
    /// </summary>
    private void SpawnEnemy(float healthMultiplier, bool isElite)
    {
        if (enemySpawner != null)
        {
            // 随机选择敌人类型
            EnemyMovement.MonsterType enemyType = (Random.value > 0.5f) ? 
                EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish;
            
            // 生成敌人
            GameObject enemy = enemySpawner.SpawnEnemyWithType(enemyType);
            
            if (enemy != null)
            {
                // 设置敌人血量
                EnemyHealth healthComponent = enemy.GetComponent<EnemyHealth>();
                if (healthComponent != null)
                {
                    healthComponent.SetHealthMultiplier(healthMultiplier);
                    
                    // 如果是精英敌人，设置为精英
                    if (isElite)
                    {
                        healthComponent.SetElite(true);
                    }
                }
                else
                {
                    Debug.LogWarning("[WaveManager] 敌人没有EnemyHealth组件，无法设置血量倍率");
                }
            }
        }
        else
        {
            Debug.LogError("[WaveManager] 未找到EnemySpawner组件，无法生成敌人");
        }
    }
    
    /// <summary>
    /// 当前波次完成
    /// </summary>
    private void WaveCompleted()
    {
        isWaveInProgress = false;
        
        // 播放波次完成音效
        if (audioSource != null && waveCompleteSound != null)
        {
            audioSource.PlayOneShot(waveCompleteSound);
        }
        
        // 显示波次完成横幅
        StartCoroutine(ShowWaveCompleteBanner());
        
        // 如果这是最后一波，触发游戏胜利
        if (currentWave >= totalWaves)
        {
            GameVictory();
            return;
        }
        
        // 设置下一波的倒计时
        waveTimer = timeBetweenWaves;
        UpdateTimerUI();
        
        Debug.Log($"[WaveManager] 第 {currentWave} 波完成，{timeBetweenWaves} 秒后开始下一波");
    }
    
    /// <summary>
    /// 显示波次完成横幅
    /// </summary>
    private IEnumerator ShowWaveCompleteBanner()
    {
        if (waveCompleteBanner != null)
        {
            waveCompleteBanner.SetActive(true);
            
            // 设置横幅文本
            TextMeshProUGUI bannerText = waveCompleteBanner.GetComponentInChildren<TextMeshProUGUI>();
            if (bannerText != null)
            {
                if (currentWave >= totalWaves)
                {
                    bannerText.text = "所有波次完成！";
                }
                else
                {
                    bannerText.text = $"第 {currentWave} 波完成！";
                }
            }
            
            yield return new WaitForSeconds(bannerDisplayTime);
            waveCompleteBanner.SetActive(false);
        }
    }
    
    /// <summary>
    /// 游戏胜利
    /// </summary>
    private void GameVictory()
    {
        Debug.Log("[WaveManager] 所有波次完成，游戏胜利！");
        
        // 通知GameManager游戏胜利
        if (gameManager != null)
        {
            // 假设GameManager有Victory方法
            if (gameManager.GetType().GetMethod("Victory") != null)
            {
                gameManager.SendMessage("Victory");
            }
        }
    }
    
    /// <summary>
    /// 更新波次UI
    /// </summary>
    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"波次: {currentWave}/{totalWaves}";
        }
    }
    
    /// <summary>
    /// 更新计时器UI
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            if (isWaveInProgress)
            {
                timerText.text = "进行中...";
            }
            else if (currentWave >= totalWaves)
            {
                timerText.text = "已完成";
            }
            else
            {
                timerText.text = $"下一波: {Mathf.CeilToInt(waveTimer)}s";
            }
        }
    }
    
    /// <summary>
    /// 播放波次相关音效
    /// </summary>
    private void PlayWaveSound()
    {
        if (audioSource == null)
            return;
            
        if (currentWave == totalWaves && finalWaveSound != null)
        {
            // 最后一波使用特殊音效
            audioSource.PlayOneShot(finalWaveSound);
        }
        else if (waveStartSound != null)
        {
            // 普通波次音效
            audioSource.PlayOneShot(waveStartSound);
        }
    }
    
    /// <summary>
    /// 敌人死亡时调用
    /// </summary>
    public void EnemyDefeated()
    {
        if (isWaveInProgress)
        {
            enemiesRemaining--;
        }
    }
    
    /// <summary>
    /// 获取当前波次
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// 获取总波次
    /// </summary>
    public int GetTotalWaves()
    {
        return totalWaves;
    }
    
    /// <summary>
    /// 设置游戏结束状态
    /// </summary>
    public void SetGameOver(bool gameOver)
    {
        isGameOver = gameOver;
        
        if (isGameOver)
        {
            StopAllCoroutines();
            isWaveInProgress = false;
            isSpawning = false;
        }
    }
    
    /// <summary>
    /// 重置波次管理器状态
    /// </summary>
    public void ResetState()
    {
        Debug.Log("[WaveManager] 重置波次管理器状态");
        
        // 停止所有协程
        StopAllCoroutines();
        
        // 重置状态
        currentWave = 0;
        isWaveInProgress = false;
        isSpawning = false;
        enemiesRemaining = 0;
        enemiesSpawned = 0;
        isGameOver = false;
        waveTimer = 0f;
        
        // 更新UI
        UpdateWaveUI();
        UpdateTimerUI();
        
        // 隐藏波次完成横幅
        if (waveCompleteBanner != null)
        {
            waveCompleteBanner.SetActive(false);
        }
        
        // 开始第一波
        StartCoroutine(StartFirstWaveAfterDelay(2f));
        
        Debug.Log("[WaveManager] 波次管理器状态已重置");
    }
}