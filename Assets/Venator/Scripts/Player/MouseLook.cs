using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns the player left/right (yaw) and tilts the camera up/down (pitch) from mouse movement.
/// Esc frees the cursor; clicking in the game window locks it again.
/// </summary>
[RequireComponent(typeof(VenatorInput))]
public class MouseLook : MonoBehaviour
{
    public Transform cameraPivot;
    [Tooltip("Degrees per pixel of mouse movement.")]
    public float sensitivity = 0.08f;
    public float maxPitch = 85f;
    public bool invertY = false;

    VenatorInput input;
    float pitch;

    /// <summary>The frame the cursor was locked, so the locking click does not also fire the crossbow.</summary>
    public int LockedFrame { get; private set; } = -1;
    public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

    void Awake() => input = GetComponent<VenatorInput>();

    void Start() => Lock();

    void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        LockedFrame = Time.frameCount;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!IsLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Lock();
        }

        if (!IsLocked) return;

        Vector2 d = input.Look * sensitivity;
        transform.Rotate(0f, d.x, 0f, Space.Self);
        pitch += invertY ? d.y : -d.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
