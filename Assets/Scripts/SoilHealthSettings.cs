using UnityEngine;

[CreateAssetMenu(menuName = "Game/SoilHealthSettings")]
public class SoilHealthSettings : ScriptableObject
{
    [Header("Health Settings")]
    [Range(50f, 200f)]
    public float maxSoilHealth = 100f;
    
    [Header("Degradation")]
    [Range(5f, 30f)]
    public float healthLossPerHarvest = 15f;
    
    [Header("Regeneration")]
    [Range(0.5f, 5f)]
    public float healthRegenerationRate = 2f; // per second when empty
    
    [Range(5f, 30f)]
    public float healthRegenerationDelay = 10f; // seconds before regen starts
    
    [Header("Different Crop Effects")]
    public CropHealthEffect[] cropHealthEffects;
    
    [System.Serializable]
    public class CropHealthEffect
    {
        public CropData crop;
        [Range(0.5f, 2f)]
        public float healthLossMultiplier = 1f; // Some crops might be harder on soil
        [Range(0f, 10f)]
        public float soilRestorationBonus = 0f; // Some crops might restore soil (like legumes)
    }
    
    public float GetHealthLossForCrop(CropData crop)
    {
        foreach (var effect in cropHealthEffects)
        {
            if (effect.crop == crop)
            {
                return healthLossPerHarvest * effect.healthLossMultiplier;
            }
        }
        return healthLossPerHarvest;
    }
    
    public float GetSoilRestorationForCrop(CropData crop)
    {
        foreach (var effect in cropHealthEffects)
        {
            if (effect.crop == crop)
            {
                return effect.soilRestorationBonus;
            }
        }
        return 0f;
    }
}

// Component for the soil health bar UI
public class SoilHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UnityEngine.UI.Slider healthSlider;
    [SerializeField] private UnityEngine.UI.Image fillImage;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    
    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    
    [Header("Animation")]
    [SerializeField] private float updateSpeed = 2f;
    [SerializeField] private bool animateChanges = true;
    
    private float targetValue;
    private float currentDisplayValue;
    
    private void Start()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<UnityEngine.UI.Slider>();
        
        if (fillImage == null)
            fillImage = healthSlider?.fillRect?.GetComponent<UnityEngine.UI.Image>();
    }
    
    private void Update()
    {
        if (animateChanges && Mathf.Abs(currentDisplayValue - targetValue) > 0.1f)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * updateSpeed);
            UpdateDisplay();
        }
    }
    
    public void SetHealth(float current, float max)
    {
        if (healthSlider == null) return;
        
        healthSlider.maxValue = max;
        targetValue = current;
        
        if (!animateChanges)
        {
            currentDisplayValue = current;
            UpdateDisplay();
        }
    }
    
    private void UpdateDisplay()
    {
        if (healthSlider == null) return;
        
        healthSlider.value = currentDisplayValue;
        
        float healthPercent = currentDisplayValue / healthSlider.maxValue;
        
        // Update color
        if (fillImage != null)
        {
            if (healthPercent > 0.6f)
                fillImage.color = healthyColor;
            else if (healthPercent > 0.3f)
                fillImage.color = warningColor;
            else
                fillImage.color = criticalColor;
        }
        
        // Update text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentDisplayValue)}/{Mathf.CeilToInt(healthSlider.maxValue)}";
        }
    }
    
    public void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }
}