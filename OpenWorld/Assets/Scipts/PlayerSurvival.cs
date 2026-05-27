using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerSurvival : MonoBehaviour
{
    [Header("Survival Settings")]
    public int gasTanksCollected = 0;
    public int totalGasNeeded = 3;

    [Header("Health System UI")]
    public Slider healthSlider; 
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("POV Damage Flash Configuration")]
    [Tooltip("Drag your DamageFlashCanvas here")]
    public GameObject damageFlashCanvas;
    [Tooltip("How long the blood stays on screen before fading completely")]
    public float flashDuration = 1.5f;
    private Coroutine flashCoroutine;

    [Header("Game Over State Configuration")]
    public GameObject youLoseCanvas;

    [Header("Interactable Objects Route")]
    public InteractableDoor actualDoorComponent;

    [HideInInspector] public bool isInsideHouse = false;
    [HideInInspector] public bool hasWonGame = false; 

    private PlayerInteraction interactionSystem;
    private bool isDead = false;

    void Start()
    {
        interactionSystem = GetComponent<PlayerInteraction>();
        
        if (youLoseCanvas != null) youLoseCanvas.SetActive(false);
        if (damageFlashCanvas != null) damageFlashCanvas.SetActive(false);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void CollectGasTank()
    {
        gasTanksCollected++;
        Debug.Log($"Gas Bottles filled count metrics: {gasTanksCollected}/{totalGasNeeded}");
    }

    public void TakeDamage(float amount)
    {
        if (hasWonGame || isDead) return; 

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Trigger the Blood POV Effect
        if (damageFlashCanvas != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashBloodOverlayRoutine());
        }

        Debug.Log($"Player took {amount} damage! Current Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            TriggerLoseSequence();
        }
    }

    private IEnumerator FlashBloodOverlayRoutine()
    {
        damageFlashCanvas.SetActive(true);
        CanvasGroup canvasGroup = damageFlashCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = damageFlashCanvas.AddComponent<CanvasGroup>();

        // Instantly make it fully visible
        canvasGroup.alpha = 1f;

        // Smoothly fade it out over time
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / flashDuration);
            yield return null;
        }

        damageFlashCanvas.SetActive(false);
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void TriggerLoseSequence()
    {
        if (isDead) return;
        isDead = true;

        if (youLoseCanvas != null)
        {
            youLoseCanvas.SetActive(true);
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            this.enabled = false;
            if (interactionSystem != null) interactionSystem.SetControllersState(false);
        }
    }

    public void EscapeSuccessVictory()
    {
        // Redirects the old function name to your working win sequence
        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.TriggerWinSequence();
        }
    }
}