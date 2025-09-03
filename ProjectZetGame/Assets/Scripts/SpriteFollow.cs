using UnityEngine;

public class SpriteFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform targetToFollow; // The sprite to follow
    [SerializeField] private float followSpeed = 0.1f; // How fast to follow
    [SerializeField] private Vector3 offset = Vector3.zero; // Offset from target position
    [SerializeField] private bool smoothFollow = true; // Whether to use smooth following
    
    [Header("Advanced Settings")]
    [SerializeField] private float maxDistance = 10f; // Maximum distance to follow
    [SerializeField] private bool maintainZPosition = true; // Keep original Z position
    
    private Vector3 originalZPosition;
    
    void Start()
    {
        // Store the original Z position if we want to maintain it
        if (maintainZPosition)
        {
            originalZPosition = transform.position;
        }
        
        // Check if target is assigned
        if (targetToFollow == null)
        {
            Debug.LogWarning("SpriteFollow: No target assigned! Please assign a target in the inspector.");
        }
    }

    void Update()
    {
        if (targetToFollow == null) return;
        
        // Calculate target position with offset
        Vector3 targetPosition = targetToFollow.position + offset;
        
        // Maintain Z position if specified
        if (maintainZPosition)
        {
            targetPosition.z = originalZPosition.z;
        }
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Only follow if within max distance
        if (distanceToTarget <= maxDistance)
        {
            if (smoothFollow)
            {
                // Smooth following using Lerp
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            }
            else
            {
                // Direct following
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.deltaTime);
            }
        }
    }
    
    // Public method to change target at runtime
    public void SetTarget(Transform newTarget)
    {
        targetToFollow = newTarget;
    }
    
    // Public method to change follow speed at runtime
    public void SetFollowSpeed(float newSpeed)
    {
        followSpeed = newSpeed;
    }
    
    // Public method to change offset at runtime
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
}
