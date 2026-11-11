using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads keyboard and mouse input through the Unity Input System.
/// The actions are defined in code so no .inputactions asset is needed.
/// Every other player script reads input from here instead of from devices directly.
/// </summary>
public class VenatorInput : MonoBehaviour
{
    InputAction move, look, jump, sprint, slide, fire, lash, reload, interact, tonic;

    void Awake()
    {
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        slide = new InputAction("Slide", InputActionType.Button, "<Keyboard>/leftCtrl");
        fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        lash = new InputAction("Lash", InputActionType.Button, "<Mouse>/rightButton");
        reload = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        tonic = new InputAction("Tonic", InputActionType.Button, "<Keyboard>/h");
    }

    InputAction[] All => new[] { move, look, jump, sprint, slide, fire, lash, reload, interact, tonic };

    void OnEnable() { foreach (var a in All) a.Enable(); }
    void OnDisable() { foreach (var a in All) a.Disable(); }
    void OnDestroy() { foreach (var a in All) a.Dispose(); }

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();
    public bool JumpPressed => jump.WasPressedThisFrame();
    public bool SprintHeld => sprint.IsPressed();
    public bool SlidePressed => slide.WasPressedThisFrame();
    public bool SlideHeld => slide.IsPressed();
    public bool FireHeld => fire.IsPressed();
    public bool LashPressed => lash.WasPressedThisFrame();
    public bool ReloadPressed => reload.WasPressedThisFrame();
    public bool InteractPressed => interact.WasPressedThisFrame();
    public bool TonicPressed => tonic.WasPressedThisFrame();
}
