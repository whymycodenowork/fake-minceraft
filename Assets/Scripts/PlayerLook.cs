using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public PauseManager pauseManager;
    public Vector2 turn;
    public float sensitivity;
    public bool thirdPerson;
    public float cameraOffset;
    public LayerMask collisionLayers; // Layers to check for collisions

    private Vector3 desiredPosition;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Lock the cursor if the game isn't paused
        if (!pauseManager.isPaused)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;

            // Capture mouse input for rotation
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            // Clamp vertical rotation (pitch)
            turn.x -= mouseY * sensitivity; // Invert Y-axis for typical FPS feel
            turn.x = Mathf.Clamp(turn.x, -90, 90); // Prevent excessive tilt

            // Apply vertical rotation (local pitch)
            transform.localEulerAngles = new Vector3(turn.x, transform.localEulerAngles.y, 0);

            // Apply horizontal rotation (yaw)
            transform.Rotate(Vector3.up * mouseX * sensitivity, Space.World); // Rotate around global Y-axis
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
        }

        // Toggle third-person mode
        if (Input.GetKeyDown(KeyCode.F5))
        {
            thirdPerson = !thirdPerson;
        }
    }
}
