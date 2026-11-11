using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Health for Grims (and later Sovereigns). Flashes white when hit.
/// Below 25% health the enemy is Staggered and glows gold (the Banish finisher hooks in here in Week 03).
/// At 0 health it is Banished: plays the Banish animation, stops colliding, and is removed.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 60f;
    [Range(0f, 1f)] public float staggerFraction = 0.25f;
    public float banishDestroyDelay = 1.2f;
    public Color hitFlashColor = Color.white;
    public Color staggerColor = new Color(1f, 0.75f, 0.2f);

    public float Current { get; private set; }
    public bool IsStaggered { get; private set; }
    public bool IsBanished { get; private set; }

    public event Action<float> OnDamaged;
    public event Action OnBanished;

    Renderer[] renderers;
    MaterialPropertyBlock block;
    Animator anim;
    float flashUntil;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColor = Shader.PropertyToID("_Color");
    static readonly int BanishHash = Animator.StringToHash("Banish");

    void Awake()
    {
        Current = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        anim = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (IsBanished || amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount);
        flashUntil = Time.time + 0.08f;

        if (Current <= 0f) Banish();
        else if (!IsStaggered && Current <= maxHealth * staggerFraction) IsStaggered = true;
        RefreshTint();
    }

    void Update()
    {
        if (flashUntil > 0f && Time.time >= flashUntil)
        {
            flashUntil = 0f;
            RefreshTint();
        }
    }

    void RefreshTint()
    {
        bool flashing = Time.time < flashUntil;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!flashing && !IsStaggered) { r.SetPropertyBlock(null); continue; }
            Color c = flashing ? hitFlashColor : staggerColor;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, c);
            block.SetColor(LegacyColor, c);
            r.SetPropertyBlock(block);
        }
    }

    /// <summary>Removes the enemy. Called at 0 health, and by the Banish finisher in Week 03.</summary>
    public void Banish()
    {
        if (IsBanished) return;
        IsBanished = true;
        IsStaggered = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger(BanishHash);
        else StartCoroutine(ShrinkAway());
        OnBanished?.Invoke();
        Destroy(gameObject, banishDestroyDelay);
    }

    IEnumerator ShrinkAway() // used when the enemy has no Animator (placeholder capsule)
    {
        Vector3 start = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / (banishDestroyDelay * 0.9f))
        {
            transform.localScale = Vector3.Lerp(start, Vector3.one * 0.01f, t);
            yield return null;
        }
    }
}
