using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI follow-arrow for Topic 1.  
/// Attach to the arrow Image GameObject inside ObjectiveCanvas.
///
/// The arrow rotates to point from the player (or screen centre when the player
/// is not assigned) toward a world-space <see cref="target"/> every frame.
/// It also plays a small bounce animation when first shown to draw attention.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FollowArrow_Tp1 : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The world-space Transform the arrow should point toward (e.g. RightWallTrigger).")]
    public Transform target;

    [Header("Optional — Player")]
    [Tooltip("Player Transform used as the 'from' point. If empty the screen centre is used.")]
    public Transform player;

    [Header("Arrow Settings")]
    [Tooltip("The Image component of the arrow graphic. Leave empty to use this GameObject's Image.")]
    public Image arrowImage;

    [Tooltip("Degrees to add so the arrow sprite's 'forward' direction aligns with Up (default = 0 for a sprite that naturally points upward).")]
    public float rotationOffset = 0f;

    [Header("Bounce Animation")]
    [Tooltip("Seconds for one full bounce cycle when the arrow first appears.")]
    public float bounceDuration  = 0.4f;
    [Tooltip("Number of bounces played on Show().")]
    public int   bounceCount     = 3;
    [Tooltip("Max pixel offset of the bounce.")]
    public float bounceAmplitude = 8f;

    // ---- runtime ----
    private RectTransform _rect;
    private Camera        _cam;
    private Coroutine     _bounceRoutine;
    private Vector2       _basePosition;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (arrowImage == null) arrowImage = GetComponent<Image>();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        _cam = Camera.main;
        _basePosition = _rect.anchoredPosition;
    }

    private void LateUpdate()
    {
        if (target == null || _cam == null) return;
        RotateTowardTarget();
    }

    /// <summary>Show the arrow and point it toward <paramref name="newTarget"/>.</summary>
    public void Show(Transform newTarget)
    {
        target = newTarget;

        // Ensure the Canvas (and whole hierarchy) is active — StartCoroutine
        // requires activeInHierarchy == true on the calling MonoBehaviour.
        Canvas rootCanvas = GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(true);

        gameObject.SetActive(true);

        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(BounceRoutine());
    }

    /// <summary>Show the arrow using the already-assigned <see cref="target"/>.</summary>
    public void Show() => Show(target);

    /// <summary>Hide the arrow.</summary>
    public void Hide()
    {
        if (_bounceRoutine != null) { StopCoroutine(_bounceRoutine); _bounceRoutine = null; }
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    private void RotateTowardTarget()
    {
        // World position -> screen position
        Vector3 targetScreen = _cam.WorldToScreenPoint(target.position);

        // Origin: player screen position or screen centre
        Vector3 originScreen;
        if (player != null)
            originScreen = _cam.WorldToScreenPoint(player.position);
        else
            originScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Vector2 dir = new Vector2(targetScreen.x - originScreen.x,
                                  targetScreen.y - originScreen.y);

        if (dir.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + rotationOffset;
        _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator BounceRoutine()
    {
        float totalTime = bounceDuration * bounceCount;
        float elapsed   = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float t      = (elapsed % bounceDuration) / bounceDuration;
            float offset = Mathf.Sin(t * Mathf.PI) * bounceAmplitude;

            // Offset along local "up" of the arrow (its pointing direction)
            Vector2 localUp = _rect.up;
            _rect.anchoredPosition = _basePosition + localUp * offset;

            yield return null;
        }

        _rect.anchoredPosition = _basePosition;
        _bounceRoutine = null;
    }
}
