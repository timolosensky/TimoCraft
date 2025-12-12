using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.0f;
    public float gravity = -9.81f;

    [Header("References")]
    public GameObject cameraHolder; // Zieh hier deine Kamera rein

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // WICHTIG: Wenn das NICHT mein Spieler ist...
        if (!isLocalPlayer)
        {
            // ...dann schalte ich SEINE Kamera und SEINEN AudioListener aus.
            if (cameraHolder != null) 
            {
                cameraHolder.SetActive(false);
                // AudioListener auch deaktivieren, sonst hörst du doppelt
                var listener = cameraHolder.GetComponent<AudioListener>();
                if (listener) listener.enabled = false;
            }
            return;
        }

        // Wenn es MEIN Spieler ist:
        // Maus fangen und verstecken
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    void Update()
    {
        // Nur ICH darf MEINEN Spieler steuern
        if (!isLocalPlayer) return;

        HandleMovement();
        HandleMouseLook();

        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
    {
        // Toggle-Logik: Wenn gefangen, dann freigeben. Wenn frei, dann fangen.
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Maus befreien
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Maus fangen
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    }

    void HandleMovement()
    {
        // Ist er am Boden?
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Kleiner Druck nach unten, damit er nicht hoppelt
        }

        // WASD Input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Bewegung relativ zur Blickrichtung
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Springen
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Schwerkraft
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Hoch/Runter schauen (Kamera rotieren)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Genickbruch verhindern

        if (cameraHolder != null)
            cameraHolder.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Links/Rechts schauen (Ganzen Körper rotieren)
        transform.Rotate(Vector3.up * mouseX);
    }
}