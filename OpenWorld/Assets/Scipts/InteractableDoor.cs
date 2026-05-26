using UnityEngine;

public class InteractableDoor : MonoBehaviour
{
    public bool isOpen = false;
    public float openAngle = 90f;
    public float smoothSpeed = 5f;

    private Vector3 originalLocalPosition;
    private Quaternion closedLocalRotation;
    private Quaternion openLocalRotation;

    void Awake()
    {
        // Restored your exact older coordinate cache layout
        originalLocalPosition = transform.localPosition;
        closedLocalRotation = transform.localRotation;
        openLocalRotation = transform.localRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        // Smoothly lerps using your original system parameters
        Quaternion target = isOpen ? openLocalRotation : closedLocalRotation;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            target,
            Time.deltaTime * smoothSpeed
        );
    }

    public void ToggleDoorState()
    {
        isOpen = !isOpen;
    }
}