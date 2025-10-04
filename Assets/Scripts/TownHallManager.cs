using UnityEngine;
using System;
using System.Collections.Generic;

public class TownHallManager : MonoBehaviour
{
    public static TownHallManager Instance { get; private set; }
    
    [Header("Town Hall Settings")]
    [SerializeField] private TownHallData[] townHallLevels;
    [SerializeField] private Transform townHallTransform;
    
    [Header("Current Progress")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private PlayerProgress playerProgress;
    
    // Current building counts
    private Dictionary<BuildingData.BuildingType, int> buildingCounts = new Dictionary<BuildingData.BuildingType, int>();
    
    // Events
    public static event Action<int> OnLevelUp;
    public static event Action<TownHallData> OnLevelChanged;
    public static event Action<TaskType, int> OnTaskProgress;
    
    // Properties
    public int CurrentLevel => currentLevel;
    public TownHallData CurrentLevelData => GetLevelData(currentLevel);
    public TownHallData NextLevelData => GetLevelData(currentLevel + 1);
    public PlayerProgress Progress => playerProgress;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializeBuildingCounts();
        InitializeProgress();
    }
    
    private void Start()
    {
        UpdateTownHallVisual();
        OnLevelChanged?.Invoke(CurrentLevelData);
    }
    
    private void OnEnable()
    {
        // Subscribe to game events
        FarmPlot.OnCropHarvested += OnCropHarvested;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        FarmPlot.OnCropHarvested -= OnCropHarvested;
    }
    
    private void InitializeBuildingCounts()
    {
        buildingCounts[BuildingData.BuildingType.Structure] = 0;
        buildingCounts[BuildingData.BuildingType.FarmPlot] = 0;
        buildingCounts[BuildingData.BuildingType.Decoration] = 0;
    }
    
    private void InitializeProgress()
    {
        if (playerProgress == null)
        {
            playerProgress = new PlayerProgress();
        }
    }
    
    private TownHallData GetLevelData(int level)
    {
        if (level <= 0 || level > townHallLevels.Length)
            return null;
        return townHallLevels[level - 1];
    }
    
    // === BUILDING LIMITS ===
    
    public bool CanBuildMore(BuildingData.BuildingType buildingType)
    {
        TownHallData currentData = CurrentLevelData;
        if (currentData == null) return false;
        
        int currentCount = buildingCounts.GetValueOrDefault(buildingType, 0);
        
        return buildingType switch
        {
            BuildingData.BuildingType.FarmPlot => currentCount < currentData.maxFarmPlots,
            BuildingData.BuildingType.Structure => currentCount < currentData.maxStructures,
            BuildingData.BuildingType.Decoration => currentCount < currentData.maxDecorations,
            _ => false
        };
    }
    
    public void OnBuildingPlaced(BuildingData buildingData)
    {
        if (buildingCounts.ContainsKey(buildingData.buildingType))
        {
            buildingCounts[buildingData.buildingType]++;
        }
        
        // Track task progress
        AddTaskProgress(TaskType.BuildStructures, 1);
        
        Debug.Log($"Building placed. {buildingData.buildingType}: {buildingCounts[buildingData.buildingType]}");
    }
    
    public void OnBuildingDestroyed(BuildingData buildingData)
    {
        if (buildingCounts.ContainsKey(buildingData.buildingType))
        {
            buildingCounts[buildingData.buildingType] = Mathf.Max(0, buildingCounts[buildingData.buildingType] - 1);
        }
    }
    
    public int GetBuildingCount(BuildingData.BuildingType buildingType)
    {
        return buildingCounts.GetValueOrDefault(buildingType, 0);
    }
    
    public int GetBuildingLimit(BuildingData.BuildingType buildingType)
    {
        TownHallData currentData = CurrentLevelData;
        if (currentData == null) return 0;
        
        return buildingType switch
        {
            BuildingData.BuildingType.FarmPlot => currentData.maxFarmPlots,
            BuildingData.BuildingType.Structure => currentData.maxStructures,
            BuildingData.BuildingType.Decoration => currentData.maxDecorations,
            _ => 0
        };
    }
    
    // === TASK PROGRESS ===
    
