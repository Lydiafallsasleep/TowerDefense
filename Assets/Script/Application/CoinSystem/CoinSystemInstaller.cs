using UnityEngine;
using UnityEngine.UI;

public class CoinSystemInstaller : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject coinManagerPrefab;
    public GameObject coinSaveSystemPrefab;
    public GameObject coinDebuggerPrefab;
    public GameObject coinUIPrefab;
    
    [Header("UI Settings")]
    [SerializeField] private string uiPanelName = "CoinPanel";
    [SerializeField] private string coinLabelText = "Coins:";
    [SerializeField] private Vector2 uiPosition = new Vector2(10, -10);
    
    private void Start()
    {
        // Install coin system
        InstallCoinSystem();
    }
    
    public void InstallCoinSystem()
    {
        // 1. Check and create CoinManager
        if (CoinManager.Instance == null && coinManagerPrefab != null)
        {
            Instantiate(coinManagerPrefab);
            Debug.Log("[CoinSystemInstaller] Created CoinManager");
        }
        
        // 2. Check and create CoinSaveSystem
        if (FindObjectOfType<CoinSaveSystem>() == null && coinSaveSystemPrefab != null)
        {
            Instantiate(coinSaveSystemPrefab);
            Debug.Log("[CoinSystemInstaller] Created CoinSaveSystem");
        }
        
        // 3. Check and create CoinDebugger
        if (FindObjectOfType<CoinDebugger>() == null && coinDebuggerPrefab != null)
        {
            Instantiate(coinDebuggerPrefab);
            Debug.Log("[CoinSystemInstaller] Created CoinDebugger");
        }
        
        // 4. Create UI
        InstallCoinUI();
    }
    
    private void InstallCoinUI()
    {
        if (coinUIPrefab == null)
            return;
            
        // Find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // If no Canvas exists, create one
            GameObject canvasObject = new GameObject("UICanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // Set up Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[CoinSystemInstaller] Created UI Canvas");
        }
        
        // Check if CoinUI already exists
        if (FindObjectOfType<CoinUI>() != null)
        {
            Debug.Log("[CoinSystemInstaller] CoinUI already exists, skipping creation");
            return;
        }
        
        // Create coin UI panel
        GameObject coinPanel = new GameObject(uiPanelName);
        coinPanel.transform.SetParent(canvas.transform, false);
        
        // Set position (top left corner of screen)
        RectTransform rectTransform = coinPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = uiPosition;
        
        // Create label text
        GameObject labelObject = new GameObject("CoinLabel");
        labelObject.transform.SetParent(coinPanel.transform, false);
        
        // Set up label
        Text labelText = labelObject.AddComponent<Text>();
        labelText.text = coinLabelText;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 24;
        labelText.color = Color.white;
        
        // Position the label
        RectTransform labelRectTransform = labelText.rectTransform;
        labelRectTransform.anchorMin = new Vector2(0, 0.5f);
        labelRectTransform.anchorMax = new Vector2(0, 0.5f);
        labelRectTransform.pivot = new Vector2(0, 0.5f);
        labelRectTransform.anchoredPosition = new Vector2(0, 0);
        labelRectTransform.sizeDelta = new Vector2(100, 30);
        
        // Create coin value text
        GameObject valueObj = new GameObject("CoinValue");
        valueObj.transform.SetParent(coinPanel.transform, false);
        Text valueText = valueObj.AddComponent<Text>();
        
        // Set value text
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0, 0.5f);
        valueRect.anchorMax = new Vector2(0, 0.5f);
        valueRect.pivot = new Vector2(0, 0.5f);
        valueRect.anchoredPosition = new Vector2(100, 0);
        valueRect.sizeDelta = new Vector2(100, 40);
        
        valueText.text = "0";
        valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        valueText.fontSize = 24;
        valueText.color = Color.yellow;
        valueText.alignment = TextAnchor.MiddleLeft;
        
        // Add CoinUI component
        CoinUI coinUI = coinPanel.AddComponent<CoinUI>();
        coinUI.coinText = valueText;
        
        Debug.Log("[CoinSystemInstaller] Created CoinUI");
    }
}