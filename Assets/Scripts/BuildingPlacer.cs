using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BuildGrid buildGrid;
    [SerializeField] private BuildingData[] buildingOptions;

    [Header("Settings")]
    [SerializeField] private Material previewValidMaterial;
    [SerializeField] private Material previewInvalidMaterial;
    [SerializeField] private Material previewLimitReachedMaterial; // New material for limit reached
    [SerializeField] private LayerMask farmPlotLayer = 1 << 6; // Layer for farm plots

    private Controls _controls;
    private GameObject _previewInstance;
    private Renderer _previewRenderer;
    private BuildingData _selectedBuilding;
    private bool[,] _occupied;

    private void Awake()
    {
        _controls = new Controls();
        if (mainCamera == null) mainCamera = Camera.main;
        _occupied = new bool[buildGrid.Rows, buildGrid.Columns];
    }

    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();

    private void Update()
    {
        if (_selectedBuilding == null || _previewInstance == null) return;

        // Ignore touches/clicks over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Read pointer position from Input System
        Vector2 screenPos = _controls.Main.TouchPosition0.ReadValue<Vector2>();
        if (screenPos == Vector2.zero)
            screenPos = _controls.Main.MousePosition.ReadValue<Vector2>();

        // Raycast from screen to world
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 snapped = buildGrid.GetNearestPointOnGrid(hit.point);
            _previewInstance.transform.position = snapped;

            Vector2Int cell = buildGrid.WorldToCell(snapped);
            BuildingPlacementResult result = CanPlaceBuilding(cell, _selectedBuilding);

            // Set material based on placement result
            _previewRenderer.sharedMaterial = result.placementStatus switch
            {
                PlacementStatus.Valid => previewValidMaterial,
                PlacementStatus.LimitReached => previewLimitReachedMaterial ?? previewInvalidMaterial,
                _ => previewInvalidMaterial
            };

            if (_controls.Main.Place.WasPerformedThisFrame() && result.canPlace)
            {
                PlaceBuilding(cell, snapped);
            }
        }
    }

    public void SelectBuilding(int index)
    {
        if (index < 0 || index >= buildingOptions.Length) return;

        _selectedBuilding = buildingOptions[index];

        // Check if building is unlocked
        if (TownHallManager.Instance != null && !TownHallManager.Instance.IsBuildingUnlocked(_selectedBuilding))
        {
            Debug.Log($"Building {_selectedBuilding.name} is not unlocked yet!");
            return;
        }

        if (_previewInstance != null) Destroy(_previewInstance);

        _previewInstance = Instantiate(_selectedBuilding.prefab);
        _previewRenderer = _previewInstance.GetComponentInChildren<Renderer>();
        _previewInstance.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private BuildingPlacementResult CanPlaceBuilding(Vector2Int originCell, BuildingData buildingData)
    {
        BuildingPlacementResult result = new BuildingPlacementResult();
        
        // Check if building type can be built more
        if (TownHallManager.Instance != null && !TownHallManager.Instance.CanBuildMore(buildingData.buildingType))
        {
            result.canPlace = false;
            result.placementStatus = PlacementStatus.LimitReached;
            result.message = $"Building limit reached for {buildingData.buildingType}!";
            return result;
        }
        
        // Check grid bounds and occupation
        for (int x = 0; x < buildingData.size.x; x++)
        {
            for (int y = 0; y < buildingData.size.y; y++)
            {
                Vector2Int checkCell = originCell + new Vector2Int(x, y);

                if (!buildGrid.IsValidCell(checkCell))
                {
                    result.canPlace = false;
                    result.placementStatus = PlacementStatus.OutOfBounds;
                    result.message = "Cannot place building outside the grid!";
                    return result;
                }
                
                if (_occupied[checkCell.x, checkCell.y])
                {
                    result.canPlace = false;
                    result.placementStatus = PlacementStatus.Occupied;
                    result.message = "Space is already occupied!";
                    return result;
                }
            }
        }
        
        result.canPlace = true;
        result.placementStatus = PlacementStatus.Valid;
        result.message = "Can place building here";
        return result;
    }

    private void ReserveCells(Vector2Int originCell, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cell = originCell + new Vector2Int(x, y);
                _occupied[cell.x, cell.y] = true;
            }
        }
    }

    private void PlaceBuilding(Vector2Int cell, Vector3 worldPos)
    {
        if (!ResourceManager.Instance.TrySpendMoney(_selectedBuilding.cost))
        {
            Debug.Log("Not enough money to place this building!");
            ClearSelection();
            return;
        }

        GameObject placedBuilding = Instantiate(_selectedBuilding.prefab, worldPos, Quaternion.identity);
        
        // If it's a farm plot, set it to the correct layer
        if (_selectedBuilding.isFarmPlot)
        {
            SetLayerRecursively(placedBuilding, 6); // FarmPlot layer
        }
        
        ReserveCells(cell, _selectedBuilding.size);

        // Notify Town Hall Manager
        if (TownHallManager.Instance != null)
        {
            TownHallManager.Instance.OnBuildingPlaced(_selectedBuilding);
        }

        Debug.Log($"Placed {_selectedBuilding.name} - Cost: ${_selectedBuilding.cost}");
        ClearSelection();
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Public method to clear selection (useful for UI)
    public void ClearSelection()
    {
        if (_previewInstance != null)
        {
            Destroy(_previewInstance);
            _previewInstance = null;
        }
        _selectedBuilding = null;
    }
    
    // Get available buildings based on town hall level
    public BuildingData[] GetAvailableBuildings()
    {
        if (TownHallManager.Instance == null) return buildingOptions;
        
        System.Collections.Generic.List<BuildingData> available = new System.Collections.Generic.List<BuildingData>();
        
        foreach (var building in buildingOptions)
        {
            if (TownHallManager.Instance.IsBuildingUnlocked(building))
            {
                available.Add(building);
            }
        }
        
        return available.ToArray();
    }
    
    // Check if a specific building can be placed (for UI feedback)
    public BuildingPlacementResult CheckBuildingPlacement(BuildingData buildingData)
    {
        BuildingPlacementResult result = new BuildingPlacementResult();
        
        // Check if unlocked
        if (TownHallManager.Instance != null && !TownHallManager.Instance.IsBuildingUnlocked(buildingData))
        {
            result.canPlace = false;
            result.placementStatus = PlacementStatus.NotUnlocked;
            result.message = "Building not unlocked yet!";
            return result;
        }
        
        // Check building limits
        if (TownHallManager.Instance != null && !TownHallManager.Instance.CanBuildMore(buildingData.buildingType))
        {
            result.canPlace = false;
            result.placementStatus = PlacementStatus.LimitReached;
            result.message = $"Limit reached: {TownHallManager.Instance.GetBuildingCount(buildingData.buildingType)}/{TownHallManager.Instance.GetBuildingLimit(buildingData.buildingType)}";
            return result;
        }
        
        // Check money
        if (!ResourceManager.Instance.TrySpendMoney(buildingData.cost))
        {
            result.canPlace = false;
            result.placementStatus = PlacementStatus.InsufficientFunds;
            result.message = $"Need ${buildingData.cost}, have ${ResourceManager.Instance.Money}";
            return result;
        }
        
        // Refund the money since this was just a check
        ResourceManager.Instance.AddMoney(buildingData.cost);
        
        result.canPlace = true;
        result.placementStatus = PlacementStatus.Valid;
        result.message = "Ready to place";
        return result;
    }
}

// Helper classes for building placement
[System.Serializable]
public class BuildingPlacementResult
{
    public bool canPlace;
    public PlacementStatus placementStatus;
    public string message;
}

public enum PlacementStatus
{
    Valid,
    OutOfBounds,
    Occupied,
    LimitReached,
    NotUnlocked,
    InsufficientFunds
}