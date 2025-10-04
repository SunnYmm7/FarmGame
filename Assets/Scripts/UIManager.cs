using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Money Display")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private string moneyPrefix = "$";

    [Header("Farming (Crop) UI")]
    [SerializeField] private GameObject farmingPanel;
    [SerializeField] private Button farmingButton;

    [Header("Building UI")]
    [SerializeField] private GameObject buildingPanel;
    [SerializeField] private Button buildingButton;

    [Header("Manager References")]
    [SerializeField] private BuildingPlacer buildingPlacer;
    [SerializeField] private FarmManager farmManager;

    [Header("Harvest Feedback")]
    [SerializeField] private GameObject harvestPopupPrefab;
    [SerializeField] private Transform uiCanvas;
    [SerializeField] private float popupDuration = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupButtons();
        UpdateMoneyDisplay();
        
        // Make sure both panels are closed at start
        if (farmingPanel != null) farmingPanel.SetActive(false);
        if (buildingPanel != null) buildingPanel.SetActive(false);
    }

    private void OnEnable()
    {
        FarmPlot.OnCropHarvested += ShowHarvestPopup;
    }

    private void OnDisable()
    {
        FarmPlot.OnCropHarvested -= ShowHarvestPopup;
    }

    private void Update()
    {
        UpdateMoneyDisplay();
    }

    private void SetupButtons()
    {
        buildingButton.onClick.AddListener(OnBuildingButtonClick);
        farmingButton.onClick.AddListener(OnFarmingButtonClick);
    }
    
    private void OnBuildingButtonClick()
    {
        bool isBuildingPanelActive = buildingPanel != null && buildingPanel.activeSelf;
        
        // Close farming panel
        if (farmingPanel != null) farmingPanel.SetActive(false);
        if (farmManager != null) farmManager.enabled = false;
        
        // Toggle building panel
        if (buildingPanel != null)
        {
            buildingPanel.SetActive(!isBuildingPanelActive);
        }
        
        // Enable/disable building placer based on panel state
        if (buildingPlacer != null)
        {
            buildingPlacer.enabled = !isBuildingPanelActive;
            
            // Clear selection when closing building panel
            if (isBuildingPanelActive)
            {
                buildingPlacer.ClearSelection();
            }
        }
    }
    
    private void OnFarmingButtonClick()
    {
        bool isFarmingPanelActive = farmingPanel != null && farmingPanel.activeSelf;
        
        // Close building panel
        if (buildingPanel != null) buildingPanel.SetActive(false);
        if (buildingPlacer != null) 
        {
            buildingPlacer.enabled = false;
            buildingPlacer.ClearSelection();
        }
        
        // Toggle farming panel
        if (farmingPanel != null)
        {
            farmingPanel.SetActive(!isFarmingPanelActive);
        }
        
        // Enable/disable farm manager based on panel state
        if (farmManager != null)
        {
            farmManager.enabled = !isFarmingPanelActive;
            
            // Clear crop selection when closing farming panel
            if (isFarmingPanelActive)
            {
                farmManager.ClearCropSelection();
            }
        }
    }
    
    // Method to close all panels (useful for other UI elements)
    public void CloseAllPanels()
    {
        if (farmingPanel != null) farmingPanel.SetActive(false);
        if (buildingPanel != null) buildingPanel.SetActive(false);
        
        if (farmManager != null) 
        {
            farmManager.enabled = false;
            farmManager.ClearCropSelection();
        }
        
        if (buildingPlacer != null) 
        {
            buildingPlacer.enabled = false;
            buildingPlacer.ClearSelection();
        }
    }
    
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null && ResourceManager.Instance != null)
        {
            moneyText.text = $"{moneyPrefix}{ResourceManager.Instance.Money}";
        }
    }

    private void ShowHarvestPopup(int moneyEarned)
    {
        if (harvestPopupPrefab != null && uiCanvas != null)
        {
            GameObject popup = Instantiate(harvestPopupPrefab, uiCanvas);
            TextMeshProUGUI popupText = popup.GetComponentInChildren<TextMeshProUGUI>();
            if (popupText != null)
            {
                popupText.text = $"+{moneyPrefix}{moneyEarned}";
            }
            StartCoroutine(AnimateHarvestPopup(popup));
        }
    }

    private System.Collections.IEnumerator AnimateHarvestPopup(GameObject popup)
    {
        RectTransform rect = popup.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popup.AddComponent<CanvasGroup>();

        Vector3 startPos = rect.localPosition;
        Vector3 endPos = startPos + Vector3.up * 100f;

        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            float t = elapsed / popupDuration;
            rect.localPosition = Vector3.Lerp(startPos, endPos, t);
            canvasGroup.alpha = 1f - t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(popup);
    }

    public void ShowMessage(string message, float duration = 3f)
    {
        Debug.Log(message);
    }
}