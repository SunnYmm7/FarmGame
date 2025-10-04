using UnityEngine;
using System;

public class FarmPlot : MonoBehaviour
{
    [Header("Plot Settings")]
    [SerializeField] private Transform cropSpawnPoint;
    [SerializeField] private GameObject harvestParticleEffect;
    [SerializeField] private GameObject plotDestroyEffect;
    
    [Header("Visual Feedback")]
    [SerializeField] private Renderer plotRenderer;
    [SerializeField] private Material emptyPlotMaterial;
    [SerializeField] private Material plantedPlotMaterial;
    [SerializeField] private Material readyPlotMaterial;
    [SerializeField] private Material degradedPlotMaterial;
    
    [Header("Soil Health System")]
    [SerializeField] private float maxSoilHealth = 100f;
    [SerializeField] private float healthLossPerHarvest = 15f;
    [SerializeField] private float healthRegenerationRate = 2f; // per second when empty
    [SerializeField] private float healthRegenerationDelay = 10f; // seconds before regen starts
    [SerializeField] private FarmPlotHealthBar healthBar;
    
    // Current crop state
    private CropData currentCrop;
    private GameObject currentCropObject;
    private int currentGrowthStage = 0;
    private float stageTimer = 0f;
    private bool isReadyToHarvest = false;
    private bool isEmpty = true;
    
    // Soil health state
    private float currentSoilHealth;
    private float timeSinceLastHarvest = 0f;
    private bool isDestroyed = false;
    
    // Events
    public static event Action<int> OnCropHarvested; // money earned
    public static event Action<FarmPlot> OnPlotDestroyed; // plot destroyed due to soil health
    public static event Action<float> OnSoilRestored; // soil health restored
    
