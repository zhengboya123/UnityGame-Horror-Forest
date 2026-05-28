using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance;

    [Header("UI Display Assets")]
    public TextMeshProUGUI dayAndMissionText;
    public Image screenFadeOverlay;

    [Header("Progression Balancing Settings")]
    public int currentDayNumber = 1;
    public int currentLogsStoredCount = 0;
    public int currentSavagesKilledCount = 0;

    [Header("Visual Warehouse Integration")]
    [Tooltip("Drag non-interactable log objects from your scene here.")]
    public GameObject[] warehouseVisualLogs;

    // These variables hold the actual dynamic math targets computed at runtime
    [HideInInspector] public int activeLogsRequired;
    [HideInInspector] public int activeSavagesKillGoal;
    [HideInInspector] public int activeSavageMaxSimultaneous;

    [HideInInspector] public bool isNightTimeActive = false;
    private PlayerInteraction playerInteraction;
    private PlayerSurvival playerSurvival;

    // Persistent tracking list to preserve exact ground instances across days
    [HideInInspector] public List<GameObject> trackedGasBottles = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        playerInteraction = Object.FindFirstObjectByType<PlayerInteraction>();
        playerSurvival = Object.FindFirstObjectByType<PlayerSurvival>();

        if (screenFadeOverlay != null) screenFadeOverlay.gameObject.SetActive(false);

        CalculateDayRequirements();
        UpdateTopLeftMissionUI();
        UpdateWarehouseVisualDisplay();
    }

    public void CalculateDayRequirements()
    {
        activeLogsRequired = 10 + ((currentDayNumber - 1) * 5);
        activeSavagesKillGoal = 5 + ((currentDayNumber - 1) * 5);
        activeSavageMaxSimultaneous = activeSavagesKillGoal;

        Debug.Log($"[DIFFICULTY] Day {currentDayNumber} Initialized: Logs Needed = {activeLogsRequired}, Kills Needed = {activeSavagesKillGoal}, Spawn Max = {activeSavageMaxSimultaneous}");
    }

    public void UpdateTopLeftMissionUI()
    {
        if (dayAndMissionText == null) return;

        if (!isNightTimeActive)
        {
            dayAndMissionText.text = $"<b>DAY {currentDayNumber}</b>\n<size=80%>Chop trees to advance time to night.</size>";
        }
        else
        {
            string logStatus = currentLogsStoredCount >= activeLogsRequired ? "<color=green>DONE</color>" : $"{currentLogsStoredCount}/{activeLogsRequired}";
            string killStatus = currentSavagesKilledCount >= activeSavagesKillGoal ? "<color=green>DONE</color>" : $"{currentSavagesKilledCount}/{activeSavagesKillGoal}";
            
            dayAndMissionText.text = $"<b>DAY {currentDayNumber} (NIGHT)</b>\n" +
                                     $"• Store Logs: {logStatus}\n" +
                                     $"• Defeat Savages: {currentSavagesKilledCount}/{activeSavagesKillGoal} ({killStatus})";
        }
    }

    public void UpdateWarehouseVisualDisplay()
    {
        if (warehouseVisualLogs == null || warehouseVisualLogs.Length == 0) return;

        for (int i = 0; i < warehouseVisualLogs.Length; i++)
        {
            if (warehouseVisualLogs[i] != null)
            {
                warehouseVisualLogs[i].SetActive(i < currentLogsStoredCount);
            }
        }
    }

    public bool CanPlayerSleepOvernight()
    {
        return isNightTimeActive && 
               currentLogsStoredCount >= activeLogsRequired && 
               currentSavagesKilledCount >= activeSavagesKillGoal;
    }

    public string GetMissingRequirementsString()
    {
        string missingText = "Missing: ";
        if (currentLogsStoredCount < activeLogsRequired) 
            missingText += $"{activeLogsRequired - currentLogsStoredCount} logs ";
        if (currentSavagesKilledCount < activeSavagesKillGoal) 
            missingText += $"{activeSavagesKillGoal - currentSavagesKilledCount} kills ";
        return missingText;
    }

    public void OnSavageKilled()
    {
        if (!isNightTimeActive) return;
        
        currentSavagesKilledCount++;
        UpdateTopLeftMissionUI();
        
        if (playerInteraction != null)
        {
            playerInteraction.MaintainSavagePopulationCount();
        }
    }

    public void AdvanceToNextDaySequence(Transform bedSleepTransform)
    {
        StartCoroutine(SleepOvernightRoutine(bedSleepTransform));
    }

    private IEnumerator SleepOvernightRoutine(Transform targetBedPos)
    {
        if (playerInteraction != null) playerInteraction.SetControllersState(false);

        if (playerSurvival != null && targetBedPos != null)
        {
            playerSurvival.transform.position = targetBedPos.position;
            playerSurvival.transform.rotation = targetBedPos.rotation;
        }

        if (screenFadeOverlay != null)
        {
            screenFadeOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime * 2f;
                screenFadeOverlay.color = new Color(0, 0, 0, Mathf.Min(elapsed, 1f));
                yield return null;
            }
        }

        WipeAllActiveSavages();
        ToggleGasBottlesVisibility(false); // Hides all current clones when moving to next morning

        yield return new WaitForSecondsRealtime(5f);

        if (playerSurvival != null)
        {
            playerSurvival.RestoreFullHealth();
        }

        currentDayNumber++;
        currentLogsStoredCount = 0;
        currentSavagesKilledCount = 0;
        isNightTimeActive = false;

        CalculateDayRequirements();
        UpdateTopLeftMissionUI();
        UpdateWarehouseVisualDisplay(); 

        if (playerInteraction != null) playerInteraction.ResetEnvironmentToMorning();

        if (screenFadeOverlay != null)
        {
            float elapsed = 1f;
            while (elapsed > 0f)
            {
                elapsed -= Time.unscaledDeltaTime * 2f;
                screenFadeOverlay.color = new Color(0, 0, 0, Mathf.Max(elapsed, 0f));
                yield return null;
            }
            screenFadeOverlay.gameObject.SetActive(false);
        }

        if (playerInteraction != null) playerInteraction.SetControllersState(true);
    }

    private void WipeAllActiveSavages()
    {
        SavageAI[] enemies = Object.FindObjectsByType<SavageAI>(FindObjectsSortMode.None);
        foreach (SavageAI enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    public void ToggleGasBottlesVisibility(bool visible)
    {
        // Remove objects completely picked up/destroyed by player before toggling
        trackedGasBottles.RemoveAll(item => item == null);

        // Toggle the entire parent GameObject state cleanly so it handles physics & display safely
        foreach (GameObject gasObj in trackedGasBottles)
        {
            if (gasObj != null)
            {
                gasObj.SetActive(visible);
            }
        }
    }
}