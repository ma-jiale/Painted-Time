using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tree that can be chopped down with an axe.
/// When hit, the tree falls with realistic physics-like animation.
/// 
/// Purpose: Creates a choppable tree for bridge/crossing puzzles.
/// Attach to tree objects that player can cut down.
/// </summary>
public class ChoppableTree : MonoBehaviour
{
    [Header("Chopping Settings")]
    [SerializeField, Tooltip("Tag of the axe object")]
    private string axeTag = "Axe";
    
    [SerializeField, Tooltip("Collider to detect axe hits (if empty, uses this object or children)")]
    private Collider hitCollider;
    
    [SerializeField, Tooltip("Number of hits required to chop down")]
    private int hitsRequired = 1;
    
    [SerializeField, Tooltip("Cooldown between hits (seconds)")]
    private float hitCooldown = 0.5f;
    
    [Header("Falling Settings")]
    [SerializeField, Tooltip("Direction to fall (local space)")]
    private Vector3 fallDirection = Vector3.right; // Falls along X axis
    
    [SerializeField, Tooltip("Final rotation angle when fallen")]
    private float fallAngle = -90f;
    
    [SerializeField, Tooltip("Time to complete the fall")]
    private float fallDuration = 1.5f;
    
    [SerializeField, Tooltip("Bounce intensity when hitting ground (0 = no bounce)")]
    [Range(0f, 1f)]
    private float bounceIntensity = 0.15f;
    
    [SerializeField, Tooltip("Number of bounces")]
    private int bounceCount = 2;
    
    [Header("Sound Effects (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chopSound;
    [SerializeField] private AudioClip fallSound;
    [SerializeField] private AudioClip impactSound;
    
    [Header("Visual Effects (Optional)")]
    [SerializeField, Tooltip("Particle effect when chopped")]
    private ParticleSystem chopEffect;
    
    [SerializeField, Tooltip("Particle effect when hitting ground")]
    private ParticleSystem impactEffect;
    
    [SerializeField, Tooltip("Tree shake amount when hit")]
    private float shakeIntensity = 0.1f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onChopped;
    [SerializeField] private UnityEvent onFallStarted;
    [SerializeField] private UnityEvent onFallComplete;
    
    // Internal state
    private int currentHits = 0;
    private bool isFalling = false;
    private bool hasFallen = false;
    private float lastHitTime = -999f;
    
    // Fall animation state
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private float fallTimer = 0f;
    
    // Shake state
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    
    /// <summary>
    /// Has the tree been chopped down?
    /// </summary>
    public bool HasFallen => hasFallen;
    
    /// <summary>
    /// Is the tree currently falling?
    /// </summary>
    public bool IsFalling => isFalling;
    
    private void Awake()
    {
        initialRotation = transform.rotation;
        originalPosition = transform.position;
        
        // Calculate target rotation based on fall direction
        Vector3 fallAxis = fallDirection.normalized;
        targetRotation = initialRotation * Quaternion.AngleAxis(fallAngle, fallAxis);
        
        // Setup collision detection helper if collider is on child object
        SetupCollisionDetection();
    }
    
    /// <summary>
    /// Setup collision detection on child collider if needed
    /// </summary>
    private void SetupCollisionDetection()
    {
        // If no collider specified, try to find one
        if (hitCollider == null)
        {
            hitCollider = GetComponentInChildren<Collider>();
        }
        
        // If collider is on a different object, add a helper component
        if (hitCollider != null && hitCollider.gameObject != gameObject)
        {
            var helper = hitCollider.gameObject.GetComponent<ChoppableTreeCollisionHelper>();
            if (helper == null)
            {
                helper = hitCollider.gameObject.AddComponent<ChoppableTreeCollisionHelper>();
            }
            helper.Initialize(this, axeTag);
        }
    }
    
    private void Update()
    {
        // Handle tree shake effect
        if (shakeTimer > 0f)
        {
            UpdateShake();
        }
        
        // Handle falling animation
        if (isFalling)
        {
            UpdateFall();
        }
    }
    
    /// <summary>
    /// Detect collision with axe
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasFallen || isFalling) return;
        
        if (other.CompareTag(axeTag))
        {
            TryChop();
        }
    }
    
    /// <summary>
    /// Alternative: Detect collision (non-trigger)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (hasFallen || isFalling) return;
        
