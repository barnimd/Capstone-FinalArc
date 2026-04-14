using UnityEngine;

/// <summary>
/// Attach to any sprite object that needs Y-axis depth sorting in a top-down 2D game.
/// Works for single SpriteRenderer OR parent with multiple child SpriteRenderers.
/// Objects lower on screen (smaller Y) render in front of objects higher on screen.
/// </summary>
public class YSort : MonoBehaviour
{
    [Tooltip("Offset the sort point — use negative value to move sort point to feet/base")]
    [SerializeField] private float sortingOffset = 0f;

    private SpriteRenderer[] renderers;

    private void Awake()
    {
        // Grab own SpriteRenderer + all children SpriteRenderers
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        int order = (int)(-(transform.position.y + sortingOffset) * 100);
        foreach (var sr in renderers)
            sr.sortingOrder = order;
    }
}
