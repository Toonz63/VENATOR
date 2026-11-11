using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Silver Chain-Whip. A short melee box in front of the camera that damages every enemy inside
/// and interrupts any Grim that is telegraphing an attack.
/// </summary>
public class WhipWeapon : MonoBehaviour
{
    public Transform cam;
    public float range = 3.5f;
    public float width = 1.4f;
    public float damage = 20f;
    public float windup = 0.12f;
    public float cooldown = 0.55f;
    [Tooltip("Draw the hit box in the Scene view while playing.")]
    public bool drawGizmo = true;

    float nextLashTime;
    float gizmoUntil;
    readonly Collider[] hits = new Collider[32];

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>();
            if (c != null) cam = c.transform;
        }
    }

    /// <summary>Returns true if a lash started (used to trigger the animation).</summary>
    public bool Lash()
    {
        if (Time.time < nextLashTime || cam == null) return false;
        nextLashTime = Time.time + cooldown;
        StartCoroutine(LashRoutine());
        return true;
    }

    IEnumerator LashRoutine()
    {
        yield return new WaitForSeconds(windup);
        Vector3 center = cam.position + cam.forward * (range * 0.5f);
        Vector3 half = new Vector3(width * 0.5f, width * 0.5f, range * 0.5f);
        gizmoUntil = Time.time + 0.2f;

        int n = Physics.OverlapBoxNonAlloc(center, half, hits, cam.rotation, ~0, QueryTriggerInteraction.Ignore);
        var done = new HashSet<IDamageable>();
        for (int i = 0; i < n; i++)
        {
            Collider col = hits[i];
            if (col.transform.IsChildOf(transform)) continue;
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null || !done.Add(target)) continue;
            target.TakeDamage(damage, col.bounds.center);
            col.GetComponentInParent<EnemyAI>()?.Interrupt();
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo || cam == null || Time.time > gizmoUntil) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(cam.position + cam.forward * (range * 0.5f), cam.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(width, width, range));
    }
}
