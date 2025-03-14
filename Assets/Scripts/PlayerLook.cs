using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public PauseManager pauseManager;
    public InventoryManager inventoryManager;
    public Vector2 turn;
    public float sensitivity;
    public bool thirdPerson;
    public float cameraOffset;
    public LayerMask collisionLayers; // Layers to check for collisions

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Lock the cursor if no menus are open
        if (!(pauseManager.isPaused || inventoryManager.inventoryOpen))
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;

            // Capture mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            turn.x += mouseY * sensitivity;
            turn.x = Mathf.Clamp(turn.x, -80, 80); // Prevent excessive tilt

            // Apply vertical rotation to the head
            transform.localEulerAngles = new Vector3(0, 0, turn.x);

            // Apply horizontal rotation to the parent
            transform.parent.Rotate(mouseX * sensitivity * Vector3.up, Space.World); // Rotate parent
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
