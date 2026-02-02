using UnityEngine;

/// <summary>
/// Detects when the player or items fall into water and triggers respawn.
/// Attach this to a trigger collider that covers the water area.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterDetector : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Tag to identify the player")]
    public string playerTag = "Player";

    [Header("Axe Settings")]
    [Tooltip("Tag to identify the axe (can be on parent object)")]
    public string axeTag = "Axe";
    
    [Tooltip("Tag used to find respawn points for the axe")]
    public string respawnPointTag = "RespawnPoint";

    private void Awake()
    {
        // Ensure this is a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("WaterDetector collider was not set as trigger. Fixed automatically.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if player fell into water
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player fell into water!");
            
            // Call respawn through GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnPlayer();
            }
            return;
        }

        // Check if axe fell into water (collider might be on child, so check parent too)
        GameObject axeObject = FindAxeObject(other.gameObject);
        if (axeObject != null)
        {
            Debug.Log("Axe fell into water! Respawning...");
            RespawnAxe(axeObject);
        }
    }

    /// <summary>
    /// Finds the axe object by checking the collider object and its parents for the axe tag
    /// </summary>
    /// <param name="colliderObject">The object that triggered the collision</param>
    /// <returns>The axe GameObject if found, null otherwise</returns>
    private GameObject FindAxeObject(GameObject colliderObject)
    {
        // Check if the collider object itself has the axe tag
        if (colliderObject.CompareTag(axeTag))
        {
            return colliderObject;
        }

        // Check parent objects for the axe tag (since collider is on child)
        Transform parent = colliderObject.transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag(axeTag))
            {
                return parent.gameObject;
            }
            parent = parent.parent;
        }

        return null;
    }

    /// <summary>
    /// Respawns the axe at the closest respawn point
    /// </summary>
    /// <param name="axe">The axe GameObject to respawn</param>
    private void RespawnAxe(GameObject axe)
    {
        // Find closest respawn point
        Transform respawnPoint = FindClosestRespawnPoint(axe.transform.position);

        if (respawnPoint == null)
        {
            Debug.LogWarning("No respawn point found for axe! Cannot respawn.");
            return;
        }

        // Reset physics before moving
        Rigidbody rb = axe.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move axe to respawn point (slightly above to avoid clipping)
        axe.transform.position = respawnPoint.position + Vector3.up * 0.5f;
        axe.transform.rotation = respawnPoint.rotation;

        Debug.Log($"Axe respawned at: {respawnPoint.name}");
    }

    /// <summary>
    /// Finds the closest respawn point to the given position
    /// </summary>
    /// <param name="fromPosition">Position to measure distance from</param>
    /// <returns>Transform of the closest respawn point, or null if none found</returns>
    private Transform FindClosestRespawnPoint(Vector3 fromPosition)
    {
        GameObject[] respawnPoints = GameObject.FindGameObjectsWithTag(respawnPointTag);

        if (respawnPoints.Length == 0)
        {
            return null;
        }

        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject point in respawnPoints)
        {
            float distance = Vector3.Distance(fromPosition, point.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point.transform;
            }
        }

        return closest;
    }
}