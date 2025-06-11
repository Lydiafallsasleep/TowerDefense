using UnityEngine;
using UnityEngine.UI;

public class CoinSystemInstaller : MonoBehaviour
{
    [Header("预制体设置")]
    [SerializeField] private bool createUIIfMissing = true;
    [SerializeField] private bool createCoinManagerIfMissing = true;
    [SerializeField] private bool createSaveSystemIfMissing = true;
    [SerializeField] private bool createDebuggerIfMissing = true;
    
    [Header("UI设置")]
    [SerializeField] private Transform uiCanvas;
    [SerializeField] private Vector2 coinTextPosition = new Vector2(120, -50);
    [SerializeField] private string coinLabelText = "金币：";
    
    void Awake()
    {
        // 安装金币系统
        InstallCoinSystem();
    }
    
    void InstallCoinSystem()
    {
        // 1. 检查并创建CoinManager
        if (createCoinManagerIfMissing && CoinManager.Instance == null)
        {
            GameObject coinManagerObj = new GameObject("CoinManager");
            coinManagerObj.AddComponent<CoinManager>();
            Debug.Log("[CoinSystemInstaller] 已创建CoinManager");
        }
        
        // 2. 检查并创建CoinSaveSystem
        if (createSaveSystemIfMissing && FindObjectOfType<CoinSaveSystem>() == null)
        {
            GameObject coinSaveObj = new GameObject("CoinSaveSystem");
            coinSaveObj.AddComponent<CoinSaveSystem>();
            Debug.Log("[CoinSystemInstaller] 已创建CoinSaveSystem");
        }
        
        // 3. 检查并创建CoinDebugger
        if (createDebuggerIfMissing && FindObjectOfType<CoinDebugger>() == null)
        {
            GameObject debuggerObj = new GameObject("CoinDebugger");
            debuggerObj.AddComponent<CoinDebugger>();
            Debug.Log("[CoinSystemInstaller] 已创建CoinDebugger");
        }
        
        // 4. 创建UI
        if (createUIIfMissing)
        {
            CreateCoinUI();
        }
    }
    
    void CreateCoinUI()
    {
        // 查找Canvas
        Canvas canvas = uiCanvas?.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        // 如果没有Canvas，创建一个
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 设置Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[CoinSystemInstaller] 已创建UI Canvas");
        }
        
        // 检查是否已存在CoinUI
        if (FindObjectOfType<CoinUI>() != null)
        {
            Debug.Log("[CoinSystemInstaller] CoinUI已存在，跳过创建");
            return;
        }
        
        // 创建金币UI面板
        GameObject coinPanel = new GameObject("CoinPanel");
        coinPanel.transform.SetParent(canvas.transform, false);
        RectTransform coinPanelRect = coinPanel.AddComponent<RectTransform>();
        
        // 设置位置（屏幕左上角）
        coinPanelRect.anchorMin = new Vector2(0, 1);
        coinPanelRect.anchorMax = new Vector2(0, 1);
        coinPanelRect.pivot = new Vector2(0, 1);
        coinPanelRect.anchoredPosition = new Vector2(10, -10);
        coinPanelRect.sizeDelta = new Vector2(200, 50);
        
        // 创建标签文本
        GameObject labelObj = new GameObject("CoinLabel");
        labelObj.transform.SetParent(coinPanelRect, false);
        Text labelText = labelObj.AddComponent<Text>();
        
        // 设置标签
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(70, 40);
        
        labelText.text = coinLabelText;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 24;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        // 创建金币值文本
        GameObject valueObj = new GameObject("CoinValue");
        valueObj.transform.SetParent(coinPanelRect, false);
        Text valueText = valueObj.AddComponent<Text>();
        
        // 设置值文本
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0, 0.5f);
        valueRect.anchorMax = new Vector2(0, 0.5f);
        valueRect.pivot = new Vector2(0, 0.5f);
        valueRect.anchoredPosition = coinTextPosition;
        valueRect.sizeDelta = new Vector2(100, 40);
        
        valueText.text = "0";
        valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        valueText.fontSize = 24;
        valueText.color = Color.yellow;
        valueText.alignment = TextAnchor.MiddleLeft;
        
        // 添加CoinUI组件
        CoinUI coinUI = coinPanel.AddComponent<CoinUI>();
        coinUI.coinText = valueText;
        
        Debug.Log("[CoinSystemInstaller] 已创建金币UI");
    }
}