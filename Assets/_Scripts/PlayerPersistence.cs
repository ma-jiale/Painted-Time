using UnityEngine;

/// <summary>
/// Makes the player (XR Origin) persist across scene changes.
/// Attach this script to the XR Origin root object.
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;

    private void Awake()
    {
        // Check if an instance already exists
        if (instance == null)
        {
            // This is the first instance, make it persistent
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Player marked as persistent across scenes");
        }
        else
        {
            // An instance already exists, destroy this duplicate
            Debug.Log("Duplicate player found, destroying...");
            Destroy(gameObject);
        }
    }
}
