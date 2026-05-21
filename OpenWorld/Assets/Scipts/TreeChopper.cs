using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TreeChopper : MonoBehaviour
{
    public Transform cameraTransform; 
    public float chopDistance = 5.0f;
    public GameObject logPrefab; 
    public Image chopProgressCircle; 
    
    private InventorySystem inventory;
    private Terrain activeTerrain;
    
    private float chopProgressTime = 0f;
    private const float TIME_REQUIRED_TO_CHOP = 5.0f; 
    
    // Track the tree by its relative position safely
    private Vector3 lockedTreeLocalPos = Vector3.zero;
    private bool hasLockedTree = false;
    private bool isProcessingCut = false; 

    private TreeInstance[] originalTreesBackup;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            activeTerrain = terrain;
            originalTreesBackup = (TreeInstance[])terrain.terrainData.treeInstances.Clone();
        }

        if (chopProgressCircle != null)
        {
            chopProgressCircle.gameObject.SetActive(false);
            chopProgressCircle.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (isProcessingCut) return;

        if (Input.GetMouseButton(0) && inventory != null && inventory.IsHoldingAxe())
        {
            CheckAndChopTreeContinuous();
        }
        else
        {
            ResetChopProgress();
        }
    }

    void CheckAndChopTreeContinuous()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, chopDistance))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                TerrainData terrainData = terrain.terrainData;
                float maxChopRadius = 3.5f; 
                float closestDistance = float.MaxValue;
                TreeInstance closestTree = default;
                bool foundTree = false;
                
                TreeInstance[] trees = terrainData.treeInstances;

                for (int i = 0; i < trees.Length; i++)
                {
                    Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainData.size) + terrain.transform.position;
                    float distance = Vector3.Distance(hit.point, treeWorldPos);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTree = trees[i];
                        foundTree = true;
                    }
                }

                if (foundTree && closestDistance <= maxChopRadius)
                {
                    // Lock using internal local positions to bypass floating point variations
                    if (!hasLockedTree || Vector3.Distance(lockedTreeLocalPos, closestTree.position) > 0.01f)
                    {
                        lockedTreeLocalPos = closestTree.position;
                        hasLockedTree = true;
                        chopProgressTime = 0f; 
                    }

                    chopProgressTime += Time.deltaTime;

                    if (chopProgressCircle != null)
                    {
                        chopProgressCircle.gameObject.SetActive(true);
                        chopProgressCircle.fillAmount = chopProgressTime / TIME_REQUIRED_TO_CHOP;
                    }

                    if (chopProgressTime >= TIME_REQUIRED_TO_CHOP)
                    {
                        // Pass the exact hit point position to guarantee deletion accuracy
                        StartCoroutine(ExecuteClosestTreeCut(terrain, hit.point));
                        ResetChopProgress(); 
                    }
                    
                    return; 
                }
            }
        }

        ResetChopProgress();
    }

    IEnumerator ExecuteClosestTreeCut(Terrain terrain, Vector3 hitWorldPoint)
    {
        isProcessingCut = true; 

        TerrainData data = terrain.terrainData;
        List<TreeInstance> currentTrees = new List<TreeInstance>(data.treeInstances);
        
        int targetIndexToRemove = -1;
        float closestDistance = float.MaxValue;
        Vector3 finalSpawnPos = hitWorldPoint;

        // CRITICAL FIX: Find the absolute closest tree to where your crosshair is pointing right now
        for (int i = 0; i < currentTrees.Count; i++)
        {
            Vector3 checkWorldPos = Vector3.Scale(currentTrees[i].position, data.size) + terrain.transform.position;
            float dist = Vector3.Distance(hitWorldPoint, checkWorldPos);
            
            if (dist < closestDistance)
            {
                closestDistance = dist;
                targetIndexToRemove = i;
                finalSpawnPos = checkWorldPos; // Capture accurate coordinate layout for log drop
            }
        }

        // Dynamic check: Ensure the closest tree is within a valid interaction radius
        if (targetIndexToRemove != -1 && closestDistance < 5.0f)
        {
            currentTrees.RemoveAt(targetIndexToRemove);
            data.treeInstances = currentTrees.ToArray();
            
            terrain.Flush(); 

            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc != null)
            {
                tc.enabled = false;
                Physics.SyncTransforms(); 
                tc.enabled = true;
            }

            if (logPrefab != null)
            {
                GameObject spawnedLog = Instantiate(logPrefab, finalSpawnPos + Vector3.up * 1.0f, Quaternion.identity);
                spawnedLog.name = "Wood_Log"; 
            }
            Debug.Log("Tree successfully cut down via relative proximity scan.");
        }

        yield return new WaitForSeconds(0.3f);
        isProcessingCut = false; 
    }

    void ResetChopProgress()
    {
        hasLockedTree = false;
        lockedTreeLocalPos = Vector3.zero;
        chopProgressTime = 0f;
        
        if (chopProgressCircle != null)
        {
            chopProgressCircle.fillAmount = 0f;
            chopProgressCircle.gameObject.SetActive(false); 
        }
    }

    void OnApplicationQuit()
    {
        if (activeTerrain != null && activeTerrain.terrainData != null && originalTreesBackup != null)
        {
            activeTerrain.terrainData.treeInstances = originalTreesBackup;
            activeTerrain.Flush();
            
            TerrainCollider tc = activeTerrain.GetComponent<TerrainCollider>();
            if (tc != null)
            {
                tc.enabled = false;
                tc.enabled = true;
            }
        }
    }
}