    private void Start()
    {
        if (cropSpawnPoint == null)
            cropSpawnPoint = transform;
        
        // Initialize soil health
        currentSoilHealth = maxSoilHealth;
        
        // Find or create health bar
        if (healthBar == null)
            healthBar = GetComponentInChildren<FarmPlotHealthBar>();
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentSoilHealth, maxSoilHealth);
        }
        
        UpdateVisuals();
    }
    
    private void Update()
    {
        if (isDestroyed) return;
        
        if (!isEmpty && !isReadyToHarvest)
        {
            GrowCrop();
        }
        
        // Handle soil health regeneration
        if (isEmpty)
        {
            timeSinceLastHarvest += Time.deltaTime;
            
            if (timeSinceLastHarvest >= healthRegenerationDelay && currentSoilHealth < maxSoilHealth)
            {
                currentSoilHealth = Mathf.Min(maxSoilHealth, currentSoilHealth + healthRegenerationRate * Time.deltaTime);
                
                if (healthBar != null)
                {
                    healthBar.SetHealth(currentSoilHealth, maxSoilHealth);
                }
                
                UpdateVisuals();
            }
        }
    }
    
    public bool CanPlantCrop()
    {
        return isEmpty && !isDestroyed && currentSoilHealth > 0;
    }
    
    public bool PlantCrop(CropData cropData)
    {
        if (!CanPlantCrop()) return false;
        
        // Check if player has enough money for seeds
        if (!ResourceManager.Instance.TrySpendMoney(cropData.seedCost))
        {
            Debug.Log($"Not enough money to buy {cropData.cropName} seeds! Need {cropData.seedCost}");
            return false;
        }
        
        currentCrop = cropData;
        currentGrowthStage = 0;
        stageTimer = 0f;
        isEmpty = false;
        isReadyToHarvest = false;
        timeSinceLastHarvest = 0f; // Reset regeneration timer
        
        SpawnCropStage();
        UpdateVisuals();
        
        Debug.Log($"Planted {cropData.cropName} for {cropData.seedCost} coins");
        return true;
    }
    
    public void HarvestCrop()
    {
        if (!isReadyToHarvest || isDestroyed) return;
        
        // Apply town hall money multiplier
        float moneyMultiplier = TownHallManager.Instance != null ? TownHallManager.Instance.GetMoneyMultiplier() : 1.0f;
        int finalMoney = Mathf.RoundToInt(currentCrop.sellPrice * moneyMultiplier);
        
        // Give money to player
        ResourceManager.Instance.AddMoney(finalMoney);
        OnCropHarvested?.Invoke(finalMoney);
        
        // Handle soil health change - use crop's specific value or fallback to default
        float healthChange = currentCrop.soilHealthChange != 0 ? currentCrop.soilHealthChange : -healthLossPerHarvest;
        
        float previousHealth = currentSoilHealth;
        currentSoilHealth = Mathf.Clamp(currentSoilHealth + healthChange, 0, maxSoilHealth);
        timeSinceLastHarvest = 0f; // Reset regeneration timer
        
        // Log the change
        if (healthChange > 0)
        {
            Debug.Log($"Harvested {currentCrop.cropName} for {finalMoney} coins! Soil restored by {healthChange} points!");
            OnSoilRestored?.Invoke(healthChange);
        }
        else
        {
            Debug.Log($"Harvested {currentCrop.cropName} for {finalMoney} coins! Soil depleted by {Mathf.Abs(healthChange)} points.");
        }
        
        Debug.Log($"Soil health: {currentSoilHealth:F1}/{maxSoilHealth}");
        
        // Spawn harvest effect
        if (harvestParticleEffect != null)
        {
            GameObject effect = Instantiate(harvestParticleEffect, cropSpawnPoint.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Clear the plot
        ClearPlot();
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentSoilHealth, maxSoilHealth);
        }
        
        // Check if soil health is depleted (only destroy if health reaches 0)
        if (currentSoilHealth <= 0)
        {
            DestroyPlot();
        }
    }
    
    private void GrowCrop()
    {
        // Apply town hall growth speed multiplier
        float growthMultiplier = TownHallManager.Instance != null ? TownHallManager.Instance.GetCropGrowthSpeedMultiplier() : 1.0f;
        stageTimer += Time.deltaTime * growthMultiplier;
        
        if (currentGrowthStage < currentCrop.stageGrowthTimes.Length)
        {
            if (stageTimer >= currentCrop.stageGrowthTimes[currentGrowthStage])
            {
                stageTimer = 0f;
                currentGrowthStage++;
                
                if (currentGrowthStage < currentCrop.growthStagePrefabs.Length)
                {
                    SpawnCropStage();
                }
                else
                {
                    // Crop is fully grown
                    isReadyToHarvest = true;
                    UpdateVisuals();
                    Debug.Log($"{currentCrop.cropName} is ready to harvest!");
                }
            }
        }
    }
    
    private void SpawnCropStage()
    {
        if (currentCropObject != null)
        {
            DestroyImmediate(currentCropObject);
        }
        
        if (currentGrowthStage < currentCrop.growthStagePrefabs.Length && 
            currentCrop.growthStagePrefabs[currentGrowthStage] != null)
        {
            currentCropObject = Instantiate(currentCrop.growthStagePrefabs[currentGrowthStage], 
                                          cropSpawnPoint.position, 
                                          cropSpawnPoint.rotation, 
                                          transform);
        }
    }
    
    private void ClearPlot()
    {
        if (currentCropObject != null)
        {
            DestroyImmediate(currentCropObject);
            currentCropObject = null;
        }
        
        currentCrop = null;
        currentGrowthStage = 0;
        stageTimer = 0f;
        isReadyToHarvest = false;
        isEmpty = true;
        
        UpdateVisuals();
    }
    
    private void DestroyPlot()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        // Spawn destruction effect
        if (plotDestroyEffect != null)
        {
            GameObject effect = Instantiate(plotDestroyEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // Notify systems
        OnPlotDestroyed?.Invoke(this);
        
        Debug.Log("Farm plot destroyed due to soil depletion!");
        
        // Destroy the plot after a short delay to show effects
        Destroy(gameObject, 1f);
    }
    
    private void UpdateVisuals()
    {
        if (plotRenderer == null) return;
        
        if (isDestroyed)
        {
            // Plot is being destroyed, keep current material
            return;
        }
        
        float healthPercent = currentSoilHealth / maxSoilHealth;
        
        if (isEmpty)
        {
            if (healthPercent < 0.3f)
                plotRenderer.material = degradedPlotMaterial ?? emptyPlotMaterial;
            else
                plotRenderer.material = emptyPlotMaterial;
        }
        else if (isReadyToHarvest)
        {
            plotRenderer.material = readyPlotMaterial;
        }
        else
        {
            plotRenderer.material = plantedPlotMaterial;
        }
    }
    
    // For UI display
    public string GetPlotStatus()
    {
        if (isDestroyed) return "Destroyed";
        if (isEmpty) 
        {
            float healthPercent = (currentSoilHealth / maxSoilHealth) * 100f;
            return $"Empty (Health: {healthPercent:F0}%)";
        }
        if (isReadyToHarvest) return $"{currentCrop.cropName} - Ready!";
        
        float remainingTime = 0f;
        for (int i = currentGrowthStage; i < currentCrop.stageGrowthTimes.Length; i++)
        {
            if (i == currentGrowthStage)
                remainingTime += currentCrop.stageGrowthTimes[i] - stageTimer;
            else
                remainingTime += currentCrop.stageGrowthTimes[i];
        }
        
        // Account for growth speed multiplier
        float growthMultiplier = TownHallManager.Instance != null ? TownHallManager.Instance.GetCropGrowthSpeedMultiplier() : 1.0f;
        remainingTime /= growthMultiplier;
        
        return $"{currentCrop.cropName} - {Mathf.CeilToInt(remainingTime)}s";
    }
    
    // Public getters
    public bool IsEmpty() => isEmpty && !isDestroyed;
    public bool IsReadyToHarvest() => isReadyToHarvest && !isDestroyed;
    public bool IsDestroyed() => isDestroyed;
    public CropData GetCurrentCrop() => currentCrop;
    public float GetSoilHealth() => currentSoilHealth;
    public float GetSoilHealthPercent() => (currentSoilHealth / maxSoilHealth) * 100f;
    
    // Methods for soil health management (could be used for fertilizers, etc.)
    public void RestoreSoilHealth(float amount)
    {
        if (!isDestroyed)
        {
            currentSoilHealth = Mathf.Min(maxSoilHealth, currentSoilHealth + amount);
            
            if (healthBar != null)
            {
                healthBar.SetHealth(currentSoilHealth, maxSoilHealth);
            }
            
            UpdateVisuals();
        }
    }
    
    public void SetSoilHealth(float health)
    {
        if (!isDestroyed)
        {
            currentSoilHealth = Mathf.Clamp(health, 0, maxSoilHealth);
            
            if (healthBar != null)
            {
                healthBar.SetHealth(currentSoilHealth, maxSoilHealth);
            }
            
            UpdateVisuals();
        }
    }
}