    public void AddTaskProgress(TaskType taskType, int amount = 1)
    {
        switch (taskType)
        {
            case TaskType.HarvestCrops:
                playerProgress.cropsHarvested += amount;
                break;
            case TaskType.BuildStructures:
                playerProgress.structuresBuilt += amount;
                break;
            case TaskType.EarnMoney:
                playerProgress.moneyEarned += amount;
                break;
            case TaskType.PlantSeeds:
                playerProgress.seedsPlanted += amount;
                break;
            case TaskType.CompleteContracts:
                playerProgress.contractsCompleted += amount;
                break;
        }
        
        OnTaskProgress?.Invoke(taskType, amount);
        CheckForLevelUp();
    }
    
    private void OnCropHarvested(int moneyAmount)
    {
        AddTaskProgress(TaskType.HarvestCrops, 1);
        AddTaskProgress(TaskType.EarnMoney, moneyAmount);
    }
    
    // === LEVEL UP SYSTEM ===
    
    public bool CanLevelUp()
    {
        TownHallData nextLevel = NextLevelData;
        if (nextLevel == null) return false;
        
        // Check money requirement
        if (ResourceManager.Instance.Money < nextLevel.upgradeCost)
            return false;
        
        // Check task requirements
        foreach (var requirement in nextLevel.taskRequirements)
        {
            if (!IsTaskRequirementMet(requirement))
                return false;
        }
        
        return true;
    }
    
    private bool IsTaskRequirementMet(TownHallData.TaskRequirement requirement)
    {
        return requirement.taskType switch
        {
            TaskType.HarvestCrops => playerProgress.cropsHarvested >= requirement.requiredAmount,
            TaskType.BuildStructures => playerProgress.structuresBuilt >= requirement.requiredAmount,
            TaskType.EarnMoney => playerProgress.moneyEarned >= requirement.requiredAmount,
            TaskType.PlantSeeds => playerProgress.seedsPlanted >= requirement.requiredAmount,
            TaskType.CompleteContracts => playerProgress.contractsCompleted >= requirement.requiredAmount,
            _ => false
        };
    }
    
    public bool TryLevelUp()
    {
        if (!CanLevelUp()) return false;
        
        TownHallData nextLevel = NextLevelData;
        
        // Spend money
        ResourceManager.Instance.TrySpendMoney(nextLevel.upgradeCost);
        
        // Level up
        currentLevel++;
        UpdateTownHallVisual();
        
        // Fire events
        OnLevelUp?.Invoke(currentLevel);
        OnLevelChanged?.Invoke(CurrentLevelData);
        
        Debug.Log($"Town Hall leveled up to {CurrentLevelData.levelName} (Level {currentLevel})!");
        
        return true;
    }
    
    private void CheckForLevelUp()
    {
        // Auto-notify if requirements are met (but don't auto-upgrade)
        if (CanLevelUp())
        {
            Debug.Log($"Town Hall can be upgraded to {NextLevelData.levelName}!");
            // You could show a notification UI here
        }
    }
    
    private void UpdateTownHallVisual()
    {
        TownHallData currentData = CurrentLevelData;
        if (currentData == null || townHallTransform == null) return;
        
        // Clear existing visual
        foreach (Transform child in townHallTransform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Spawn new visual
        if (currentData.levelPrefab != null)
        {
            GameObject visual = Instantiate(currentData.levelPrefab, townHallTransform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
    }
    
    // === PUBLIC API ===
    
    public float GetCropGrowthSpeedMultiplier()
    {
        return CurrentLevelData?.cropGrowthSpeedMultiplier ?? 1.0f;
    }
    
    public float GetMoneyMultiplier()
    {
        return CurrentLevelData?.moneyMultiplier ?? 1.0f;
    }
    
    public BuildingData[] GetUnlockedBuildings()
    {
        TownHallData currentData = CurrentLevelData;
        return currentData?.unlockedBuildings ?? new BuildingData[0];
    }
    
    public bool IsBuildingUnlocked(BuildingData buildingData)
    {
        BuildingData[] unlocked = GetUnlockedBuildings();
        foreach (var building in unlocked)
        {
            if (building == buildingData) return true;
        }
        
        // Check previous levels too
        for (int i = 1; i < currentLevel; i++)
        {
            TownHallData levelData = GetLevelData(i);
            if (levelData != null && levelData.unlockedBuildings != null)
            {
                foreach (var building in levelData.unlockedBuildings)
                {
                    if (building == buildingData) return true;
                }
            }
        }
        
        return false;
    }
}

// Save/Load data structure
[System.Serializable]
public class PlayerProgress
{
    public int cropsHarvested = 0;
    public int structuresBuilt = 0;
    public int moneyEarned = 0;
    public int seedsPlanted = 0;
    public int contractsCompleted = 0;
}