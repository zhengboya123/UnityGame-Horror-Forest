using UnityEngine;
using UnityEngine.UI; // Crucial for controlling the visual UI Slider component!
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Vitals")]
    public float playerHealth = 100f;
    public Slider healthBarUI;         // Drag your 'HealthBar' slider object here!
    public GameObject gameOverPanel;   

    [Header("Environment & Progression")]
    public Light sunLight;             
    public int totalTreesCut = 0;
    public int maxTreesForTotalDarkness = 10;

    [Header("Escape Quest Items")]
    public int gasolineCollected = 0;
    public int totalGasolineNeeded = 5;

    private bool isPlayerDead = false;
    private Color originalSunColor;
    private float originalSunIntensity;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (sunLight != null)
        {
            originalSunColor = sunLight.color;
            originalSunIntensity = sunLight.intensity;
        }
        
        // Initialize the health bar boundaries
        if (healthBarUI != null)
        {
            healthBarUI.minValue = 0f;
            healthBarUI.maxValue = 100f;
            healthBarUI.value = playerHealth;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        if (isPlayerDead) return;

        playerHealth -= amount;
        
        // Instantly update the slider fill position on screen
        if (healthBarUI != null)
        {
            healthBarUI.value = Mathf.Max(0, playerHealth);
            
            // OPTIONAL HORROR FLUSH: Dynamically turn the health bar fill red if your health drops low!
            Image fillImage = healthBarUI.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (playerHealth < 30f) fillImage.color = Color.red;
                else fillImage.color = Color.green;
            }
        }

        if (playerHealth <= 0)
        {
            PlayerDie();
        }
    }

    public void TreeWasCut()
    {
        totalTreesCut++;
        Debug.Log("Trees cut down: " + totalTreesCut);
        UpdateSkyDarkness();
    }

    void UpdateSkyDarkness()
    {
        if (sunLight == null) return;

        float percentage = Mathf.Clamp01((float)totalTreesCut / maxTreesForTotalDarkness);
        sunLight.intensity = Mathf.Lerp(originalSunIntensity, 0.02f, percentage);
        sunLight.color = Color.Lerp(originalSunColor, new Color(0.1f, 0.05f, 0.2f), percentage);

        RenderSettings.ambientSkyColor = Color.Lerp(Color.gray, Color.black, percentage);
    }

    public void CollectGasoline()
    {
        gasolineCollected++;
        Debug.Log($"Gasoline found: {gasolineCollected} / {totalGasolineNeeded}");
    }

    void PlayerDie()
    {
        isPlayerDead = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}