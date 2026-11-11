using UnityEngine;

/// <summary>
/// First-person movement on a CharacterController: walk, sprint, jump, and slide.
/// Slide: hold Shift to sprint forward, then press Ctrl. The capsule shrinks so the Venator
/// can slide under low gaps, and stays crouched until there is room to stand back up.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(VenatorInput))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8.5f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    [Range(0f, 1f)] public float airControl = 0.6f;

    [Header("Slide")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.7f;
    public float slideCooldown = 0.4f;
    public float crouchHeight = 1.0f;

    [Header("Camera")]
    public Transform cameraPivot;
    public float standEyeHeight = 1.6f;
    public float crouchEyeHeight = 0.8f;
    public float eyeLerpSpeed = 12f;

    CharacterController cc;
    VenatorInput input;
    HealthSystem health;
    float standHeight;
    float verticalVelocity;
    Vector3 planarVelocity;
    Vector3 slideDirection;
    float slideTimer, nextSlideTime;
    readonly Collider[] overlap = new Collider[16];

    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsCrouched { get; private set; }
    public bool IsSprinting { get; private set; }
    public float PlanarSpeed => new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        input = GetComponent<VenatorInput>();
        health = GetComponent<HealthSystem>();
        standHeight = cc.height;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool dead = health != null && health.IsDead;
        IsGrounded = cc.isGrounded;
        if (IsGrounded && verticalVelocity < 0f) verticalVelocity = -2f; // keeps us pressed to slopes

        Vector2 m = dead ? Vector2.zero : input.Move;
        Vector3 wish = transform.right * m.x + transform.forward * m.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        IsSprinting = !dead && input.SprintHeld && m.y > 0.1f && !IsCrouched;

        // ---- start a slide
        if (!dead && !IsSliding && IsGrounded && IsSprinting && input.SlidePressed && Time.time >= nextSlideTime)
        {
            IsSliding = true;
            slideTimer = slideDuration;
            slideDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
            SetCapsuleHeight(crouchHeight);
        }

        // ---- choose planar velocity
        Vector3 target;
        if (IsSliding)
        {
            slideTimer -= dt;
            float t = Mathf.Clamp01(slideTimer / slideDuration);
            target = slideDirection * Mathf.Lerp(crouchSpeed, slideSpeed, t);
            if (slideTimer <= 0f || !IsGrounded)
            {
                IsSliding = false;
                nextSlideTime = Time.time + slideCooldown;
                IsCrouched = true; // try to stand below
            }
        }
        else
        {
            float speed = IsCrouched ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
            target = wish * speed;
        }

        // stand back up once there is headroom (and Ctrl is released)
        if (IsCrouched && !IsSliding && !input.SlideHeld && CanStand())
        {
            IsCrouched = false;
            SetCapsuleHeight(standHeight);
        }

        planarVelocity = IsGrounded || IsSliding ? target : Vector3.Lerp(planarVelocity, target, airControl * dt * 8f);

        // ---- jump (also cancels a slide)
        if (!dead && IsGrounded && input.JumpPressed && !IsCrouched && !IsSliding)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else if (!dead && IsGrounded && input.JumpPressed && IsSliding && CanStand())
        {
            IsSliding = false; IsCrouched = false; SetCapsuleHeight(standHeight);
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * dt;
        cc.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

        // ---- camera height follows the capsule
        if (cameraPivot != null)
        {
            float eye = (IsSliding || IsCrouched) ? crouchEyeHeight : standEyeHeight;
            Vector3 lp = cameraPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, eye, eyeLerpSpeed * dt);
            cameraPivot.localPosition = lp;
        }
    }

    void SetCapsuleHeight(float h)
    {
        cc.height = h;
        cc.center = new Vector3(0f, h * 0.5f, 0f);
    }

    /// <summary>True if a full-height capsule fits here (ignores our own collider).</summary>
    bool CanStand()
    {
        float r = cc.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (crouchHeight);
        Vector3 top = transform.position + Vector3.up * (standHeight - r);
        if (top.y < bottom.y) top = bottom;
        int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlap, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!overlap[i].transform.IsChildOf(transform)) return false;
        return true;
    }

    /// <summary>Move the player instantly (respawns, checkpoints). CharacterController must be off while teleporting.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        cc.enabled = true;
        verticalVelocity = 0f;
        planarVelocity = Vector3.zero;
        IsSliding = false;
        IsCrouched = false;
        SetCapsuleHeight(standHeight);
    }
}
