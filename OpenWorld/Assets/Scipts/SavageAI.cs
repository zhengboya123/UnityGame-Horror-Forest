using UnityEngine;
using UnityEngine.AI;

public class SavageAI : MonoBehaviour
{
    [Header("Savage Attributes")]
    public float health = 50f;
    public float damage = 15f;
    public float attackRate = 1.5f;
    
    [Tooltip("Extra melee range buffer outside of agent stopping distance")]
    public float attackRangeBuffer = 1.5f; 

    private Transform playerTarget;
    private PlayerSurvival targetSurvival;
    private NavMeshAgent agent;
    private float nextAttackAllowedTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        FindPlayerReferences();
    }

    void Update()
    {
        // Fallback: If references were missed at spawn, try to acquire them again safely
        if (playerTarget == null || targetSurvival == null)
        {
            FindPlayerReferences();
            if (playerTarget == null || targetSurvival == null) return; // Skip frame if player doesn't exist
        }

        if (agent == null || !agent.isOnNavMesh) return;

        // Pathfind directly towards the player's position
        agent.SetDestination(playerTarget.position);

        // Check distance to see if enemy is within hitting range
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        // Dynamic attack range calculation that handles default stopping distances reliably
        float maxAttackDistance = Mathf.Max(agent.stoppingDistance, 0.5f) + attackRangeBuffer;

        if (distanceToPlayer <= maxAttackDistance)
        {
            // Only strike if the player is exposed outside of their base
            if (!targetSurvival.isInsideHouse)
            {
                AttemptDirectPlayerAttack();
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Player is safe inside house. Standing guard.");
            }
        }
    }

    void FindPlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            targetSurvival = playerObj.GetComponent<PlayerSurvival>() 
                             ?? playerObj.GetComponentInParent<PlayerSurvival>() 
                             ?? playerObj.GetComponentInChildren<PlayerSurvival>();
            
            if (targetSurvival == null)
            {
                Debug.LogError($"[{gameObject.name}] Found object with 'Player' tag, but it is missing the PlayerSurvival component script!");
            }
        }
    }

    void AttemptDirectPlayerAttack()
    {
        if (Time.time >= nextAttackAllowedTime)
        {
            nextAttackAllowedTime = Time.time + attackRate;
            
            Debug.LogWarning($"💥 [{gameObject.name}] Attacking player! Dealing {damage} damage.");
            targetSurvival.TakeDamage(damage);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"[{gameObject.name}] Took {amount} damage! Remaining Health: {health}");

        if (health <= 0)
        {
            Debug.Log($"💀 [{gameObject.name}] Defeated!");
            
            // Unregister from tracker if your wave script utilizes registration trackers
            if (SavageWaveTracker.Instance != null)
            {
                // Put any wave clean-up hook calls here if needed!
            }

            Destroy(gameObject);
        }
    }
}