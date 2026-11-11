using UnityEngine;

/// <summary>Anything that can take damage (player, Grims, later Sovereigns).</summary>
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint);
}

/// <summary>Physics helpers that skip the colliders belonging to one object (usually the shooter).</summary>
public static class VenatorPhysics
{
    static readonly RaycastHit[] buffer = new RaycastHit[32];

    static bool Closest(int count, Transform ignoreRoot, out RaycastHit best)
    {
        best = default;
        float bestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            var h = buffer[i];
            if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
            if (h.distance < bestDist) { bestDist = h.distance; best = h; found = true; }
        }
        return found;
    }

    public static bool Raycast(Ray ray, float maxDistance, Transform ignoreRoot, out RaycastHit hit)
    {
        int n = Physics.RaycastNonAlloc(ray, buffer, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        return Closest(n, ignoreRoot, out hit);
    }

    public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, Transform ignoreRoot, out RaycastHit hit)
    {
        int n = Physics.SphereCastNonAlloc(origin, radius, direction, buffer, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        return Closest(n, ignoreRoot, out hit);
    }
}
