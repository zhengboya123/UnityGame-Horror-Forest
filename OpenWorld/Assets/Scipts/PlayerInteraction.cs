using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform cameraTransform;
    public float interactionDistance = 4.5f;
    public TextMeshProUGUI hintTextElement;

    [Header("Axe Arc Combat Cleave")]
    public float attackRadius = 4.0f;
    public float attackAngle = 180f;
    private float nextAttackAllowedTime = 0f;
    private const float singleSwingFreezeDuration = 0.8f;

    [Header("Horror Visual Effects")]
    public GameObject bloodSplashPrefab; // Drag your blood particle prefab here
    [Header("Axe Animation Hook")]
    public Animator axeAnimator; 

    [Header("UI Progress Assets")]
    public Image chopProgressCircle;
    
    [Header("Global Boat Fueling Slider (0-300)")]
    public Slider fuelProgressBarSlider; 

    [Header("Win Game State Configuration")]
    public GameObject youWinCanvas; 

    [Header("Item Prefabs to Drop")]
    public GameObject axePrefab;
    public GameObject logPrefab;
    public GameObject gasBottlePrefab;

    [Header("Gas Bottle Spawning Settings")]
    public int totalGasBottlesToSpawn = 5; 
    public Transform[] gasBottleSpawnPoints; 

    [Header("Day/Night & Spawning System")]
    public Light directionalLight;
    public float timeAdvancePerChop = 15.0f;
    public GameObject abnormalSpeciesPrefab;
    public Transform[] transformSpawnPoints; 
    
    [Header("Savage Trigger Config")]
    public int treesRequiredToSpawnSavage = 20;

    [Header("Game Progression Stats")]
    public int totalTreesChopped = 0;

    private InventorySystem inventory;
    private PlayerSurvival playerSurvival;
    private Terrain currentTerrain;
    
    private float treeChopTimer = 0f;
    private const float totalTreeChopDuration = 5.0f;

    [Header("Boat Fuel Progress (0-300)")]
    public float currentBoatFuelAmount = 0f; 

    private bool isProcessingTreeCut = false;
    private TreeInstance[] originalTreesBackup;
    private HashSet<int> choppedTreeIndices = new HashSet<int>();
    
    private bool isGameOverSequenceActive = false; 
    private List<GameObject> aliveSavagesList = new List<GameObject>();

    // OPTIMIZATION ADDITION: Stores only the index numbers that are valid trees
    private HashSet<int> validTreePrototypeIndexes = new HashSet<int>();

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        playerSurvival = GetComponent<PlayerSurvival>();

        currentTerrain = Terrain.activeTerrain;
        if (currentTerrain != null && currentTerrain.terrainData != null)
        {
            originalTreesBackup = (TreeInstance[])currentTerrain.terrainData.treeInstances.Clone();
            
            // OPTIMIZATION: Pre-calculate tree indexes once at boot to save thousands of CPU frames
            for (int i = 0; i < currentTerrain.terrainData.treePrototypes.Length; i++)
            {
                GameObject prefab = currentTerrain.terrainData.treePrototypes[i].prefab;
                if (prefab != null && prefab.name.Contains("Tree"))
                {
                    validTreePrototypeIndexes.Add(i);
                }
            }
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        if (fuelProgressBarSlider != null) fuelProgressBarSlider.gameObject.SetActive(false);
        if (youWinCanvas != null) youWinCanvas.SetActive(false); 

        ResetProgressVisuals();
        ClearPrompt();
    }

    void Update()
    {
        ManageInteractionRaycast();
        HandleAbandonItemInput();
        CheckInventoryLogDepletionGuard();
    }

    void SpawnGasBottlesRandomly()
    {
        if (gasBottlePrefab == null || gasBottleSpawnPoints == null || gasBottleSpawnPoints.Length == 0) return;
        int targetSpawnCount = Mathf.Clamp(totalGasBottlesToSpawn, 0, gasBottleSpawnPoints.Length);

        List<Transform> temporaryPool = new List<Transform>(gasBottleSpawnPoints);
        for (int i = temporaryPool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            Transform temp = temporaryPool[i];
            temporaryPool[i] = temporaryPool[rnd];
            temporaryPool[rnd] = temp;
        }

        for (int i = 0; i < targetSpawnCount; i++)
        {
            if (temporaryPool[i] != null)
            {
                GameObject spawnedCan = Instantiate(gasBottlePrefab, temporaryPool[i].position, temporaryPool[i].rotation);
                GasBottleItem bottleScript = spawnedCan.GetComponent<GasBottleItem>();
                if (bottleScript != null) bottleScript.structuralFuelCapacity = 10f;
            }
        }
    }

    void ManageInteractionRaycast()
    {
        string frameworkText = "";
        bool foundInteractiveTarget = false;
        bool lookingAtTreeValue = false;
        bool lookingAtBoatThisFrame = false;

        if (cameraTransform == null) return;

        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Player", "UI", "Ignore Raycast");
        bool raycastHitAnything = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance, layerMask);
        
        ObjectiveInteractable objective = null;

        if (raycastHitAnything)
        {
            Debug.DrawLine(cameraTransform.position, hit.point, Color.green);
            
            objective = hit.collider.GetComponentInParent<ObjectiveInteractable>()
                ?? hit.collider.GetComponent<ObjectiveInteractable>();

            bool isLookingAtBoat = (objective != null && objective.objectType == ObjectiveInteractable.InteractionObjectType.EscapeBoat) 
                                   || hit.collider.CompareTag("EscapeBoat") 
                                   || hit.collider.transform.root.CompareTag("EscapeBoat");

            if (isLookingAtBoat)
            {
                lookingAtBoatThisFrame = true;
                
                if (objective == null)
                {
                    objective = hit.collider.GetComponentInParent<ObjectiveInteractable>() 
                                ?? hit.collider.transform.root.GetComponentInChildren<ObjectiveInteractable>();
                }

                foundInteractiveTarget = true;
                if (playerSurvival != null)
                {
                    if (fuelProgressBarSlider != null && !fuelProgressBarSlider.gameObject.activeSelf) 
                    {
                        fuelProgressBarSlider.gameObject.SetActive(true);
                    }

                    if (fuelProgressBarSlider != null)
                    {
                        fuelProgressBarSlider.value = Mathf.Min(currentBoatFuelAmount, 300f);
                    }

                    if (currentBoatFuelAmount >= 300f)
                    {
                        frameworkText = "Press [E] to escape the forest! (WIN)";
                        
                        if (Input.GetKeyDown(KeyCode.E)) 
                        {
                            TriggerWinSequence();
                        }
                    }
                    else
                    {
                        bool holdingGas = (inventory != null && inventory.currentSlot == 3 && inventory.unlockedSlots[3]);

                        if (holdingGas && inventory.gasBottleFuelCapacity > 0f)
                        {
                            frameworkText = $"Hold [Left Click] to pour Gasoline | Fuel left in tank: {inventory.gasBottleFuelCapacity:F1}s";
                            
                            if (Input.GetMouseButton(0))
                            {
                                float fuelToPour = 10f * Time.deltaTime;

                                if (currentBoatFuelAmount + fuelToPour > 300f)
                                {
                                    fuelToPour = 300f - currentBoatFuelAmount;
                                }

                                if (fuelToPour > 0f)
                                {
                                    currentBoatFuelAmount += fuelToPour;
                                    inventory.gasBottleFuelCapacity -= Time.deltaTime;
                                }

                                if (inventory.gasBottleFuelCapacity <= 0f)
                                {
                                    inventory.gasBottleFuelCapacity = 0f;
                                    playerSurvival.CollectGasTank(); 
                                    if (inventory != null) { inventory.EmptyGasCan(); } 
                                }
                            }
                        }
                        else if (holdingGas && inventory.gasBottleFuelCapacity <= 0f)
                        {
                            frameworkText = "This Gas Bottle is empty!";
                        }
                        else
                        {
                            frameworkText = "Boat needs Fuel. Equip Gas Bottle from Slot 4!";
                        }
                    }
                }
            }

            // 2. CABIN DOOR CHECK
            InteractableDoor houseDoor = hit.collider.GetComponentInParent<InteractableDoor>()
                ?? hit.collider.GetComponent<InteractableDoor>();

            if (houseDoor == null && (hit.collider.name == "DoorRepairAnchor" || hit.collider.CompareTag("DoorFrame")))
            {
                if (playerSurvival != null) houseDoor = playerSurvival.actualDoorComponent;
            }

            if (houseDoor != null && !foundInteractiveTarget)
            {
                foundInteractiveTarget = true;
                frameworkText = houseDoor.isOpen ? "Press [E] to Close Door" : "Press [E] to Open Door";
                if (Input.GetKeyDown(KeyCode.E)) houseDoor.ToggleDoorState();
            }

            // 3. PICKUP ITEMS CHECK
            PickupItem pickupComponent = hit.collider.GetComponentInParent<PickupItem>()
                ?? hit.collider.GetComponent<PickupItem>();

            bool isGasBottleTarget = hit.collider.CompareTag("GasBottle") 
                                     || hit.collider.transform.root.CompareTag("GasBottle")
                                     || hit.collider.name.Contains("Gas");

            if ((pickupComponent != null || isGasBottleTarget) && !foundInteractiveTarget)
            {
                foundInteractiveTarget = true;

                if (pickupComponent != null && pickupComponent.itemType == PickupItem.ItemType.Axe)
                {
                    if (inventory != null && !inventory.unlockedSlots[1])
                    {
                        frameworkText = "Press [E] to pick up Axe";
                        if (Input.GetKeyDown(KeyCode.E)) { inventory.AddItemToHotbar(1, "Axe"); Destroy(pickupComponent.gameObject); }
                    }
                    else frameworkText = "You already have an Axe";
                }
                else if (pickupComponent != null && pickupComponent.itemType == PickupItem.ItemType.Log)
                {
                    if (inventory != null)
                    {
                        if (inventory.CanPickupLog())
                        {
                            frameworkText = "Press [E] to pick up Log";
                            if (Input.GetKeyDown(KeyCode.E)) { inventory.AddItemToHotbar(2, "Log"); Destroy(pickupComponent.gameObject); }
                        }
                        else frameworkText = "<color=red>Inventory Full! Max 3 Logs</color>";
                    }
                }
                else if (isGasBottleTarget)
                {
                    if (inventory != null)
                    {
                        if (!inventory.unlockedSlots[3])
                        {
                            GameObject targetedRootInstance = pickupComponent != null ? pickupComponent.gameObject : hit.collider.gameObject;
                            if (targetedRootInstance.transform.parent != null && targetedRootInstance.transform.root.CompareTag("GasBottle"))
                            {
                                targetedRootInstance = targetedRootInstance.transform.root.gameObject;
                            }

                            GasBottleItem physicalGasComponent = targetedRootInstance.GetComponentInChildren<GasBottleItem>();
                            float remainingGasUnits = (physicalGasComponent != null) ? physicalGasComponent.structuralFuelCapacity : 10f;
                            int roundedPercentage = Mathf.CeilToInt((remainingGasUnits / 10f) * 100f);

                            frameworkText = $"Press [E] to collect Gas Bottle ({roundedPercentage}%)";
                            
                            if (Input.GetKeyDown(KeyCode.E)) 
                            { 
                                inventory.AddItemToHotbar(3, "GasBottle", remainingGasUnits); 
                                Destroy(targetedRootInstance); 
                            }
                        }
                        else frameworkText = "<color=yellow>Already carrying a Gas Bottle!</color>";
                    }
                }
            }

            // 4. WAREHOUSE CHECK (MISSION COUNTING LOG SYSTEM)
            if (hit.collider.CompareTag("WarehouseSpace") && !foundInteractiveTarget)
            {
                foundInteractiveTarget = true;
                
                int currentGoal = DayNightCycleManager.Instance.activeLogsRequired;
                int currentCount = DayNightCycleManager.Instance.currentLogsStoredCount;

                frameworkText = $"Warehouse Goal: {currentCount}/{currentGoal} | [P] Store Log Progress";
                
                if (Input.GetKeyDown(KeyCode.P) && inventory != null && inventory.woodLogCount > 0)
                {
                    DayNightCycleManager.Instance.currentLogsStoredCount++;
                    inventory.woodLogCount--; 
                    inventory.UpdateLogCounterUI(); 
                    CheckInventoryLogDepletionGuard();
                    
                    DayNightCycleManager.Instance.UpdateTopLeftMissionUI();
                    DayNightCycleManager.Instance.UpdateWarehouseVisualDisplay();
                }
            }

            // 5. BED INTERACTION SYSTEM CHECK
            if (hit.collider.CompareTag("Bed") || hit.collider.name.Contains("Bed"))
            {
                foundInteractiveTarget = true;
                
                if (!DayNightCycleManager.Instance.isNightTimeActive)
                {
                    frameworkText = "You can only sleep at nighttime!";
                }
                else if (DayNightCycleManager.Instance.CanPlayerSleepOvernight())
                {
                    frameworkText = "Press [E] to sleep through the night safely";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        DayNightCycleManager.Instance.AdvanceToNextDaySequence(hit.collider.transform);
                    }
                }
                else
                {
                    frameworkText = $"<color=red>{DayNightCycleManager.Instance.GetMissingRequirementsString()}</color>";
                }
            }

            // 6. FIXED TERRAIN CHOPPING TREE CHECK
            // Use GetComponentInParent to safely find the Terrain script regardless of compound colliders.
            Terrain terrain = hit.collider.GetComponentInParent<Terrain>();
            if (terrain != null && terrain.terrainData != null && !foundInteractiveTarget && !isProcessingTreeCut)
            {
                TerrainData terrainData = terrain.terrainData;
                
                // ADJUST THESE FOR YOUR PRECISION PREFERENCE:
                float maxCrosshairRadius = 1.8f; // Distance threshold on the horizontal XZ plane to the trunk
                float maxPlayerReach = 4.0f;     // Absolute max physical reach distance
                
                float closestAimDistance = float.MaxValue;
                int targetedTreeIndex = -1;

                TreeInstance[] trees = terrainData.treeInstances;
                for (int i = 0; i < trees.Length; i++)
                {
                    if (choppedTreeIndices.Contains(i) || trees[i].position.y < -2f || trees[i].widthScale <= 0.01f) continue;

                    if (!validTreePrototypeIndexes.Contains(trees[i].prototypeIndex)) continue;

                    // Calculate world position of this tree instance
                    Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainData.size) + terrain.transform.position;
                    
                    // We check distance entirely on the X-Z plane (ignoring height differences) 
                    // This guarantees that looking up at the high trunk still registers accurately!
                    Vector3 flatHitPoint = new Vector3(hit.point.x, 0, hit.point.z);
                    Vector3 flatTreePos = new Vector3(treeWorldPos.x, 0, treeWorldPos.z);
                    float crosshairDistance = Vector3.Distance(flatHitPoint, flatTreePos);
                    
                    // Physical check: Ensure player is standing near the trunk
                    float playerDistance = Vector3.Distance(transform.position, treeWorldPos);

                    if (crosshairDistance <= maxCrosshairRadius && playerDistance <= maxPlayerReach)
                    {
                        if (crosshairDistance < closestAimDistance)
                        {
                            closestAimDistance = crosshairDistance;
                            targetedTreeIndex = i;
                        }
                    }
                }

                if (targetedTreeIndex != -1)
                {
                    foundInteractiveTarget = true;
                    lookingAtTreeValue = true;
                    if (inventory != null && !inventory.IsHoldingAxe())
                    {
                        frameworkText = "Requires an Axe to chop";
                        ResetProgressVisuals();
                    }
                    else if (inventory != null && inventory.IsHoldingAxe() && Time.time >= nextAttackAllowedTime)
                    {
                        frameworkText = "Hold [Left Click] to chop tree";
                        if (Input.GetMouseButton(0))
                        {
                            treeChopTimer += Time.deltaTime;
                            if (axeAnimator != null) axeAnimator.SetBool("isChopping", true);
                            if (chopProgressCircle != null) { chopProgressCircle.gameObject.SetActive(true); chopProgressCircle.fillAmount = treeChopTimer / totalTreeChopDuration; }
                            if (treeChopTimer >= totalTreeChopDuration) { ResetProgressVisuals(); StartCoroutine(ExecutePhysicalTreeRemoval(terrain, targetedTreeIndex)); }
                        }
                    }
                }
            }
        }

        if (!lookingAtBoatThisFrame)
        {
            if (fuelProgressBarSlider != null && fuelProgressBarSlider.gameObject.activeSelf)
            {
                fuelProgressBarSlider.gameObject.SetActive(false);
            }
        }

        if (!lookingAtTreeValue || !Input.GetMouseButton(0))
        {
            ResetProgressVisuals();
            if (axeAnimator != null) axeAnimator.SetBool("isChopping", false);
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackAllowedTime && !lookingAtTreeValue && !lookingAtBoatThisFrame)
        {
            if (inventory != null && inventory.IsHoldingAxe()) ExecuteSingleSwingAttack();
        }

        if (foundInteractiveTarget && !string.IsNullOrEmpty(frameworkText)) 
        {
            SetPromptText(frameworkText);
        }
        else 
        {
            ClearPrompt();
        }
    }

    public void TriggerWinSequence()
    {
        if (isGameOverSequenceActive) return; 
        isGameOverSequenceActive = true;

        ClearPrompt();
        
        if (youWinCanvas != null)
        {
            youWinCanvas.SetActive(true);
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            SetControllersState(false);

            if (playerSurvival != null)
            {
                playerSurvival.hasWonGame = true;
                playerSurvival.enabled = false;
            }
        }
    }

    public void MaintainSavagePopulationCount()
    {
        if (abnormalSpeciesPrefab == null || transformSpawnPoints == null || transformSpawnPoints.Length == 0) return;

        aliveSavagesList.RemoveAll(item => item == null);
        int currentMaxAllowed = DayNightCycleManager.Instance.activeSavageMaxSimultaneous;

        Debug.Log($"[SPAWNER] Active savages: {aliveSavagesList.Count} / Target: {currentMaxAllowed}");

        while (aliveSavagesList.Count < currentMaxAllowed)
        {
            Transform randomPoint = transformSpawnPoints[Random.Range(0, transformSpawnPoints.Length)];
            if (randomPoint != null)
            {
                GameObject newSavage = Instantiate(abnormalSpeciesPrefab, randomPoint.position, randomPoint.rotation);
                aliveSavagesList.Add(newSavage);
                Debug.Log($"[SPAWNER] Spawned new savage at {randomPoint.name}.");
            }
        }
    }

    public void ResetEnvironmentToMorning()
    {
        totalTreesChopped = 0; 
        aliveSavagesList.Clear(); 
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f); 
            directionalLight.intensity = 1.0f;
        }
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.ambientSkyColor = new Color(0.2f, 0.2f, 0.2f); 
        DynamicGI.UpdateEnvironment();
    }

    public void SetControllersState(bool state)
    {
        this.enabled = state;
        DisableComponentOnTarget(this.gameObject, "FirstPersonController", state);
        DisableComponentOnTarget(this.gameObject, "PlayerMovement", state);
        DisableComponentOnTarget(this.gameObject, "CharacterController", state);

        if (cameraTransform != null)
        {
            DisableComponentOnTarget(cameraTransform.gameObject, "MouseLook", state);
            DisableComponentOnTarget(cameraTransform.gameObject, "CameraController", state);
        }
        
        Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !state;
    }

    private void DisableComponentOnTarget(GameObject targetObj, string typeName, bool state)
    {
        if (targetObj == null) return;
        Component targetComp = targetObj.GetComponent(typeName);
        if (targetComp != null && targetComp is MonoBehaviour)
        {
            ((MonoBehaviour)targetComp).enabled = state;
        }
    }

    void ExecuteSingleSwingAttack()
    {
        nextAttackAllowedTime = Time.time + singleSwingFreezeDuration;
        if (axeAnimator != null) axeAnimator.SetTrigger("swingOnce");

        Collider[] victims = Physics.OverlapSphere(transform.position, attackRadius);
        foreach (Collider target in victims)
        {
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToTarget) <= (attackAngle / 2f))
            {
                SavageAI savage = target.transform.root.GetComponentInChildren<SavageAI>() 
                               ?? target.GetComponentInParent<SavageAI>() 
                               ?? target.GetComponentInChildren<SavageAI>() 
                               ?? target.GetComponent<SavageAI>();

                if (savage != null) 
                {
                    savage.TakeDamage(15f);

                    if (bloodSplashPrefab != null)
                    {
                        GameObject strikeSplash = Instantiate(bloodSplashPrefab, target.bounds.center, Quaternion.identity);
                        Destroy(strikeSplash, 2.0f);
                    }
                }
            }
        }
    }

    IEnumerator ExecutePhysicalTreeRemoval(Terrain terrain, int accurateTreeIndex)
    {
        isProcessingTreeCut = true;
        ClearPrompt();
        if (axeAnimator != null) axeAnimator.SetBool("isChopping", false);

        TerrainData data = terrain.terrainData;
        TreeInstance[] currentTrees = data.treeInstances;

        if (accurateTreeIndex >= 0 && accurateTreeIndex < currentTrees.Length)
        {
            choppedTreeIndices.Add(accurateTreeIndex);
            TreeInstance targetedInstance = currentTrees[accurateTreeIndex];
            Vector3 finalSpawnPos = Vector3.Scale(targetedInstance.position, data.size) + terrain.transform.position;

            targetedInstance.widthScale = 0f; targetedInstance.heightScale = 0f;
            targetedInstance.position = new Vector3(targetedInstance.position.x, -25f, targetedInstance.position.z);
            currentTrees[accurateTreeIndex] = targetedInstance;
            data.treeInstances = currentTrees;
            terrain.Flush();

            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc != null) { tc.enabled = false; Physics.SyncTransforms(); tc.enabled = true; }

            totalTreesChopped++;

            if (totalTreesChopped == treesRequiredToSpawnSavage && bloodSplashPrefab != null)
            {
                GameObject treeBloodSplash = Instantiate(bloodSplashPrefab, finalSpawnPos + Vector3.up * 1.5f, Quaternion.identity);
                Destroy(treeBloodSplash, 3.0f);
            }

            ProgressTimeAndDarkenSky();
            CheckAbnormalSpeciesTrigger();
            if (logPrefab != null) Instantiate(logPrefab, finalSpawnPos + Vector3.up * 1.0f, Quaternion.identity);
        }
        yield return new WaitForSeconds(0.2f);
        isProcessingTreeCut = false;
    }

    void CheckInventoryLogDepletionGuard()
    {
        if (inventory != null && inventory.woodLogCount <= 0)
        {
            if (inventory.visualLogInHand != null && inventory.visualLogInHand.activeSelf) inventory.visualLogInHand.SetActive(false);
            if (inventory.currentSlot == 2) { inventory.unlockedSlots[2] = false; inventory.UpdateLogCounterUI(); }
        }
    }

    void ResetProgressVisuals()
    {
        treeChopTimer = 0f;
        if (chopProgressCircle != null) { chopProgressCircle.fillAmount = 0f; chopProgressCircle.gameObject.SetActive(false); }
    }

    void ProgressTimeAndDarkenSky()
    {
        if (directionalLight != null)
        {
            if (directionalLight.transform.forward.y < 0.85f) directionalLight.transform.Rotate(Vector3.right * timeAdvancePerChop, Space.World);
            RenderSettings.ambientIntensity = Mathf.Max(0.01f, RenderSettings.ambientIntensity - 0.05f);
            directionalLight.intensity = Mathf.Max(0.00f, directionalLight.intensity - 0.05f);
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, Color.black, 0.15f);
            DynamicGI.UpdateEnvironment();
        }
    }

    void CheckAbnormalSpeciesTrigger() 
    { 
        if (totalTreesChopped >= treesRequiredToSpawnSavage && !DayNightCycleManager.Instance.isNightTimeActive) 
        { 
            DayNightCycleManager.Instance.isNightTimeActive = true;
            DayNightCycleManager.Instance.UpdateTopLeftMissionUI();
            
            MaintainSavagePopulationCount();

            if (DayNightCycleManager.Instance.currentDayNumber == 1)
            {
                SpawnGasBottlesRandomly();
            }
            DayNightCycleManager.Instance.ToggleGasBottlesVisibility(true);
        } 
    }

    public void SetPromptText(string text)
    {
        if (hintTextElement != null)
        {
            if (!hintTextElement.gameObject.activeSelf) hintTextElement.gameObject.SetActive(true);
            hintTextElement.text = text;
            hintTextElement.ForceMeshUpdate();
        }
    }

    public void ClearPrompt()
    {
        if (hintTextElement != null)
        {
            hintTextElement.text = "";
            hintTextElement.ForceMeshUpdate();
        }
    }
    
    void HandleAbandonItemInput()
    {
        if (!Input.GetKeyDown(KeyCode.Q) || inventory == null) return;
        if (inventory.currentSlot == 1 && inventory.unlockedSlots[1]) { inventory.DropAxeFromInventory(); SpawnDroppedItem(axePrefab); }
        else if (inventory.currentSlot == 2 && inventory.unlockedSlots[2] && inventory.woodLogCount > 0) { inventory.DropSingleLogFromInventory(); SpawnDroppedItem(logPrefab); }
        else if (inventory.currentSlot == 3 && inventory.unlockedSlots[3]) 
        { 
            float remainingFuelToInject = inventory.gasBottleFuelCapacity;
            inventory.DropGasCanFromInventory(); 
            SpawnDroppedItem(gasBottlePrefab, remainingFuelToInject); 
        }
    }

    void SpawnDroppedItem(GameObject itemPrefab) => SpawnDroppedItem(itemPrefab, 0f);

    void SpawnDroppedItem(GameObject itemPrefab, float fuelDataToPreserve)
    {
        if (itemPrefab == null) return;
        Vector3 spawnPosition = transform.position + transform.forward * 1.5f + Vector3.up * 1.2f;
        GameObject spawnedItem = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        
        GasBottleItem physicalGasComponent = spawnedItem.GetComponentInChildren<GasBottleItem>() ?? spawnedItem.AddComponent<GasBottleItem>();
        if (physicalGasComponent != null && itemPrefab == gasBottlePrefab)
        {
            physicalGasComponent.structuralFuelCapacity = fuelDataToPreserve;
        }

        foreach (Collider col in spawnedItem.GetComponentsInChildren<Collider>()) { col.isTrigger = false; col.enabled = true; }
        Rigidbody rbTemp = spawnedItem.GetComponent<Rigidbody>() ?? spawnedItem.AddComponent<Rigidbody>();
        rbTemp.isKinematic = false; rbTemp.useGravity = true; rbTemp.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnApplicationQuit()
    {
        if (currentTerrain != null && currentTerrain.terrainData != null && originalTreesBackup != null) { currentTerrain.terrainData.treeInstances = originalTreesBackup; currentTerrain.Flush(); }
    }
}