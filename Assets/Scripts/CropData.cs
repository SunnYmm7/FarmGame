using UnityEngine;

[CreateAssetMenu(menuName = "Game/CropData")]
public class CropData : ScriptableObject
{
    [Header("Basic Info")]
    public string cropName = "Wheat";
    public Sprite cropIcon;
    
    [Header("Growth Settings")]
    public GameObject[] growthStagePrefabs; // Different models for growth stages
    public float[] stageGrowthTimes; // Time in seconds for each stage
    
    [Header("Economics")]
    public int seedCost = 10;
    public int sellPrice = 25;
    
    [Header("Soil Health Effects")]
    [Range(-30f, 20f)]
    public float soilHealthChange = -15f; // Negative = depletes, Positive = restores
    
    [Header("Visuals")]
    public Color readyToHarvestColor = Color.yellow;
    
    public float TotalGrowthTime
    {
        get
        {
            float total = 0f;
            foreach (float time in stageGrowthTimes)
                total += time;
            return total;
        }
    }
    
    public bool RestoresSoil()
    {
        return soilHealthChange > 0;
    }
}