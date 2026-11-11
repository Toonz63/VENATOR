using UnityEngine;

/// <summary>
/// A Light Bolt. Moves in a straight line and sphere-casts each frame along its path,
/// so even very fast bolts never tunnel through thin walls or enemies.
/// Ignores the colliders of whoever fired it.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float radius = 0.08f;
    public float lifetime = 3f;
    public GameObject impactEffect;

    Vector3 velocity;
    float damage;
    Transform owner;
    float dieAt;
    bool launched;

    public void Launch(Vector3 initialVelocity, float hitDamage, Transform shooter)
    {
        velocity = initialVelocity;
        damage = hitDamage;
        owner = shooter;
        dieAt = Time.time + lifetime;
        launched = true;
    }

    void Update()
    {
        if (!launched) return;
        if (Time.time >= dieAt) { Destroy(gameObject); return; }

        Vector3 step = velocity * Time.deltaTime;
        float dist = step.magnitude;
        if (dist <= 0f) return;

        if (VenatorPhysics.SphereCast(transform.position, radius, step / dist, dist, owner, out RaycastHit hit))
        {
            var target = hit.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(damage, hit.point);
            if (impactEffect != null) Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(gameObject);
            return;
        }
        transform.position += step;
    }
}
