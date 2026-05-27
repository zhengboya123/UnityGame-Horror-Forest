using UnityEngine;

public class WaterDamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerSecond = 15f;
    
    private PlayerSurvival playerInWater;
    private float nextDamageTime = 0f;

    void Update()
    {
        // If the player is actively inside this box zone
        if (playerInWater != null && Time.time >= nextDamageTime)
        {
            playerInWater.TakeDamage(damagePerSecond * Time.deltaTime);
            // Apply damage smoothly frame-by-frame or chunk-by-chunk
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing that entered the box is the player
        PlayerSurvival player = other.GetComponentInParent<PlayerSurvival>() ?? other.GetComponent<PlayerSurvival>();
        if (player != null)
        {
            playerInWater = player;
            Debug.Log("Player entered dangerous water area!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerSurvival player = other.GetComponentInParent<PlayerSurvival>() ?? other.GetComponent<PlayerSurvival>();
        if (player != null && player == playerInWater)
        {
            playerInWater = null;
            Debug.Log("Player exited dangerous water area safely.");
        }
    }
}