using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FarmManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CropData[] availableCrops;
    [SerializeField] private SoilHealthSettings soilHealthSettings;
    
    [Header("UI References")]
    [SerializeField] private GameObject cropSelectionPanel;
    [SerializeField] private Transform cropButtonParent;
    [SerializeField] private GameObject cropButtonPrefab;
    
    [Header("Plot Management")]
    [SerializeField] private GameObject farmPlotPrefab; // Prefab to respawn destroyed plots
    [SerializeField] private int maxPlotRespawns = 3; // How many times a plot can be respawned(as many coffes i can drink at this point    )
    
    [Header("Settings")]
    [SerializeField] private LayerMask farmPlotLayer = 1 << 6; // Assume farm plots are on layer 6
    
    [Header("Notifications")]
    [SerializeField] private bool showSoilHealthWarnings = true;
    [SerializeField] private float lowHealthWarningThreshold = 30f;
    
    private Controls _controls;
    private CropData selectedCrop;
    private FarmPlot selectedPlot;
    private List<GameObject> cropButtons = new List<GameObject>();
    private Dictionary<Vector3, int> plotRespawnCount = new Dictionary<Vector3, int>();
    
    private void Awake()
    {
        _controls = new Controls();
        if (mainCamera == null) mainCamera = Camera.main;
        
        SetupCropButtons();
    }
    
    private void OnEnable()
    {
        _controls.Enable();
        FarmPlot.OnCropHarvested += OnCropHarvested;
        FarmPlot.OnPlotDestroyed += OnPlotDestroyed;
    }
    
    private void OnDisable()
    {
        _controls.Disable();
        FarmPlot.OnCropHarvested -= OnCropHarvested;
        FarmPlot.OnPlotDestroyed -= OnPlotDestroyed;
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        // Ignore touches/clicks over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        
        if (_controls.Main.Place.WasPerformedThisFrame())
        {
            Vector2 screenPos = _controls.Main.TouchPosition0.ReadValue<Vector2>();
            if (screenPos == Vector2.zero)
                screenPos = _controls.Main.MousePosition.ReadValue<Vector2>();
            
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, farmPlotLayer))
            {
                FarmPlot plot = hit.collider.GetComponent<FarmPlot>();
                if (plot != null && !plot.IsDestroyed())
                {
                    InteractWithPlot(plot);
                }
            }
        }
    }
    
    private void InteractWithPlot(FarmPlot plot)
    {
        if (plot.IsReadyToHarvest())
        {
            // Check soil health before harvesting and warn player
            if (showSoilHealthWarnings && plot.GetSoilHealthPercent() <= lowHealthWarningThreshold)
            {
                float healthAfterHarvest = plot.GetSoilHealth() - (soilHealthSettings?.healthLossPerHarvest ?? 15f);
                if (healthAfterHarvest <= 0)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowMessage($"Warning: This harvest will destroy the farm plot due to soil depletion!", 4f);
                    }
                }
                else
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowMessage($"Warning: Low soil health! Consider letting it rest after harvest.", 3f);
                    }
                }
            }
            
            // Harvest the crop
            plot.HarvestCrop();
        }
        else if (plot.IsEmpty())
        {
            // Check if soil is too degraded to plant
            if (plot.GetSoilHealth() <= 0)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMessage("This soil is too degraded to plant crops!", 2f);
                }
                return;
            }
            
            // Show crop selection or plant if crop is selected
            selectedPlot = plot;
            
            if (selectedCrop != null)
            {
                if (plot.PlantCrop(selectedCrop))
                {
                    // Track planting for town hall progress
                    if (TownHallManager.Instance != null)
                    {
                        TownHallManager.Instance.AddTaskProgress(TaskType.PlantSeeds, 1);
                    }
                    
                    selectedCrop = null; // Clear selection after planting
                    HideCropSelection();
                }
            }
            else
            {
                ShowCropSelection();
            }
        }
        else
        {
            // Plot has growing crop, show status
            string status = plot.GetPlotStatus();
            float soilHealth = plot.GetSoilHealthPercent();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage($"{status}\nSoil Health: {soilHealth:F0}%", 2f);
            }
            
            Debug.Log($"Plot status: {status}, Soil Health: {soilHealth:F1}%");
        }
    }
    
    private void SetupCropButtons()
    {
        if (cropButtonPrefab == null || cropButtonParent == null) return;
        
        foreach (GameObject button in cropButtons)
        {
            if (button != null) Destroy(button);
        }
        cropButtons.Clear();
        
        for (int i = 0; i < availableCrops.Length; i++)
        {
            CropData crop = availableCrops[i];
            GameObject button = Instantiate(cropButtonPrefab, cropButtonParent);
            
            // Setup button (Gng I will need to modify this based on your button prefab structure)
            CropButton cropButton = button.GetComponent<CropButton>();
            if (cropButton != null)
            {
                cropButton.Setup(crop, this);
            }
            
            cropButtons.Add(button);
        }
    }
    
    public void SelectCrop(CropData crop)
    {
        selectedCrop = crop;
        Debug.Log($"Selected crop: {crop.cropName}");
        
        if (selectedPlot != null && selectedPlot.IsEmpty() && !selectedPlot.IsDestroyed())
        {
            if (selectedPlot.PlantCrop(selectedCrop))
            {
                // Track planting for town hall progress
                if (TownHallManager.Instance != null)
                {
                    TownHallManager.Instance.AddTaskProgress(TaskType.PlantSeeds, 1);
                }
                
                selectedCrop = null;
                selectedPlot = null;
            }
        }
        
        HideCropSelection();
    }
    
    private void ShowCropSelection()
    {
        if (cropSelectionPanel != null)
        {
            cropSelectionPanel.SetActive(true);
        }
    }
    
    private void HideCropSelection()
    {
        if (cropSelectionPanel != null)
        {
            cropSelectionPanel.SetActive(false);
        }
        selectedPlot = null;
    }
    
    private void OnCropHarvested(int moneyEarned)
    {
        Debug.Log($"Earned {moneyEarned} coins from harvest!");
        // Town hall progress tracking is handled in TownHallManager via FarmPlot.OnCropHarvested event
    }
    
    private void OnPlotDestroyed(FarmPlot destroyedPlot)
    {
        Vector3 plotPosition = destroyedPlot.transform.position;
        
        // Show notification
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("Farm plot destroyed due to soil depletion! Consider crop rotation in the future.", 5f);
        }
        
        // Update building count in TownHallManager
        if (TownHallManager.Instance != null)
        {
            // Create a temporary BuildingData to represent the destroyed farm plot
            // This would need to be handled based on how your BuildingData is structured
            // You might want to add a method to TownHallManager for handling destroyed buildings
        }
        
        // Optionally respawn the plot after some time or conditions
        if (farmPlotPrefab != null)
        {
            int respawnCount = plotRespawnCount.GetValueOrDefault(plotPosition, 0);
            if (respawnCount < maxPlotRespawns)
            {
                StartCoroutine(RespawnPlotAfterDelay(plotPosition, 30f)); // 30 second delay
                plotRespawnCount[plotPosition] = respawnCount + 1;
            }
            else
            {
                Debug.Log($"Plot at {plotPosition} has been destroyed too many times and cannot be respawned.");
            }
        }
    }
    
    private System.Collections.IEnumerator RespawnPlotAfterDelay(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("New fertile soil has been discovered!", 3f);
        }
        
        // Spawn new plot with full soil health
        GameObject newPlot = Instantiate(farmPlotPrefab, position, Quaternion.identity);
        
        // Set it to the correct layer
        SetLayerRecursively(newPlot, 6); // FarmPlot layer
        
        Debug.Log($"Respawned farm plot at {position}");
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // Public API for UI
    public CropData[] GetAvailableCrops() => availableCrops;
    public CropData GetSelectedCrop() => selectedCrop;
    public void ClearCropSelection() => selectedCrop = null;
    
    // Soil health management methods
    public void RestoreAllSoilHealth(float amount)
    {
        FarmPlot[] allPlots = FindObjectsOfType<FarmPlot>();
        foreach (var plot in allPlots)
        {
            if (!plot.IsDestroyed())
            {
                plot.RestoreSoilHealth(amount);
            }
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage($"All farm plots restored by {amount} health!", 3f);
        }
    }
    
    public int GetHealthyPlotsCount()
    {
        FarmPlot[] allPlots = FindObjectsOfType<FarmPlot>();
        int count = 0;
        foreach (var plot in allPlots)
        {
            if (!plot.IsDestroyed() && plot.GetSoilHealthPercent() > 50f)
            {
                count++;
            }
        }
        return count;
    }
    
    public int GetDegradedPlotsCount()
    {
        FarmPlot[] allPlots = FindObjectsOfType<FarmPlot>();
        int count = 0;
        foreach (var plot in allPlots)
        {
            if (!plot.IsDestroyed() && plot.GetSoilHealthPercent() <= 30f)
            {
                count++;
            }
        }
        return count;
    }
}