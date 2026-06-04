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
    public enum ArrowDisplayMode
    {
        FixedHud,
        ScreenEdge
    }

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

    [Header("Display Mode")]
    public ArrowDisplayMode displayMode = ArrowDisplayMode.FixedHud;
    [Tooltip("Distance in pixels kept between a screen-edge arrow and the edge of the canvas.")]
    public float screenEdgeMargin = 52f;
    [Tooltip("Hide the screen-edge arrow while the target is already visible.")]
    public bool hideWhenTargetVisible = true;
    [Tooltip("Hide the screen-edge arrow when the player is this close to the target. Set to 0 to disable.")]
    public float hideWithinWorldDistance = 1.5f;

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
    private bool          _requestedVisible;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (arrowImage == null) arrowImage = GetComponent<Image>();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        ResolveSceneReferences();
        _basePosition = _rect.anchoredPosition;
    }

    private void LateUpdate()
    {
        if (!_requestedVisible || target == null) return;
        ResolveSceneReferences();
        if (_cam == null) return;

        if (displayMode == ArrowDisplayMode.ScreenEdge)
            UpdateScreenEdgeArrow();
        else
            RotateTowardTarget();
    }

    /// <summary>Show the arrow and point it toward <paramref name="newTarget"/>.</summary>
    public void Show(Transform newTarget)
    {
        target = newTarget;
        _requestedVisible = target != null;

        // Ensure the Canvas (and whole hierarchy) is active — StartCoroutine
        // requires activeInHierarchy == true on the calling MonoBehaviour.
        Canvas rootCanvas = GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(true);

        gameObject.SetActive(true);
        ResolveSceneReferences();

        if (arrowImage != null)
            arrowImage.enabled = true;

        if (displayMode == ArrowDisplayMode.ScreenEdge)
        {
            ConfigureScreenEdgeRect();
            // The ObjectiveCanvas can still have its compact objective-panel
            // dimensions during the frame it is reactivated. Wait for the next
            // LateUpdate so the arrow never flashes at a wrong position.
            if (arrowImage != null)
                arrowImage.enabled = false;
        }
        else
        {
            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceRoutine());
        }
    }

    /// <summary>Show the arrow using the already-assigned <see cref="target"/>.</summary>
    public void Show() => Show(target);

    /// <summary>Hide the arrow.</summary>
    public void Hide()
    {
        _requestedVisible = false;
        if (_bounceRoutine != null) { StopCoroutine(_bounceRoutine); _bounceRoutine = null; }
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    private void ResolveSceneReferences()
    {
        if (_cam == null)
            _cam = Camera.main;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void ConfigureScreenEdgeRect()
    {
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void UpdateScreenEdgeArrow()
    {
        Vector3 targetScreen3 = _cam.WorldToScreenPoint(target.position);
        Vector3 viewport = _cam.WorldToViewportPoint(target.position);
        bool targetVisible = viewport.z > 0f
                             && viewport.x >= 0f && viewport.x <= 1f
                             && viewport.y >= 0f && viewport.y <= 1f;
        bool playerNear = player != null && hideWithinWorldDistance > 0f
                          && Vector2.Distance(player.position, target.position) <= hideWithinWorldDistance;

        if (arrowImage != null)
            arrowImage.enabled = !(playerNear || (hideWhenTargetVisible && targetVisible));

        if (playerNear || (hideWhenTargetVisible && targetVisible))
            return;

        // Screen-edge indicators should point from the visible screen centre,
        // not from the player's on-screen position. The player can be offset
        // while the camera is clamped at a room boundary.
        Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        if (targetScreen3.z < 0f)
            direction = -direction;
        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect != null)
        {
            Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
            Vector2 halfArrow = _rect.rect.size * 0.5f;
            Vector2 usable = new Vector2(
                Mathf.Max(1f, halfCanvas.x - screenEdgeMargin - halfArrow.x),
                Mathf.Max(1f, halfCanvas.y - screenEdgeMargin - halfArrow.y));
            float scaleX = Mathf.Abs(direction.x) > 0.001f
                ? usable.x / Mathf.Abs(direction.x)
                : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.001f
                ? usable.y / Mathf.Abs(direction.y)
                : float.MaxValue;
            _rect.anchoredPosition = direction * Mathf.Min(scaleX, scaleY);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + rotationOffset;
        _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

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
