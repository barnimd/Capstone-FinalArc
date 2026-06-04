using UnityEngine;

public class RoomTransitionTrigger : MonoBehaviour
{
    [Header("Player Spawn")]
    public Transform spawnPoint; // titik spawn di Kitchen

    [Header("Camera")]
    public CameraFollow cameraFollow;
    public Transform cameraSnapPoint;
    public Vector2 newMinBounds;
    public Vector2 newMaxBounds;

    [Header("Transition Safety")]
    public float retriggerDelay = 0.35f;

    private float nextAllowedTransitionTime;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || spawnPoint == null)
            return;

        if (Time.unscaledTime < nextAllowedTransitionTime)
            return;

        nextAllowedTransitionTime = Time.unscaledTime + retriggerDelay;

        Rigidbody2D playerRb = collision.GetComponentInParent<Rigidbody2D>();
        Transform playerBody = playerRb != null ? playerRb.transform : collision.transform;

        if (playerRb != null)
            playerRb.velocity = Vector2.zero;
        MovePlayerHierarchy(playerBody, spawnPoint.position);

        if (cameraFollow != null)
        {
            float z = cameraFollow.transform.position.z;
            Vector3 snapPosition = cameraSnapPoint != null ? cameraSnapPoint.position : spawnPoint.position;
            cameraFollow.transform.position = new Vector3(snapPosition.x, snapPosition.y, z);
            cameraFollow.minBounds = newMinBounds;
            cameraFollow.maxBounds = newMaxBounds;
        }
    }

    private static void MovePlayerHierarchy(Transform playerBody, Vector3 destination)
    {
        Transform playerRoot = playerBody;
        while (playerRoot.parent != null && playerRoot.parent.CompareTag("Player"))
            playerRoot = playerRoot.parent;

        playerRoot.position += destination - playerBody.position;
        Physics2D.SyncTransforms();
    }
}
