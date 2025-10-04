using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TownHallUI : MonoBehaviour
{
    [Header("Town Hall Panel")]
    [SerializeField] private GameObject townHallPanel;
    [SerializeField] private Button townHallButton;
    [SerializeField] private Button closePanelButton;
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private Image levelIcon;
    
    [Header("Upgrade Section")]
    [SerializeField] private GameObject upgradeSection;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private TextMeshProUGUI nextLevelNameText;
    [SerializeField] private GameObject maxLevelReachedText;
    
    [Header("Requirements Display")]
    [SerializeField] private Transform requirementsParent;
    [SerializeField] private GameObject requirementItemPrefab;
    
    [Header("Building Limits Display")]
    [SerializeField] private TextMeshProUGUI farmPlotsText;
    [SerializeField] private TextMeshProUGUI structuresText;
    [SerializeField] private TextMeshProUGUI decorationsText;
    
    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI cropsHarvestedText;
    [SerializeField] private TextMeshProUGUI structuresBuiltText;
    [SerializeField] private TextMeshProUGUI moneyEarnedText;
    [SerializeField] private TextMeshProUGUI seedsPlantedText;
    
    [Header("Bonuses Display")]
    [SerializeField] private Transform bonusesParent;
    [SerializeField] private GameObject bonusItemPrefab;
    
    [Header("Animation")]
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private float levelUpEffectDuration = 3f;
    
    private void Start()
    {
        SetupButtons();
        townHallPanel?.SetActive(false);
        UpdateUI();
    }
    
    private void OnEnable()
    {
        TownHallManager.OnLevelUp += OnLevelUp;
        TownHallManager.OnLevelChanged += OnLevelChanged;
        TownHallManager.OnTaskProgress += OnTaskProgress;
    }
    
    private void OnDisable()
    {
        TownHallManager.OnLevelUp -= OnLevelUp;
        TownHallManager.OnLevelChanged -= OnLevelChanged;
        TownHallManager.OnTaskProgress -= OnTaskProgress;
    }
    
    private void SetupButtons()
    {
        if (townHallButton != null)
            townHallButton.onClick.AddListener(ToggleTownHallPanel);
            
        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(CloseTownHallPanel);
            
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(TryUpgrade);
    }
    
    private void Update()
    {
        // Update UI periodically (could be optimized to only update when needed)
        if (townHallPanel != null && townHallPanel.activeSelf)
        {
            UpdateProgressDisplay();
            UpdateUpgradeButton();
        }
    }
    
    // === PANEL CONTROL ===
    
    public void ToggleTownHallPanel()
    {
        if (townHallPanel != null)
        {
            bool isActive = townHallPanel.activeSelf;
            townHallPanel.SetActive(!isActive);
            
            if (!isActive)
            {
                UpdateUI();
            }
        }
    }
    
    public void CloseTownHallPanel()
    {
        if (townHallPanel != null)
            townHallPanel.SetActive(false);
    }
    
    // === EVENT HANDLERS ===
    
    private void OnLevelUp(int newLevel)
    {
        StartCoroutine(PlayLevelUpEffect());
        UpdateUI();
    }
    
    private void OnLevelChanged(TownHallData newLevelData)
    {
        UpdateUI();
    }
    
    private void OnTaskProgress(TaskType taskType, int amount)
    {
        // Update progress display if panel is open
        if (townHallPanel != null && townHallPanel.activeSelf)
        {
            UpdateProgressDisplay();
        }
    }
    
    // === UI UPDATES ===
    
    private void UpdateUI()
    {
        if (TownHallManager.Instance == null) return;
        
        UpdateLevelDisplay();
        UpdateUpgradeSection();
        UpdateBuildingLimitsDisplay();
        UpdateProgressDisplay();
        UpdateBonusesDisplay();
        UpdateRequirementsDisplay();
    }
    
    private void UpdateLevelDisplay()
    {
        TownHallData currentLevel = TownHallManager.Instance.CurrentLevelData;
        if (currentLevel == null) return;
        
        if (levelNameText != null)
            levelNameText.text = currentLevel.levelName;
            
        if (currentLevelText != null)
            currentLevelText.text = $"Level {TownHallManager.Instance.CurrentLevel}";
            
        if (levelIcon != null && currentLevel.levelIcon != null)
            levelIcon.sprite = currentLevel.levelIcon;
    }
    
    private void UpdateUpgradeSection()
    {
        TownHallData nextLevel = TownHallManager.Instance.NextLevelData;
        bool hasNextLevel = nextLevel != null;
        
        if (upgradeSection != null)
            upgradeSection.SetActive(hasNextLevel);
            
        if (maxLevelReachedText != null)
            maxLevelReachedText.SetActive(!hasNextLevel);
        
        if (hasNextLevel)
        {
            if (upgradeCostText != null)
                upgradeCostText.text = $"Cost: Rs{nextLevel.upgradeCost}";
                
            if (nextLevelNameText != null)
                nextLevelNameText.text = $"Next Level: {nextLevel.levelName}";
        }
    }
    
    private void UpdateUpgradeButton()
    {
        if (upgradeButton == null) return;
        
        bool canUpgrade = TownHallManager.Instance.CanLevelUp();
        upgradeButton.interactable = canUpgrade;
        
        // Visual feedback
        CanvasGroup canvasGroup = upgradeButton.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = canUpgrade ? 1f : 0.5f;
        }
    }
    
    private void UpdateBuildingLimitsDisplay()
    {
        if (TownHallManager.Instance == null) return;
        
        if (farmPlotsText != null)
        {
            int current = TownHallManager.Instance.GetBuildingCount(BuildingData.BuildingType.FarmPlot);
            int max = TownHallManager.Instance.GetBuildingLimit(BuildingData.BuildingType.FarmPlot);
            farmPlotsText.text = $"Farm Plots: {current}/{max}";
        }
        
        if (structuresText != null)
        {
            int current = TownHallManager.Instance.GetBuildingCount(BuildingData.BuildingType.Structure);
            int max = TownHallManager.Instance.GetBuildingLimit(BuildingData.BuildingType.Structure);
            structuresText.text = $"Structures: {current}/{max}";
        }
        
        if (decorationsText != null)
        {
            int current = TownHallManager.Instance.GetBuildingCount(BuildingData.BuildingType.Decoration);
            int max = TownHallManager.Instance.GetBuildingLimit(BuildingData.BuildingType.Decoration);
            decorationsText.text = $"Decorations: {current}/{max}";
        }
    }
    
    private void UpdateProgressDisplay()
    {
        if (TownHallManager.Instance == null) return;
        
        var progress = TownHallManager.Instance.Progress;
        
        if (cropsHarvestedText != null)
            cropsHarvestedText.text = $"Crops Harvested: {progress.cropsHarvested}";
            
        if (structuresBuiltText != null)
            structuresBuiltText.text = $"Structures Built: {progress.structuresBuilt}";
            
        if (moneyEarnedText != null)
            moneyEarnedText.text = $"Money Earned: ${progress.moneyEarned}";
            
        if (seedsPlantedText != null)
            seedsPlantedText.text = $"Seeds Planted: {progress.seedsPlanted}";
    }
    
    private void UpdateBonusesDisplay()
    {
        if (bonusesParent == null || TownHallManager.Instance == null) return;
        
        // Clear existing bonuses
        foreach (Transform child in bonusesParent)
        {
            Destroy(child.gameObject);
        }
        
        TownHallData currentLevel = TownHallManager.Instance.CurrentLevelData;
        if (currentLevel == null) return;
        
        // Create bonus items
        if (currentLevel.cropGrowthSpeedMultiplier != 1.0f)
        {
            CreateBonusItem($"Crop Growth: {currentLevel.cropGrowthSpeedMultiplier:P0}");
        }
        
        if (currentLevel.moneyMultiplier != 1.0f)
        {
            CreateBonusItem($"Money Bonus: {currentLevel.moneyMultiplier:P0}");
        }
        
        if (currentLevel.dailyBonusMoney > 0)
        {
            CreateBonusItem($"Daily Bonus: ${currentLevel.dailyBonusMoney}");
        }
    }
    
    private void UpdateRequirementsDisplay()
    {
        if (requirementsParent == null || TownHallManager.Instance == null) return;
        
        // Clear existing requirements
        foreach (Transform child in requirementsParent)
        {
            Destroy(child.gameObject);
        }
        
        TownHallData nextLevel = TownHallManager.Instance.NextLevelData;
        if (nextLevel == null || nextLevel.taskRequirements == null) return;
        
        // Create requirement items
        foreach (var requirement in nextLevel.taskRequirements)
        {
            CreateRequirementItem(requirement);
        }
    }
    
    private void CreateBonusItem(string bonusText)
    {
        if (bonusItemPrefab == null) return;
        
        GameObject bonusItem = Instantiate(bonusItemPrefab, bonusesParent);
        TextMeshProUGUI text = bonusItem.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = bonusText;
        }
    }
    
    private void CreateRequirementItem(TownHallData.TaskRequirement requirement)
    {
        if (requirementItemPrefab == null) return;
        
        GameObject reqItem = Instantiate(requirementItemPrefab, requirementsParent);
        TextMeshProUGUI text = reqItem.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null)
        {
            int currentProgress = GetCurrentProgress(requirement.taskType);
            bool isComplete = currentProgress >= requirement.requiredAmount;
            
            text.text = $"{requirement.description}: {currentProgress}/{requirement.requiredAmount}";
            text.color = isComplete ? Color.green : Color.white;
        }
    }
    
    private int GetCurrentProgress(TaskType taskType)
    {
        if (TownHallManager.Instance == null) return 0;
        
        var progress = TownHallManager.Instance.Progress;
        return taskType switch
        {
            TaskType.HarvestCrops => progress.cropsHarvested,
            TaskType.BuildStructures => progress.structuresBuilt,
            TaskType.EarnMoney => progress.moneyEarned,
            TaskType.PlantSeeds => progress.seedsPlanted,
            TaskType.CompleteContracts => progress.contractsCompleted,
            _ => 0
        };
    }
    
    // === ACTIONS ===
    
    private void TryUpgrade()
    {
        if (TownHallManager.Instance != null)
        {
            if (TownHallManager.Instance.TryLevelUp())
            {
                Debug.Log("Town Hall upgraded successfully!");
            }
            else
            {
                Debug.Log("Cannot upgrade Town Hall - requirements not met!");
            }
        }
    }
    
    private IEnumerator PlayLevelUpEffect()
    {
        if (levelUpEffectPrefab != null && townHallPanel != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, townHallPanel.transform);
            yield return new WaitForSeconds(levelUpEffectDuration);
            if (effect != null) Destroy(effect);
        }
    }
}