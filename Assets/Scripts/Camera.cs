using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform player;
    public PauseManager pauseManager;
    public Vector2 turn;
    public float sensitivity;
    public bool thirdPerson;
    public float cameraOffset;
    public LayerMask collisionLayers; // Layers to check for collisions

    private Vector3 desiredPosition;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle pauses and third person
        transform.position = new Vector3(player.position.x, player.position.y + 0.5f, player.position.z);

        if (!pauseManager.isPaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            turn.x += -Input.GetAxis("Mouse Y") * sensitivity;
            turn.x = Mathf.Clamp(turn.x, -90, 90); // Clamping rotation to avoid excessive tilt

            turn.y += Input.GetAxis("Mouse X") * sensitivity;
            transform.localRotation = Quaternion.Euler(turn.x, turn.y, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (thirdPerson)
        {
            desiredPosition = player.position - transform.forward * cameraOffset;
            Vector3 direction = desiredPosition - player.position;

            // Perform raycast to detect collision
            RaycastHit hit;
            if (Physics.Raycast(player.position, direction, out hit, cameraOffset, collisionLayers))
            {
                transform.position = hit.point; // Move camera to collision point
            }
            else
            {
                transform.position = desiredPosition;
            }
        }
        else
        {
            transform.position = new Vector3(player.position.x, player.position.y + 0.5f, player.position.z);
        }

        // Replace with UI later
        if (Input.GetKeyDown(KeyCode.F5))
        {
            thirdPerson = !thirdPerson;
        }
    }
}
