using UnityEngine;
using UnityEngine.SceneManagement; // FIXED: This line clears the CS0103 compile error!

public class ObjectiveInteractable : MonoBehaviour
{
    public enum InteractionObjectType { GasTank, EscapeBoat }
    [Header("Target Properties")]
    public InteractionObjectType objectType;

    public void ProcessInteraction(PlayerSurvival survival)
    {
        if (objectType == InteractionObjectType.EscapeBoat)
        {
            if (survival.gasTanksCollected >= survival.totalGasNeeded)
            {
                // Trigger the victory escape loop!
                survival.EscapeSuccessVictory();
            }
        }
        // Keep your regular GasTank pick up loop block below if you have one...
    }
}