using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmPlotHealthBar : MonoBehaviour
{
    [Header("Health Bar Components")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Health Bar Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Vector2 healthBarSize = new Vector2(200f, 20f);
    [SerializeField] private bool showHealthText = true;
    [SerializeField] private bool showWhenFull = false;
    [SerializeField] private float fadeDistance = 15f;
    
    [Header("Colors")]
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color warningColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color criticalColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Animation")]
    [SerializeField] private bool smoothTransitions = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private AnimationCurve healthChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Private variables
    private Camera playerCamera;
    private FarmPlot parentPlot;
    private CanvasGroup canvasGroup;
    private float targetHealth;
    private float currentDisplayHealth;
    private float maxHealth;
    private bool isInitialized = false;
    
    private void Awake()
    {
        playerCamera = Camera.main;
        parentPlot = GetComponentInParent<FarmPlot>();
        
        if (healthCanvas == null)
            healthCanvas = GetComponent<Canvas>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    
    private void Start()
    {
        SetupHealthBar();
        InitializeHealthBar();
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        UpdateCameraFacing();
        UpdateVisibilityBasedOnDistance();
        
        if (smoothTransitions)
        {
            UpdateSmoothHealthDisplay();
        }
    }
    
    private void SetupHealthBar()
    {
        // Setup canvas
        if (healthCanvas != null)
        {
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.worldCamera = playerCamera;
            
            // Position the health bar
            transform.localPosition = offset;
            
            // Set canvas size
            RectTransform canvasRect = healthCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = healthBarSize;
                canvasRect.localScale = Vector3.one * 0.01f; // Scale down for world space
            }
        }
        
        // Setup slider if it exists
        if (healthSlider != null)
        {
            if (backgroundImage == null)
                backgroundImage = healthSlider.GetComponentInChildren<Image>();
            
            if (fillImage == null)
                fillImage = healthSlider.fillRect?.GetComponent<Image>();
            
            // Set colors
            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;
        }
        
        // Setup health text
        if (healthText != null && !showHealthText)
        {
            healthText.gameObject.SetActive(false);
        }
    }
    
    private void InitializeHealthBar()
    {
        if (parentPlot != null)
        {
            maxHealth = 100f; // You can get this from parentPlot or settings
            targetHealth = parentPlot.GetSoilHealth();
            currentDisplayHealth = targetHealth;
            
            UpdateHealthDisplay();
            UpdateVisibility();
            
            isInitialized = true;
        }
    }
    
    private void UpdateCameraFacing()
    {
        if (playerCamera != null && healthCanvas != null)
        {
            // Make the health bar always face the camera
            Vector3 directionToCamera = playerCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Keep it horizontal
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
    
    private void UpdateVisibilityBasedOnDistance()
    {
        if (playerCamera == null || canvasGroup == null) return;
        
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        float alpha = Mathf.Clamp01((fadeDistance - distance) / fadeDistance);
        
        canvasGroup.alpha = alpha;
    }
    
    private void UpdateSmoothHealthDisplay()
    {
        if (Mathf.Abs(currentDisplayHealth - targetHealth) > 0.1f)
        {
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetHealth, Time.deltaTime * transitionSpeed);
            UpdateHealthSliderValue();
            UpdateHealthColor();
            UpdateHealthText();
        }
    }
    
    public void SetHealth(float current, float maximum)
    {
        maxHealth = maximum;
        targetHealth = current;
        
        if (!smoothTransitions)
        {
            currentDisplayHealth = current;
            UpdateHealthDisplay();
        }
        
        UpdateVisibility();
    }
    
    private void UpdateHealthDisplay()
    {
        UpdateHealthSliderValue();
        UpdateHealthColor();
        UpdateHealthText();
    }
    
    private void UpdateHealthSliderValue()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentDisplayHealth;
        }
    }
    
    private void UpdateHealthColor()
    {
        if (fillImage == null) return;
        
        float healthPercent = currentDisplayHealth / maxHealth;
        Color targetColor;
        
        if (healthPercent > 0.6f)
            targetColor = healthyColor;
        else if (healthPercent > 0.3f)
            targetColor = Color.Lerp(criticalColor, warningColor, (healthPercent - 0.3f) / 0.3f);
        else
            targetColor = criticalColor;
        
        fillImage.color = targetColor;
    }
    
    private void UpdateHealthText()
    {
        if (healthText != null && showHealthText)
        {
            healthText.text = $"{Mathf.CeilToInt(currentDisplayHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
    
    private void UpdateVisibility()
    {
        bool shouldShow = true;
        
        // Hide when health is full (optional)
        if (!showWhenFull && targetHealth >= maxHealth)
        {
            shouldShow = false;
        }
        
        // Hide when plot is destroyed
        if (parentPlot != null && parentPlot.IsDestroyed())
        {
            shouldShow = false;
        }
        
        gameObject.SetActive(shouldShow);
    }
    
    // Animation methods
    public void AnimateHealthChange(float newHealth)
    {
        if (smoothTransitions)
        {
            StartCoroutine(AnimateHealthChangeCoroutine(newHealth));
        }
        else
        {
            SetHealth(newHealth, maxHealth);
        }
    }
    
    private System.Collections.IEnumerator AnimateHealthChangeCoroutine(float newHealth)
    {
        float startHealth = currentDisplayHealth;
        float duration = Mathf.Abs(newHealth - startHealth) / (maxHealth * 0.5f); // Dynamic duration based on change
        duration = Mathf.Clamp(duration, 0.2f, 1f);
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = healthChangeCurve.Evaluate(t);
            
            currentDisplayHealth = Mathf.Lerp(startHealth, newHealth, curveValue);
            UpdateHealthDisplay();
            
            yield return null;
        }
        
        currentDisplayHealth = newHealth;
        targetHealth = newHealth;
        UpdateHealthDisplay();
    }
    
    // Public methods for customization
    public void SetColors(Color healthy, Color warning, Color critical)
    {
        healthyColor = healthy;
        warningColor = warning;
        criticalColor = critical;
        UpdateHealthColor();
    }
    
    public void SetShowWhenFull(bool show)
    {
        showWhenFull = show;
        UpdateVisibility();
    }
    
    public void SetShowHealthText(bool show)
    {
        showHealthText = show;
        if (healthText != null)
        {
            healthText.gameObject.SetActive(show);
        }
    }
    
    // Flash effect for low health warning
    public void FlashLowHealthWarning()
    {
        StartCoroutine(FlashHealthBar());
    }
    
    private System.Collections.IEnumerator FlashHealthBar()
    {
        Color originalColor = fillImage.color;
        Color flashColor = Color.white;
        
        for (int i = 0; i < 3; i++)
        {
            // Flash to white
            fillImage.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            
            // Back to original
            fillImage.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // Cleanup
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}