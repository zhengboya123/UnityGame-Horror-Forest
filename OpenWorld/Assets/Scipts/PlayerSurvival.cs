using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSurvival : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthBarSlider; 

    [Header("Game UI Canvases")]
    public GameObject gameOverCanvas; 
    public GameObject victoryCanvas; // Add your Escape Win UI panel here!

    [Header("Game Objectives")]
    public int gasTanksCollected = 0;
    public int totalGasNeeded = 3; // Changed to 3 bottles max requirement!
    public InteractableDoor actualDoorComponent; 

    [HideInInspector]
    public bool isInsideHouse = false;
    private bool isGameFinished = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
        }
        
        UpdateHealthUI();

        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
    }

    public void CollectGasTank()
    {
        gasTanksCollected++;
        Debug.LogWarning($"[Gas Poured] Streamed 10s bottle completely! Fuel state: {gasTanksCollected}/{totalGasNeeded}");
    }

    public void EscapeSuccessVictory()
    {
        if (isGameFinished) return;
        isGameFinished = true;

        Debug.LogWarning("VICTORY! You escaped the forest alive!");

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
        }

        // Freeze world space completely
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TakeDamage(float amount)
    {
        if (isGameFinished) return;
        if (isInsideHouse) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            TriggerGameOver();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
    }

    void TriggerGameOver()
    {
        if (isGameFinished) return;
        isGameFinished = true;

        if (gameOverCanvas != null) gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}