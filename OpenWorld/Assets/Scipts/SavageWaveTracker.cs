using UnityEngine;

public class SavageWaveTracker : MonoBehaviour
{
    public static SavageWaveTracker Instance;
    public int activeSavagesInWorld = 0;
    private PlayerInteraction playerInteraction;

    void Awake()
    {
        Instance = this;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerInteraction = player.GetComponent<PlayerInteraction>();
    }

    public void RegisterSpawn()
    {
        activeSavagesInWorld++;
    }

    public void ReportKill()
    {
        activeSavagesInWorld--;
        Debug.Log($"Savage terminated. Remaining: {activeSavagesInWorld}");

        if (activeSavagesInWorld <= 0)
        {
            Debug.LogWarning("Clear Wave Victory! Sun rotation speed cut in half for tomorrow.");
            if (playerInteraction != null)
            {
                // Half the dusk speed penalty to extend exploration length significantly
                playerInteraction.timeAdvancePerChop = 2.25f; 
            }
        }
    }
}