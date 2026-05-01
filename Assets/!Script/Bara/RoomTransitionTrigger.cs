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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        collision.transform.position = spawnPoint.position;

        if (cameraFollow != null)
        {
            float z = cameraFollow.transform.position.z;
            cameraFollow.transform.position = new Vector3(cameraSnapPoint.position.x, cameraSnapPoint.position.y, z);
            cameraFollow.minBounds = newMinBounds;
            cameraFollow.maxBounds = newMaxBounds;
        }
    }
}