        if (collision.collider.CompareTag(axeTag))
        {
            TryChop();
        }
    }
    
    /// <summary>
    /// Attempt to chop the tree
    /// </summary>
    public void TryChop()
    {
        if (hasFallen || isFalling) return;
        
        // Check cooldown
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;
        
        currentHits++;
        
        // Play chop effects
        PlayChopEffects();
        
        // Start shake
        shakeTimer = 0.3f;
        
        // Invoke chop event
        onChopped?.Invoke();
        
        Debug.Log($"[{gameObject.name}] Chopped! ({currentHits}/{hitsRequired})");
        
        // Check if enough hits to fall
        if (currentHits >= hitsRequired)
        {
            StartFalling();
        }
    }
    
    /// <summary>
    /// Start the falling animation
    /// </summary>
    private void StartFalling()
    {
        if (isFalling || hasFallen) return;
        
        isFalling = true;
        fallTimer = 0f;
        
        // Play fall sound
        if (audioSource != null && fallSound != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
        
        onFallStarted?.Invoke();
        Debug.Log($"[{gameObject.name}] Timber! Tree is falling!");
    }
    
    /// <summary>
    /// Update the falling animation with realistic physics feel
    /// </summary>
    private void UpdateFall()
    {
        fallTimer += Time.deltaTime;
        float normalizedTime = fallTimer / fallDuration;
        
        if (normalizedTime >= 1f)
        {
            // Fall complete
            transform.rotation = targetRotation;
            isFalling = false;
            hasFallen = true;
            
            // Play impact effects
            PlayImpactEffects();
            
            onFallComplete?.Invoke();
            Debug.Log($"[{gameObject.name}] Tree has fallen!");
            return;
        }
        
        // Calculate rotation with realistic physics curve
        float fallProgress = CalculateFallCurve(normalizedTime);
        
        // Apply rotation
        transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, fallProgress);
    }
    
    /// <summary>
    /// Calculate the fall curve with acceleration and bounce
    /// Creates a realistic "timber!" feeling
    /// </summary>
    private float CalculateFallCurve(float t)
    {
        // Phase 1: Main fall with gravity acceleration (0 to ~0.7)
        // Phase 2: Impact and bounces (~0.7 to 1.0)
        
        float mainFallEnd = 0.7f;
        
        if (t < mainFallEnd)
        {
            // Ease-in curve (simulates gravity acceleration)
            // Starts slow, accelerates as it falls
            float fallT = t / mainFallEnd;
            
            // Quadratic ease-in for gravity feel
            // y = t^2 gives acceleration effect
            return fallT * fallT;
        }
        else
        {
            // Bounce phase
            float bounceT = (t - mainFallEnd) / (1f - mainFallEnd);
            
            // Start at 1.0 (fully fallen)
            float baseValue = 1f;
            
            // Add bounces (decaying sine wave)
            if (bounceIntensity > 0f && bounceCount > 0)
            {
                float frequency = bounceCount * Mathf.PI;
                float decay = 1f - bounceT; // Decay over time
                float bounce = Mathf.Sin(bounceT * frequency) * bounceIntensity * decay;
                
                // Bounce goes slightly past target and back
                baseValue = 1f - Mathf.Abs(bounce);
            }
            
            return Mathf.Clamp01(baseValue);
        }
    }
    
    /// <summary>
    /// Update shake effect when hit
    /// </summary>
    private void UpdateShake()
    {
        shakeTimer -= Time.deltaTime;
        
        if (shakeTimer > 0f)
        {
            // Random shake offset
            float intensity = shakeIntensity * (shakeTimer / 0.3f);
            Vector3 shakeOffset = new Vector3(
                Random.Range(-intensity, intensity),
                0f,
                Random.Range(-intensity, intensity)
            );
            transform.position = originalPosition + shakeOffset;
        }
        else
        {
            // Reset position
            transform.position = originalPosition;
        }
    }
    
    /// <summary>
    /// Play chopping effects
    /// </summary>
    private void PlayChopEffects()
    {
        if (audioSource != null && chopSound != null)
        {
            audioSource.PlayOneShot(chopSound);
        }
        
        if (chopEffect != null)
        {
            chopEffect.Play();
        }
    }
    
    /// <summary>
    /// Play impact effects when tree hits ground
    /// </summary>
    private void PlayImpactEffects()
    {
        if (audioSource != null && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
        
        if (impactEffect != null)
        {
            impactEffect.Play();
        }
    }
    
    /// <summary>
    /// Set the fall direction at runtime
    /// </summary>
    public void SetFallDirection(Vector3 direction)
    {
        fallDirection = direction.normalized;
        targetRotation = initialRotation * Quaternion.AngleAxis(fallAngle, fallDirection);
    }
    
    /// <summary>
    /// Force the tree to fall (for testing or scripted events)
    /// </summary>
    [ContextMenu("Force Fall")]
    public void ForceFall()
    {
        currentHits = hitsRequired;
        StartFalling();
    }
    
    /// <summary>
    /// Reset the tree to standing position
    /// </summary>
    [ContextMenu("Reset Tree")]
    public void ResetTree()
    {
        transform.rotation = initialRotation;
        transform.position = originalPosition;
        currentHits = 0;
        isFalling = false;
        hasFallen = false;
        fallTimer = 0f;
        shakeTimer = 0f;
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Show fall direction
        Gizmos.color = Color.red;
        Vector3 fallEnd = transform.position + fallDirection.normalized * 3f;
        Gizmos.DrawLine(transform.position, fallEnd);
        Gizmos.DrawWireSphere(fallEnd, 0.2f);
        
        // Show fallen position preview
        if (!Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Quaternion previewRot = transform.rotation * Quaternion.AngleAxis(fallAngle, fallDirection.normalized);
            
            // Draw approximate fallen tree position
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, previewRot, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.up * 2f, new Vector3(0.5f, 4f, 0.5f));
            Gizmos.matrix = oldMatrix;
        }
    }
}

/// <summary>
/// Helper component to forward collision events from child collider to parent ChoppableTree.
/// This is automatically added at runtime, do not add manually.
/// </summary>
public class ChoppableTreeCollisionHelper : MonoBehaviour
{
    private ChoppableTree parentTree;
    private string axeTag;
    
    public void Initialize(ChoppableTree tree, string tag)
    {
        parentTree = tree;
        axeTag = tag;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (parentTree == null) return;
        
        if (other.CompareTag(axeTag))
        {
            parentTree.TryChop();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (parentTree == null) return;
        
        if (collision.collider.CompareTag(axeTag))
        {
            parentTree.TryChop();
        }
    }
}
