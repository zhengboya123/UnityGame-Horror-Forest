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

    [Header("Axe Animation Hook")]
    public Animator axeAnimator; 

    [Header("UI Progress Assets")]
    public Image chopProgressCircle;

    [Header("Item Prefabs to Drop")]
    public GameObject axePrefab;
    public GameObject logPrefab;
    public GameObject gasBottlePrefab;

    [Header("Day/Night & Spawning System")]
    public Light directionalLight;
    public float timeAdvancePerChop = 15.0f;
    public GameObject abnormalSpeciesPrefab;
    public Transform[] spawnPoints;
    
    [Header("Savage Trigger Config")]
    public int treesRequiredToSpawnSavage = 20;

    [Header("Game Progression Stats")]
    public int totalTreesChopped = 0;
    private bool speciesSpawned = false;

    private InventorySystem inventory;
    private PlayerSurvival playerSurvival;
    private Terrain currentTerrain;
    
    private float treeChopTimer = 0f;
    private const float totalTreeChopDuration = 5.0f;
    private float gasPourTimer = 0f;
    private const float singleBottlePourDuration = 10.0f;

    private bool isProcessingTreeCut = false;
    private TreeInstance[] originalTreesBackup;
    private HashSet<int> choppedTreeIndices = new HashSet<int>();

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        playerSurvival = GetComponent<PlayerSurvival>();

        currentTerrain = Terrain.activeTerrain;
        if (currentTerrain != null && currentTerrain.terrainData != null)
        {
            originalTreesBackup = (TreeInstance[])currentTerrain.terrainData.treeInstances.Clone();
        }

        // SANITY CHECK 1: Missing reference warning
        if (hintTextElement == null)
        {
            Debug.LogError("[CRITICAL UI ERROR] 'hintTextElement' is NOT assigned in the PlayerInteraction inspector! The script has nowhere to print text.");
        }
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            Debug.LogWarning("[Setup Warning] cameraTransform was empty, auto-assigned to Main Camera.");
        }

        ResetProgressVisuals();
        ClearPrompt();
    }

    void Update()
    {
        ManageInteractionRaycast();
        HandleAbandonItemInput();
        CheckInventoryLogDepletionGuard();
    }

    void ManageInteractionRaycast()
    {
        string frameworkText = "";
        bool foundInteractiveTarget = false;
        bool lookingAtTreeValue = false;
        bool pouringGasThisFrame = false;

        if (cameraTransform == null) return;

        // Visual Debug Ray: Red means it hit nothing, Green means it hit something!
        RaycastHit hit;
        bool raycastHitAnything = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance);
        
        if (raycastHitAnything)
        {
            Debug.DrawLine(cameraTransform.position, hit.point, Color.green);
            
            // DIAGNOSTIC LOG: Print out what you are looking at to the console
            // Debug.Log($"[Raycast Trace] Looking directly at GameObject: {hit.collider.gameObject.name} | Tag: {hit.collider.tag}");

            // 1. BOAT CHECK
            ObjectiveInteractable objective = hit.collider.GetComponentInParent<ObjectiveInteractable>()
                ?? hit.collider.GetComponent<ObjectiveInteractable>();

            if (objective != null && objective.objectType == ObjectiveInteractable.InteractionObjectType.EscapeBoat)
            {
                foundInteractiveTarget = true;
                if (playerSurvival != null)
                {
                    if (playerSurvival.gasTanksCollected >= playerSurvival.totalGasNeeded)
                    {
                        frameworkText = "Press [E] to escape the forest! (WIN)";
                        if (Input.GetKeyDown(KeyCode.E)) objective.ProcessInteraction(playerSurvival);
                    }
                    else
                    {
                        bool holdingGas = (inventory != null && inventory.currentSlot == 3 && inventory.unlockedSlots[3]);
                        if (holdingGas)
                        {
                            frameworkText = $"Hold [P] to pour Gasoline ({gasPourTimer:F1}s / 10s) | Progress: {playerSurvival.gasTanksCollected}/{playerSurvival.totalGasNeeded}";
                            if (Input.GetKey(KeyCode.P))
                            {
                                pouringGasThisFrame = true;
                                gasPourTimer += Time.deltaTime;
                                if (chopProgressCircle != null)
                                {
                                    chopProgressCircle.gameObject.SetActive(true);
                                    chopProgressCircle.fillAmount = gasPourTimer / singleBottlePourDuration;
                                }
                                if (gasPourTimer >= singleBottlePourDuration)
                                {
                                    gasPourTimer = 0f;
                                    ResetProgressVisuals();
                                    playerSurvival.CollectGasTank();
                                    if (inventory != null) { inventory.unlockedSlots[3] = false; inventory.currentSlot = 0; }
                                }
                            }
                        }
                        else
                        {
                            frameworkText = $"Boat needs Fuel ({playerSurvival.gasTanksCollected}/{playerSurvival.totalGasNeeded} poured). Equip a Gas Bottle!";
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

            if (pickupComponent != null && !foundInteractiveTarget)
            {
                foundInteractiveTarget = true;
                if (pickupComponent.itemType == PickupItem.ItemType.Axe)
                {
                    if (inventory != null && !inventory.unlockedSlots[1])
                    {
                        frameworkText = "Press [E] to pick up Axe";
                        if (Input.GetKeyDown(KeyCode.E)) { inventory.AddItemToHotbar(1, "Axe"); Destroy(pickupComponent.gameObject); }
                    }
                    else frameworkText = "You already have an Axe";
                }
                else if (pickupComponent.itemType == PickupItem.ItemType.Log)
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
                else if (pickupComponent.name.Contains("Gas") || pickupComponent.CompareTag("GasBottle"))
                {
                    if (inventory != null)
                    {
                        if (!inventory.unlockedSlots[3])
                        {
                            frameworkText = "Press [E] to collect Gas Bottle (Max 1)";
                            if (Input.GetKeyDown(KeyCode.E)) { inventory.unlockedSlots[3] = true; inventory.currentSlot = 3; Destroy(pickupComponent.gameObject); }
                        }
                        else frameworkText = "<color=yellow>Already carrying a Gas Bottle!</color>";
                    }
                }
            }

            // 4. WAREHOUSE CHECK
            if (hit.collider.CompareTag("WarehouseSpace") && !foundInteractiveTarget)
            {
                foundInteractiveTarget = true;
                LogDropZone dropZone = hit.collider.GetComponent<LogDropZone>();
                if (dropZone != null)
                {
                    int currentStored = dropZone.totalLogsInWarehouse;
                    frameworkText = $"Warehouse: {currentStored}/50 | [P] Store Log | [G] Get Log";
                    if (Input.GetKeyDown(KeyCode.P) && currentStored < 50 && inventory != null && inventory.woodLogCount > 0)
                    {
                        dropZone.totalLogsInWarehouse++; inventory.woodLogCount--; inventory.UpdateLogCounterUI(); CheckInventoryLogDepletionGuard();
                    }
                    if (Input.GetKeyDown(KeyCode.G) && currentStored > 0 && inventory != null && inventory.CanPickupLog())
                    {
                        dropZone.totalLogsInWarehouse--; inventory.AddItemToHotbar(2, "Log");
                    }
                }
            }

            // 5. TERRAIN CHOPPING TREE CHECK
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null && terrain.terrainData != null && !foundInteractiveTarget && !isProcessingTreeCut)
            {
                TerrainData terrainData = terrain.terrainData;
                float maxChopRadius = 3.5f;
                float closestDistance = float.MaxValue;
                int targetedTreeIndex = -1;

                TreeInstance[] trees = terrainData.treeInstances;
                for (int i = 0; i < trees.Length; i++)
                {
                    if (choppedTreeIndices.Contains(i) || trees[i].position.y < -2f || trees[i].widthScale <= 0.01f) continue;
                    Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainData.size) + terrain.transform.position;
                    float distance = Vector3.Distance(hit.point, treeWorldPos);
                    if (distance < closestDistance) { closestDistance = distance; targetedTreeIndex = i; }
                }

                if (targetedTreeIndex != -1 && closestDistance <= maxChopRadius)
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
        else
        {
            // If the raycast completely missed everything, draw a long red line out from center camera
            Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactionDistance, Color.red);
        }

        // --- ENGINE HOOKS ---
        if (!pouringGasThisFrame) gasPourTimer = 0f;
        if ((!lookingAtTreeValue || !Input.GetMouseButton(0)) && !pouringGasThisFrame)
        {
            ResetProgressVisuals();
            if (axeAnimator != null) axeAnimator.SetBool("isChopping", false);
        }
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackAllowedTime && !lookingAtTreeValue)
        {
            if (inventory != null && inventory.IsHoldingAxe()) ExecuteSingleSwingAttack();
        }

        // --- FINAL RENDERING ZONE ---
        if (foundInteractiveTarget && !string.IsNullOrEmpty(frameworkText)) 
        {
            // DIAGNOSTIC LOG: Print out the text assigned to the UI elements
            Debug.Log($"[UI Output Success] Displaying text: '{frameworkText}'");
            SetPromptText(frameworkText);
        }
        else 
        {
            ClearPrompt();
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
                SavageAI savage = target.GetComponentInChildren<SavageAI>() ?? target.GetComponentInParent<SavageAI>() ?? target.GetComponent<SavageAI>();
                if (savage != null) savage.TakeDamage(15f);
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

    void CheckAbnormalSpeciesTrigger() { if (totalTreesChopped >= treesRequiredToSpawnSavage && !speciesSpawned) { speciesSpawned = true; SpawnAbnormalEntities(); } }
    void SpawnAbnormalEntities() { if (abnormalSpeciesPrefab == null) return; if (spawnPoints != null && spawnPoints.Length > 0) { foreach (Transform sp in spawnPoints) { if (sp != null) Instantiate(abnormalSpeciesPrefab, sp.position, sp.rotation); } } }

    void SetPromptText(string text)
    {
        if (hintTextElement != null)
        {
            // 1. Force the GameObject itself to be fully active in the scene hierarchy
            if (!hintTextElement.gameObject.activeSelf)
            {
                hintTextElement.gameObject.SetActive(true);
            }

            // 2. Assign the string data directly to the text mesh property
            hintTextElement.text = text;

            // 3. FORCE UNITY GRAPHICS ENGINE TO REDRAW THE CANVAS TEXT INSTANTLY!
            // This completely bypasses any delayed layout or font mesh initialization bugs.
            hintTextElement.ForceMeshUpdate();
        }
    }

    public void ClearPrompt()
    {
        if (hintTextElement != null)
        {
            hintTextElement.text = "";
            
            // Ensure the canvas layout handles the blank update accurately
            hintTextElement.ForceMeshUpdate();
        }
    }
    
    void HandleAbandonItemInput()
    {
        if (!Input.GetKeyDown(KeyCode.Q) || inventory == null) return;
        if (inventory.currentSlot == 1 && inventory.unlockedSlots[1]) { inventory.DropAxeFromInventory(); SpawnDroppedItem(axePrefab); }
        else if (inventory.currentSlot == 2 && inventory.unlockedSlots[2] && inventory.woodLogCount > 0) { inventory.DropSingleLogFromInventory(); SpawnDroppedItem(logPrefab); }
        else if (inventory.currentSlot == 3 && inventory.unlockedSlots[3]) { inventory.unlockedSlots[3] = false; inventory.currentSlot = 0; SpawnDroppedItem(gasBottlePrefab); }
    }

    void SpawnDroppedItem(GameObject itemPrefab)
    {
        if (itemPrefab == null) return;
        Vector3 spawnPosition = transform.position + transform.forward * 1.5f + Vector3.up * 1.2f;
        GameObject spawnedItem = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        foreach (Collider col in spawnedItem.GetComponentsInChildren<Collider>()) { col.isTrigger = false; col.enabled = true; }
        Rigidbody rbTemp = spawnedItem.GetComponent<Rigidbody>() ?? spawnedItem.AddComponent<Rigidbody>();
        rbTemp.isKinematic = false; rbTemp.useGravity = true; rbTemp.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnApplicationQuit()
    {
        if (currentTerrain != null && currentTerrain.terrainData != null && originalTreesBackup != null) { currentTerrain.terrainData.treeInstances = originalTreesBackup; currentTerrain.Flush(); }
    }
}