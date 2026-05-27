using UnityEngine;
using UnityEngine.SceneManagement;

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
                // Trigger the victory escape loop panel securely
                survival.EscapeSuccessVictory();
            }
        }
    }
}