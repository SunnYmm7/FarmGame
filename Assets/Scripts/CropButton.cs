using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CropButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image cropIcon;
    [SerializeField] private TextMeshProUGUI cropNameText;
    [SerializeField] private TextMeshProUGUI seedCostText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI growthTimeText;
    
    private CropData cropData;
    private FarmManager farmManager;
    
    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }
    
    public void Setup(CropData crop, FarmManager manager)
    {
        cropData = crop;
        farmManager = manager;
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (cropData == null) return;
        
        // Update icon
        if (cropIcon != null && cropData.cropIcon != null)
        {
            cropIcon.sprite = cropData.cropIcon;
        }
        
        // Update name
        if (cropNameText != null)
        {
            cropNameText.text = cropData.cropName;
        }
        
        // Update seed cost
        if (seedCostText != null)
        {
            seedCostText.text = $"Seeds: Rs{cropData.seedCost}";
        }
        
        // Update sell price
        if (sellPriceText != null)
        {
            sellPriceText.text = $"Sells: Rs{cropData.sellPrice}";
        }
        
        // Update growth time
        if (growthTimeText != null)
        {
            growthTimeText.text = $"Time: {Mathf.CeilToInt(cropData.TotalGrowthTime)}s";
        }
        
        // Check if player can afford this crop
        bool canAfford = ResourceManager.Instance != null && 
                        ResourceManager.Instance.Money >= cropData.seedCost;
        
        button.interactable = canAfford;
        
        // Visual feedback for affordability
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = canAfford ? 1f : 0.5f;
        }
    }
    
    private void OnButtonClick()
    {
        if (farmManager != null && cropData != null)
        {
            farmManager.SelectCrop(cropData);
        }
    }
    
    private void Update()
    {
        // Update affordability in real-time
        if (cropData != null && ResourceManager.Instance != null)
        {
            bool canAfford = ResourceManager.Instance.Money >= cropData.seedCost;
            
            if (button.interactable != canAfford)
            {
                button.interactable = canAfford;
                
                CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = canAfford ? 1f : 0.5f;
                }
            }
        }
    }
}