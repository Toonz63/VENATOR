using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Builds the NavMesh for this district when the scene loads, from this object's children only
/// (the greybox geometry), so Grims always have an up-to-date walkable area while the level is
/// still being blocked out. Runs before every other script.
/// </summary>
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshBaker : MonoBehaviour
{
    void Awake()
    {
        var surface = GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }
}
