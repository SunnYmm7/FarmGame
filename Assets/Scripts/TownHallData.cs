using UnityEngine;

[CreateAssetMenu(menuName = "Game/TownHallData")]
public class TownHallData : ScriptableObject
{
    [Header("Level Info")]
    public int level = 1;
    public string levelName = "Cottage";
    public Sprite levelIcon;
    public GameObject levelPrefab; // Visual representation for this level
    
    [Header("Upgrade Requirements")]
    public int upgradeCost = 500;
    public TaskRequirement[] taskRequirements;
    
    [Header("Building Limits")]
    public int maxFarmPlots = 5;
    public int maxStructures = 3;
    public int maxDecorations = 10;
    public int maxSpecialBuildings = 1;
    
    [Header("Unlocked Buildings")]
    public BuildingData[] unlockedBuildings; // Buildings that become available at this level
    
    [Header("Bonuses")]
    public float cropGrowthSpeedMultiplier = 1.0f;
    public float moneyMultiplier = 1.0f;
    public int dailyBonusMoney = 0;
    
    [System.Serializable]
    public class TaskRequirement
    {
        public TaskType taskType;
        public int requiredAmount;
        public string description;
    }
}