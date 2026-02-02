using UnityEngine;

/// <summary>
/// Trigger component for story/guidance text display.
/// Attach to a GameObject with a Collider (set as trigger) to trigger stories
/// when the player enters the area.
/// 
/// Setup:
/// 1. Add a Collider component (e.g., BoxCollider, SphereCollider)
/// 2. Enable "Is Trigger" on the collider
/// 3. Set the storyId to match a key in StoryManager's storyData
/// 4. Optionally enable triggerOnce for one-time triggers
/// </summary>
[RequireComponent(typeof(Collider))]
public class StoryTrigger : MonoBehaviour
{
    [Header("Story Settings")]
    [Tooltip("The story ID to play (e.g., 'NearTree', 'EnterRoomA', 'NearBoss')")]
    public string storyId = "";

    [Tooltip("Only trigger once per game session")]
    public bool triggerOnce = true;

    [Tooltip("Tag of the object that can trigger this (usually 'Player')")]
    public string triggerTag = "Player";

    [Header("Optional Settings")]
    [Tooltip("Delay before playing the story after trigger")]
    public float triggerDelay = 0f;

    [Tooltip("Optional: Disable the trigger after playing")]
    public bool disableAfterTrigger = false;

    // Internal state
    private bool hasTriggered = false;

    private void Start()
    {
        // Ensure collider is set as trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"StoryTrigger on '{gameObject.name}': Collider should be set as trigger!");
        }

        if (string.IsNullOrEmpty(storyId))
        {
            Debug.LogWarning($"StoryTrigger on '{gameObject.name}': No storyId assigned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if already triggered (for one-time triggers)
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        // Check tag
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
        {
            // Also check parent for XR setups where the collider might be on a child
            if (other.transform.parent == null || !other.transform.parent.CompareTag(triggerTag))
            {
                return;
            }
        }

        // Check if StoryManager exists
        if (StoryManager.Instance == null)
        {
            Debug.LogWarning($"StoryTrigger: StoryManager.Instance is null!");
            return;
        }

        // Check if this story was already played via StoryManager tracking
        if (triggerOnce && StoryManager.Instance.HasPlayed(storyId))
        {
            hasTriggered = true;
            return;
        }

        // Don't interrupt if another story is playing
        if (StoryManager.Instance.IsPlaying)
        {
            return;
        }

        hasTriggered = true;

        Debug.Log($"StoryTrigger: Player entered trigger '{gameObject.name}', playing story '{storyId}'");

        if (triggerDelay > 0f)
        {
            StartCoroutine(PlayStoryWithDelay());
        }
        else
        {
            StoryManager.Instance.PlayStory(storyId, OnStoryComplete);
        }
    }

    private System.Collections.IEnumerator PlayStoryWithDelay()
    {
        yield return new WaitForSeconds(triggerDelay);
        
        if (StoryManager.Instance != null && !StoryManager.Instance.IsPlaying)
        {
            StoryManager.Instance.PlayStory(storyId, OnStoryComplete);
        }
    }

    private void OnStoryComplete()
    {
        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reset the trigger (e.g., when restarting the level)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    /// <summary>
    /// Manually trigger the story (for event-based triggers)
    /// </summary>
    public void TriggerStory()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (StoryManager.Instance == null)
        {
            Debug.LogWarning($"StoryTrigger: StoryManager.Instance is null!");
            return;
        }

        if (triggerOnce && StoryManager.Instance.HasPlayed(storyId))
        {
            hasTriggered = true;
            return;
        }

        hasTriggered = true;
        StoryManager.Instance.PlayStory(storyId, OnStoryComplete);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw a visual indicator in the editor
        Gizmos.color = hasTriggered ? Color.gray : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the story ID when selected
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Story: {storyId}");
    }
#endif
}
