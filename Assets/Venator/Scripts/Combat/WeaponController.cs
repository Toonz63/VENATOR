using System.Collections;
using UnityEngine;

/// <summary>
/// Repeater Crossbow (left mouse, hold for automatic fire), reload (R), and whip lash (right mouse).
/// Bolts aim at whatever is under the crosshair, so they hit what the player is looking at
/// even though they spawn from the crossbow on the right side of the screen.
/// </summary>
[RequireComponent(typeof(VenatorInput))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform muzzle;
    public Projectile boltPrefab;
    public Animator viewmodelAnimator;

    [Header("Repeater Crossbow")]
    public float boltSpeed = 60f;
    public float boltDamage = 25f;
    public float fireInterval = 0.22f;
    public int magazineSize = 18;
    public int startingReserve = 42;
    public int maxReserve = 60;
    public float reloadTime = 1.4f;

    public int Loaded { get; private set; }
    public int Reserve { get; private set; }
    public bool IsReloading { get; private set; }

    VenatorInput input;
    MouseLook look;
    HealthSystem health;
    FirstPersonController mover;
    WhipWeapon whip;
    float nextFireTime;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int FireHash = Animator.StringToHash("Fire");
    static readonly int ReloadHash = Animator.StringToHash("Reload");
    static readonly int LashHash = Animator.StringToHash("Lash");

    void Awake()
    {
        input = GetComponent<VenatorInput>();
        look = GetComponent<MouseLook>();
        health = GetComponent<HealthSystem>();
        mover = GetComponent<FirstPersonController>();
        whip = GetComponent<WhipWeapon>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        Loaded = magazineSize;
        Reserve = Mathf.Min(startingReserve, maxReserve);
    }

    void Update()
    {
        if (AnimReady && mover != null)
            viewmodelAnimator.SetFloat(SpeedHash, mover.IsGrounded ? mover.PlanarSpeed : 0f);

        if (health != null && health.IsDead) return;
        // don't shoot while the cursor is free, or on the click that re-locks it
        if (look != null && (!look.IsLocked || Time.frameCount - look.LockedFrame < 2)) return;

        if (input.ReloadPressed) TryReload();

        if (input.LashPressed && !IsReloading && whip != null && whip.Lash())
            Trigger(LashHash);

        if (input.FireHeld && !IsReloading && Time.time >= nextFireTime)
        {
            if (Loaded > 0) Fire();
            else if (Reserve > 0) TryReload();
            else nextFireTime = Time.time + 0.3f; // empty
        }
    }

    void Fire()
    {
        Loaded--;
        nextFireTime = Time.time + fireInterval;

        Ray aim = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = VenatorPhysics.Raycast(aim, 300f, transform, out RaycastHit hit)
            ? hit.point
            : aim.origin + aim.direction * 300f;
        Vector3 origin = muzzle != null ? muzzle.position : aim.origin;
        Vector3 dir = (target - origin).normalized;

        if (boltPrefab != null)
        {
            Projectile bolt = Instantiate(boltPrefab, origin, Quaternion.LookRotation(dir));
            bolt.Launch(dir * boltSpeed, boltDamage, transform);
        }
        Trigger(FireHash);
    }

    public void TryReload()
    {
        if (IsReloading || Loaded >= magazineSize || Reserve <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        Trigger(ReloadHash);
        yield return new WaitForSeconds(reloadTime);
        int take = Mathf.Min(magazineSize - Loaded, Reserve);
        Loaded += take;
        Reserve -= take;
        IsReloading = false;
    }

    /// <summary>Ammo pickups call this (Week 03).</summary>
    public int AddReserve(int amount)
    {
        int before = Reserve;
        Reserve = Mathf.Min(maxReserve, Reserve + amount);
        return Reserve - before;
    }

    bool AnimReady => viewmodelAnimator != null && viewmodelAnimator.runtimeAnimatorController != null;

    void Trigger(int hash)
    {
        if (AnimReady) viewmodelAnimator.SetTrigger(hash);
    }
}
