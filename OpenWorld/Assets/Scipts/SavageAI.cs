using UnityEngine;
using UnityEngine.AI;

public class SavageAI : MonoBehaviour
{
    [Header("Savage Health Settings")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;

    [Header("Movement & Pathfinding")]
    public float chaseSpeed = 4.5f;
    public float stoppingDistance = 1.5f;
    
    [Header("Attack Properties")]
    public float damageAmount = 10f;
    public float attackCooldown = 1.5f;
    [Tooltip("How close the savage needs to be to hit the player")]
    public float attackRange = 2.0f; 
    private float nextAttackTime = 0f;

    [Header("Animations (Optional)")]
    public Animator savageAnimator;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private PlayerSurvival playerSurvival;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = stoppingDistance;
        }
        
        PlayerSurvival player = Object.FindFirstObjectByType<PlayerSurvival>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerSurvival = player;
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // 1. Pathfind towards player
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(playerTransform.position);
        }

        // 2. Handle animator speeds
        if (savageAnimator != null && agent != null)
        {
            savageAnimator.SetFloat("Speed", agent.velocity.magnitude);
        }

        // FIX: Pure proximity-based attack loop. Eliminates broken physics engine issues completely!
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            ExecuteAttackOnPlayer();
        }
    }

    private void ExecuteAttackOnPlayer()
    {
        if (playerSurvival != null && !playerSurvival.hasWonGame)
        {
            nextAttackTime = Time.time + attackCooldown;
            
            Debug.Log($"[SAVAGE ATTACK] {gameObject.name} hit the player for {damageAmount} damage!");
            playerSurvival.TakeDamage(damageAmount);

            if (savageAnimator != null)
            {
                savageAnimator.SetTrigger("Attack");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[SAVAGE] {gameObject.name} took {amount} damage. Health remaining: {currentHealth}");

        if (savageAnimator != null)
        {
            savageAnimator.SetTrigger("Hit");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[SAVAGE] {gameObject.name} died. Notifying DayNightCycleManager...");

        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.OnSavageKilled();
        }

        if (agent != null) agent.enabled = false;
        
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        if (savageAnimator != null)
        {
            savageAnimator.SetTrigger("Die");
        }

        Destroy(gameObject, 0.5f); 
    }
}