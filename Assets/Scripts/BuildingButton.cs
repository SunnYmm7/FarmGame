using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image buildingIcon;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI buildingCostText;
    [SerializeField] private TextMeshProUGUI buildingLimitText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject limitReachedOverlay;
    
    private BuildingData buildingData;
    private BuildingPlacer buildingPlacer;
    private int buildingIndex;
    
    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }
    
    public void Setup(BuildingData building, BuildingPlacer placer, int index)
    {
        buildingData = building;
        buildingPlacer = placer;
        buildingIndex = index;
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (buildingData == null) return;
        
        // Update building name
        if (buildingNameText != null)
        {
            buildingNameText.text = buildingData.name;
        }
        
        // Update cost
        if (buildingCostText != null)
        {
            buildingCostText.text = $"${buildingData.cost}";
        }
        
        // Update building limits
        if (buildingLimitText != null && TownHallManager.Instance != null)
        {
            int current = TownHallManager.Instance.GetBuildingCount(buildingData.buildingType);
            int max = TownHallManager.Instance.GetBuildingLimit(buildingData.buildingType);
            buildingLimitText.text = $"{current}/{max}";
        }
        
        // Check building placement status
        if (buildingPlacer != null)
        {
            BuildingPlacementResult result = buildingPlacer.CheckBuildingPlacement(buildingData);
            UpdateButtonState(result);
        }
    }
    
    private void UpdateButtonState(BuildingPlacementResult result)
    {
        bool isUnlocked = TownHallManager.Instance == null || TownHallManager.Instance.IsBuildingUnlocked(buildingData);
        bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.Money >= buildingData.cost;
        bool limitReached = result.placementStatus == PlacementStatus.LimitReached;
        
        // Show/hide overlays
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked);
            
        if (limitReachedOverlay != null)
            limitReachedOverlay.SetActive(limitReached && isUnlocked);
        
        // Update button interactability
        button.interactable = result.canPlace && isUnlocked && canAfford;
        
        // Visual feedback
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            if (!isUnlocked)
                canvasGroup.alpha = 0.3f; // Locked
            else if (limitReached)
                canvasGroup.alpha = 0.6f; // Limit reached
            else if (!canAfford)
                canvasGroup.alpha = 0.5f; // Can't afford
            else
                canvasGroup.alpha = 1f;   // Available
        }
        
        // Update button colors based on status
        ColorBlock colors = button.colors;
        if (!isUnlocked)
        {
            colors.normalColor = Color.gray;
        }
        else if (limitReached)
        {
            colors.normalColor = Color.red;
        }
        else if (!canAfford)
        {
            colors.normalColor = Color.yellow;
        }
        else
        {
            colors.normalColor = Color.white;
        }
        button.colors = colors;
    }
    
    private void OnButtonClick()
    {
        if (buildingPlacer != null && buildingData != null)
        {
            // Check if we can place this building
            BuildingPlacementResult result = buildingPlacer.CheckBuildingPlacement(buildingData);
            
            if (result.canPlace)
            {
                buildingPlacer.SelectBuilding(buildingIndex);
                Debug.Log($"Selected building: {buildingData.name}");
            }
            else
            {
                Debug.Log($"Cannot select building: {result.message}");
                
                // Show appropriate message based on status
                string message = result.placementStatus switch
                {
                    PlacementStatus.NotUnlocked => $"Unlock {buildingData.name} by upgrading your Town Hall!",
                    PlacementStatus.LimitReached => $"Building limit reached! Upgrade your Town Hall to build more.",
                    PlacementStatus.InsufficientFunds => $"Need ${buildingData.cost} to place this building.",
                    _ => result.message
                };
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMessage(message);
                }
            }
        }
    }
    
    private void Update()
    {
        // Update UI in real-time for money changes, etc.
        if (buildingData != null)
        {
            UpdateUI();
        }
    }
}