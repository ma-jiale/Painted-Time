using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Makes the XRInteractionManager persist across scene changes.
/// Attach this script to the same GameObject that has XRInteractionManager.
/// This ensures that the XR Origin's interactors always have a valid manager
/// to register with after scene transitions.
/// </summary>
[RequireComponent(typeof(XRInteractionManager))]
public class XRInteractionManagerPersistence : MonoBehaviour
{
    private static XRInteractionManagerPersistence instance;

    private void Awake()
    {
        // Check if an instance already exists
        if (instance == null)
        {
            // This is the first instance, make it persistent
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("XRInteractionManager marked as persistent across scenes");
        }
        else
        {
            // An instance already exists, destroy this duplicate
            Debug.Log("Duplicate XRInteractionManager found, destroying...");
            Destroy(gameObject);
        }
    }
}
