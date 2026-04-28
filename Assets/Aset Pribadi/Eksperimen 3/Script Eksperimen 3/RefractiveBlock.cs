using UnityEngine;

/// <summary>
/// RefractiveBlock.cs
/// Attach to each refractive block (Balok 1, Balok 2).
/// The collider MUST be a trigger=false MeshCollider or BoxCollider so
/// the laser can hit both faces (entry and exit).
///
/// Make sure the block has TWO-SIDED collision (entry & exit faces).
/// The easiest approach: set the block's collider as a solid (non-trigger)
/// MeshCollider/BoxCollider and ensure the laser layer mask includes this object's layer.
/// </summary>
public class RefractiveBlock : MonoBehaviour
{
    [Tooltip("Index of refraction for this medium. Air=1.0, Glass≈1.5, Water≈1.33, Diamond≈2.42")]
    [Range(1f, 3f)]
    public float refractiveIndex = 1.5f;

    [Tooltip("Human-readable medium name shown in the angle indicator.")]
    public string mediumName = "Glass";

    // Optional: make the block semi-transparent so the laser is visible inside.
    // Assign a material with a shader that supports transparency (e.g. Standard with Fade mode).
    void Start()
    {
        // Ensure the object has a non-trigger collider for raycasting
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[RefractiveBlock] {gameObject.name} has no Collider! Add a BoxCollider or MeshCollider.");
        }
        else if (col.isTrigger)
        {
            Debug.LogWarning($"[RefractiveBlock] {gameObject.name}'s collider is a trigger. Set isTrigger=false for laser raycasting to work.");
        }
    }
}
