using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private RoundManager roundManager;

    private void Start()
    {
        // Find RoundManager in the scene
        roundManager = FindObjectOfType<RoundManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Player hit by Enemy!");
            roundManager.StartNextRound();
        }
    }

    // If using trigger colliders instead:
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Player touched Enemy (trigger)!");
            roundManager.StartNextRound();
        }
    }
}
