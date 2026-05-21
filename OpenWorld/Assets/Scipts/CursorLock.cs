using UnityEngine;

public class CursorLock : MonoBehaviour
{
    void Start()
    {
        // Hide the mouse cursor and lock it to the center of the game window
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Optional: If you press Escape, free the mouse so you can stop the game safely
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}