using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class towerT : MonoBehaviour
{
    SpriteRenderer spriteRend;

    [Header("Tower Properties")]
    public string towerName = "Arrow Tower";
    public int level = 1;
    public int upgradeCost = 100;
    public int sellValue = 70;

    [Header("UI conponent")]
    public TextMesh towerNameText;
    public TextMesh upgradePriceText;
    public TextMesh sellPriceText;

    [Header("button")]
    public Button updatebutton;
    public Button sellbutton;

    // UI References
    public GameObject operationPanel; // Assign this in the Inspector
    private Text nameText;
    private Text levelText;
    private Text upgradeText;
    private Button upgradeButton;
    private Button sellButton;

    // Start is called before the first frame update
    protected void Awake()
    {
        UpdateUIText();
    }

    private void Start()
    {
        UpdateUIText();
        spriteRend = GetComponent<SpriteRenderer>();

        // Initialize references when panel is instantiated

        operationPanel.SetActive(false); // Hide panel initially

        // Get references to UI components
        nameText = operationPanel.transform.Find("NameText").GetComponent<Text>();
        levelText = operationPanel.transform.Find("LevelText").GetComponent<Text>();
        upgradeText = operationPanel.transform.Find("UpgradeText").GetComponent<Text>();
        upgradeButton = operationPanel.transform.Find("UpgradeButton").GetComponent<Button>();
        sellButton = operationPanel.transform.Find("SellButton").GetComponent<Button>();

        // Add button listeners
        upgradeButton.onClick.AddListener(UpgradeTower);
        sellButton.onClick.AddListener(SellTower);
    }

    // Update UI TextMesh components with tower information
    private void UpdateUIText()
    {
        // Update TextMesh components if they are assigned
        if (towerNameText != null)
        {
            towerNameText.text = towerName;
        }

        if (upgradePriceText != null)
        {
            upgradePriceText.text = upgradeCost.ToString();
        }

        if (sellPriceText != null)
        {
            sellPriceText.text = sellValue.ToString();
        }
    }

    private void OnMouseDown()
    {
        // Toggle panel visibility
        bool shouldShow = !operationPanel.activeSelf;

        // Hide all other tower panels first
        HideAllTowerPanels();

        // Show/hide this tower's panel
        operationPanel.SetActive(shouldShow);

        if (shouldShow)
        {
            // Update panel information
            UpdatePanelInfo();

            // Position the panel near the tower
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            operationPanel.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(screenPos.x - Screen.width / 2, screenPos.y - Screen.height / 2 + 100);
        }
    }

    private void HideAllTowerPanels()
    {
        // Find all arrow towers and hide their panels
        towerT[] allTowers = FindObjectsOfType<towerT>();
        foreach (var tower in allTowers)
        {
            if (tower.operationPanel != null)
            {
                tower.operationPanel.SetActive(false);
            }
        }
    }

    private void UpdatePanelInfo()
    {
        nameText.text = "Tower: " + towerName;
        levelText.text = "Level: " + level;
        upgradeText.text = "Upgrade: " + upgradeCost;
        sellButton.GetComponentInChildren<Text>().text = "Sell (" + sellValue + ")";
    }

    private void UpgradeTower()
    {
        // Here you would add your game's currency check and deduction logic
        // For now we'll just upgrade without checking

        level++;
        upgradeCost = Mathf.RoundToInt(upgradeCost * 1.5f);
        sellValue = Mathf.RoundToInt(sellValue * 1.3f);

        // Update both panel and TextMesh components
        UpdatePanelInfo();
        UpdateUIText();

        // You might also want to change the tower's appearance or stats here
        Debug.Log(towerName + " upgraded to level " + level);
    }

    private void SellTower()
    {
        // Here you would add the sell value to player's currency
        Debug.Log(towerName + " sold for " + sellValue);

        Destroy(operationPanel);
        Destroy(gameObject);
    }

    // Public method to set tower properties from external scripts
    public void SetTowerProperties(string name, int lvl, int upgrade, int sell)
    {
        towerName = name;
        level = lvl;
        upgradeCost = upgrade;
        sellValue = sell;

        // Update UI with new values
        UpdateUIText();

        // If panel is active, also update panel info
        if (operationPanel != null && operationPanel.activeSelf)
        {
            UpdatePanelInfo();
        }
    }

    private void OnMouseEnter()
    {
        spriteRend.color = new Vector4(0.8f, 0.8f, 0.8f, 1f);
    }

    private void OnMouseExit()
    {
        spriteRend.color = new Vector4(1f, 1f, 1f, 1f);
    }

    // Hide panel when clicking elsewhere
    private void Update()
    {
        if (operationPanel != null && operationPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // Check if click was outside the panel and tower
            if (!IsMouseOverUI() && !IsMouseOverTower())
            {
                operationPanel.SetActive(false);
            }
        }
    }

    private bool IsMouseOverUI()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            operationPanel.GetComponent<RectTransform>(),
            Input.mousePosition,
            null);
    }

    private bool IsMouseOverTower()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePosition);
        return hit != null && hit.gameObject == gameObject;
    }

    private void OnDestroy()
    {
        if (operationPanel != null)
        {
            Destroy(operationPanel);
        }
    }
}