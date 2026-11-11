using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The Venator's Vitality (health) and Tonics.
/// Vitality never regenerates on its own. Tonics (H key for now, Arsenal Wheel later) restore 40%.
/// At 0 Vitality the Venator respawns at the respawn point (Wardlights will set this in Week 03).
/// </summary>
public class HealthSystem : MonoBehaviour, IDamageable
{
    public float maxVitality = 100f;
    [Tooltip("Seconds of invulnerability after taking a hit.")]
    public float invulnerableTime = 0.5f;
    public int maxTonics = 3;
    [Range(0f, 1f)] public float tonicHealFraction = 0.4f;
    public float respawnDelay = 1.5f;
    [Tooltip("Week 01 test keys: K = take 10 damage.")]
    public bool debugKeys = true;

    public float Current { get; private set; }
    public int Tonics { get; private set; }
    public bool IsDead { get; private set; }
    public float LastHitTime { get; private set; } = -10f;

    /// <summary>(current, max)</summary>
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnRespawn;

    Vector3 respawnPosition;
    Quaternion respawnRotation;
    float invulnerableUntil;
    FirstPersonController controller;
    VenatorInput input;

    void Awake()
    {
        Current = maxVitality;
        Tonics = maxTonics;
        controller = GetComponent<FirstPersonController>();
        input = GetComponent<VenatorInput>();
        SetRespawnPoint(transform.position, transform.rotation);
    }

    void Update()
    {
        if (IsDead) return;
        if (input != null && input.TonicPressed) UseTonic();
        if (debugKeys && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
            TakeDamage(10f, transform.position);
    }

    public void SetRespawnPoint(Vector3 position, Quaternion rotation)
    {
        respawnPosition = position;
        respawnRotation = rotation;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (IsDead || amount <= 0f || Time.time < invulnerableUntil) return;
        Current = Mathf.Max(0f, Current - amount);
        invulnerableUntil = Time.time + invulnerableTime;
        LastHitTime = Time.time;
        OnHealthChanged?.Invoke(Current, maxVitality);
        if (Current <= 0f) StartCoroutine(DieAndRespawn());
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(maxVitality, Current + amount);
        OnHealthChanged?.Invoke(Current, maxVitality);
    }

    public bool UseTonic()
    {
        if (IsDead || Tonics <= 0 || Current >= maxVitality) return false;
        Tonics--;
        Heal(maxVitality * tonicHealFraction);
        return true;
    }

    /// <summary>Wardlights call this in Week 03.</summary>
    public void RefillAll()
    {
        Tonics = maxTonics;
        Current = maxVitality;
        OnHealthChanged?.Invoke(Current, maxVitality);
    }

    IEnumerator DieAndRespawn()
    {
        IsDead = true;
        OnDeath?.Invoke();
        yield return new WaitForSeconds(respawnDelay);
        if (controller != null) controller.Teleport(respawnPosition, respawnRotation);
        else transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        IsDead = false;
        RefillAll();
        OnRespawn?.Invoke();
    }
}
