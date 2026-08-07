using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI objective marker, shared by Topic 1 / 3 / 4 / 5 / 6.
/// Attach to the marker Image GameObject inside ObjectiveCanvas.
///
/// Three runtime states (ScreenEdge display mode):
///   • NearMarker  — player is close and the objective is on screen, so the
///                   "!" sprite floats above the objective's head.
///   • PinnedArrow — objective is on screen but still far, so the arrow sits
///                   above it and points down at it.
///   • EdgeArrow   — objective is off screen, so the arrow slides to the screen
///                   edge and rotates toward it, shrinking with distance.
///
/// Every new field defaults to the old behaviour, so existing scenes keep
/// working until the new sprites / profiles are assigned in the Inspector.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FollowArrow_Tp1 : MonoBehaviour
{
    public enum ArrowDisplayMode
    {
        FixedHud,
        ScreenEdge
    }

    private enum MarkerState
    {
        None,
        EdgeArrow,
        PinnedArrow,
        NearMarker
    }

    /// <summary>Which way the arrow artwork points before any rotation is applied.</summary>
    public enum SpriteFacing
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>Per-profile version of <see cref="SpriteFacing"/> with an inherit option.</summary>
    public enum SpriteFacingOverride
    {
        Inherit,
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Per-objective override. Lets each target carry its own head-top anchor
    /// and its own icon pair without needing a second script.
    /// </summary>
    [System.Serializable]
    public class ObjectiveMarkerProfile
    {
        [Tooltip("Inspector label only. Not used at runtime.")]
        public string label;

        [Tooltip("The objective this profile belongs to. Matched against the Transform passed to Show(). A parent or child of the target also matches.")]
        public Transform target;

        [Tooltip("Optional. An empty GameObject parented above the objective's head. When set it wins over bounds + height offset — easiest way to hand-place the marker.")]
        public Transform anchorOverride;

        [Tooltip("Optional. Renderer used to find the top of the sprite. Auto-detected from the target when left empty.")]
        public Renderer boundsSource;

        [Tooltip("Extra world units added above the resolved anchor for this objective only.")]
        public float extraWorldHeight;

        [Tooltip("Icon shown while the objective is far / off screen. Empty = use the shared Far Arrow Sprite.")]
        public Sprite farSprite;

        [Tooltip("Which way this objective's far sprite points before rotation. Inherit = use the shared Sprite Facing.")]
        public SpriteFacingOverride farSpriteFacing = SpriteFacingOverride.Inherit;

        [Tooltip("Icon shown while the player is near the objective. Empty = use the shared Near Marker Sprite.")]
        public Sprite nearSprite;

        [Tooltip("Tint multiplied onto the marker for this objective.")]
        public Color tint = Color.white;
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

    [Tooltip("Which way the arrow artwork points in the source image. Set this once and every rotation below is corrected automatically — no manual 180 juggling.")]
    public SpriteFacing spriteFacing = SpriteFacing.Up;

    [Tooltip("Extra manual fine-tune on top of Sprite Facing. Normally leave at 0.")]
    public float rotationOffset = 0f;

    [Header("Display Mode")]
    public ArrowDisplayMode displayMode = ArrowDisplayMode.FixedHud;
    [Tooltip("Distance in pixels kept between a screen-edge arrow and the edge of the canvas.")]
    public float screenEdgeMargin = 52f;
    [Tooltip("Extra canvas-space inset for left, bottom, right, and top edges.")]
    public Vector4 screenEdgeInsets = Vector4.zero;
    [Tooltip("Hide the screen-edge arrow while the target is already visible.")]
    public bool hideWhenTargetVisible = true;
    [Tooltip("When the target is visible, show the arrow as a marker above it instead of keeping it at the screen edge.")]
    public bool pinToVisibleTarget;
    public Vector2 visibleTargetOffset = new Vector2(0f, 44f);
    [Tooltip("Where the pinned marker should point, written as if the sprite faced Up. 180 = point down at the objective. Sprite Facing is added on top automatically.")]
    public float visibleTargetRotation = 180f;
    [Tooltip("Hide the screen-edge arrow when the player is this close to the target. Set to 0 to disable.")]
    public float hideWithinWorldDistance = 1.5f;

    [Header("Sprites — Two-State Marker")]
    [Tooltip("Default arrow sprite, used while the objective is far away or off screen. Leave empty to keep whatever sprite the Image already has.")]
    public Sprite farArrowSprite;
    [Tooltip("Default '!' sprite, shown above the objective once the player is near. Leave empty to disable the near state entirely.")]
    public Sprite nearMarkerSprite;
    [Tooltip("World distance at or below which the marker swaps to the '!' sprite. Requires the target to be on screen.")]
    public float nearMarkerWorldDistance = 5f;
    [Tooltip("Z rotation applied to the '!' marker. Keep at 0 so it stays upright.")]
    public float nearMarkerRotation = 0f;
    [Tooltip("Size multiplier applied while the '!' marker is showing.")]
    public float nearMarkerScale = 1f;

    [Header("Anchor — Above the Objective's Head")]
    [Tooltip("Anchor the pinned marker to the top of the objective's sprite instead of its Transform origin. Turn on for pivots placed at the feet.")]
    public bool useRendererBounds = false;
    [Tooltip("World units added above the resolved anchor for every objective.")]
    public float worldHeightOffset = 0.25f;

    [Header("Per-Objective Profiles (optional)")]
    [Tooltip("Drop each objective here to give it its own anchor point and its own icon pair. Objectives with no entry fall back to the shared settings above.")]
    public ObjectiveMarkerProfile[] markerProfiles = new ObjectiveMarkerProfile[0];

    [Header("Idle Bob")]
    [Tooltip("Keep the marker gently bobbing so it stays noticeable. Runs on unscaled time so it survives a paused timescale.")]
    public bool idleBobEnabled = true;
    [Tooltip("Peak pixel offset of the bob.")]
    public float idleBobAmplitude = 5f;
    [Tooltip("Seconds for one full bob cycle.")]
    public float idleBobPeriod = 1.1f;
    [Tooltip("Extra size pulse riding along with the bob. 0 = no pulse.")]
    public float idleBobScalePulse = 0.06f;

    [Header("Smoothing & Fade")]
    [Tooltip("SmoothDamp time for marker movement. 0 = snap every frame like before.")]
    public float positionSmoothTime = 0.05f;
    [Tooltip("Seconds for the marker to fade in on Show() and out on Hide(). 0 = instant.")]
    public float fadeDuration = 0.15f;

    [Header("Distance Scaling")]
    [Tooltip("Shrink the marker as the objective gets further away, so distance is readable at a glance.")]
    public bool scaleWithDistance = false;
    [Tooltip("World distance at (or below) which the marker is at its largest.")]
    public float scaleNearDistance = 4f;
    [Tooltip("World distance at (or above) which the marker is at its smallest.")]
    public float scaleFarDistance = 24f;
    [Tooltip("Size multiplier at Scale Near Distance.")]
    public float maxDistanceScale = 1f;
    [Tooltip("Size multiplier at Scale Far Distance.")]
    public float minDistanceScale = 0.6f;

    [Header("Bounce Animation (FixedHud mode)")]
    [Tooltip("Seconds for one full bounce cycle when the arrow first appears.")]
    public float bounceDuration  = 0.4f;
    [Tooltip("Number of bounces played on Show().")]
    public int   bounceCount     = 3;
    [Tooltip("Max pixel offset of the bounce.")]
    public float bounceAmplitude = 8f;

    // ---- runtime ----
    private RectTransform _rect;
    private Camera        _cam;
    private Canvas        _canvas;
    private RectTransform _canvasRect;
    private Coroutine     _bounceRoutine;
    private Coroutine     _fadeRoutine;
    private Vector2       _basePosition;
    private Vector3       _baseScale = Vector3.one;
    private Color         _originalColor = Color.white;
    private bool          _requestedVisible;
    private bool          _fadingOut;
    private float         _alpha = 1f;

    private ObjectiveMarkerProfile _profile;
    private Transform     _cachedRendererTarget;
    private Renderer      _cachedRenderer;

    private MarkerState   _state = MarkerState.None;
    private Vector2       _smoothedPosition;
    private Vector2       _smoothVelocity;
    private bool          _snapNextFrame = true;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (arrowImage == null) arrowImage = GetComponent<Image>();
        if (arrowImage != null) _originalColor = arrowImage.color;
        _baseScale = _rect.localScale;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        ResolveSceneReferences();
        _basePosition = _rect.anchoredPosition;
    }

    private void LateUpdate()
    {
        if (!_requestedVisible && !_fadingOut) return;
        if (target == null) return;
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
        _fadingOut = false;
        _profile = ResolveProfile(target);
        _cachedRendererTarget = null;
        _cachedRenderer = null;
        _state = MarkerState.None;
        _snapNextFrame = true;

        // Ensure the Canvas (and whole hierarchy) is active — StartCoroutine
        // requires activeInHierarchy == true on the calling MonoBehaviour.
        Canvas rootCanvas = GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(true);

        gameObject.SetActive(true);
        ResolveSceneReferences();

        if (arrowImage != null)
            arrowImage.enabled = true;

        if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
        if (fadeDuration > 0f)
        {
            _alpha = 0f;
            ApplyAlpha();
            _fadeRoutine = StartCoroutine(FadeRoutine(1f));
        }
        else
        {
            _alpha = 1f;
            ApplyAlpha();
        }

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

        if (fadeDuration <= 0f || !gameObject.activeInHierarchy)
        {
            _fadingOut = false;
            _alpha = 1f;
            ApplyAlpha();
            gameObject.SetActive(false);
            return;
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadingOut = true;
        _fadeRoutine = StartCoroutine(FadeRoutine(0f));
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

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>(true);
            _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
        }
    }

    private ObjectiveMarkerProfile ResolveProfile(Transform t)
    {
        if (t == null || markerProfiles == null) return null;

        for (int i = 0; i < markerProfiles.Length; i++)
        {
            ObjectiveMarkerProfile p = markerProfiles[i];
            if (p == null || p.target == null) continue;
            if (p.target == t || t.IsChildOf(p.target) || p.target.IsChildOf(t))
                return p;
        }

        return null;
    }

    private void ConfigureScreenEdgeRect()
    {
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// World point the pinned marker floats above: the profile's hand-placed
    /// anchor if there is one, otherwise the top of the sprite bounds, otherwise
    /// the plain Transform origin.
    /// </summary>
    private Vector3 GetAnchorWorldPosition()
    {
        if (_profile != null && _profile.anchorOverride != null)
            return _profile.anchorOverride.position;

        Vector3 anchor = target.position;

        if (useRendererBounds)
        {
            Renderer r = _profile != null && _profile.boundsSource != null
                ? _profile.boundsSource
                : GetCachedRenderer();

            if (r != null)
                anchor.y = r.bounds.max.y;
        }

        anchor.y += worldHeightOffset;
        if (_profile != null)
            anchor.y += _profile.extraWorldHeight;

        return anchor;
    }

    private Renderer GetCachedRenderer()
    {
        if (_cachedRendererTarget != target)
        {
            _cachedRendererTarget = target;
            _cachedRenderer = target != null ? target.GetComponentInChildren<Renderer>() : null;
        }

        return _cachedRenderer;
    }

    private float GetObjectiveDistance()
    {
        if (target == null) return float.MaxValue;

        Vector3 from = player != null ? player.position
                     : _cam != null ? _cam.transform.position
                     : Vector3.zero;

        return Vector2.Distance(from, target.position);
    }

    private void UpdateScreenEdgeArrow()
    {
        Vector3 anchorWorld = GetAnchorWorldPosition();
        Vector3 targetScreen3 = _cam.WorldToScreenPoint(anchorWorld);
        Vector3 viewport = _cam.WorldToViewportPoint(target.position);
        bool targetVisible = viewport.z > 0f
                             && viewport.x >= 0f && viewport.x <= 1f
                             && viewport.y >= 0f && viewport.y <= 1f;

        float distance = GetObjectiveDistance();
        bool playerNear = player != null && hideWithinWorldDistance > 0f
                          && distance <= hideWithinWorldDistance;

        Sprite nearSprite = _profile != null && _profile.nearSprite != null
            ? _profile.nearSprite
            : nearMarkerSprite;

        bool useNearMarker = nearSprite != null && targetVisible
                             && distance <= nearMarkerWorldDistance;

        bool pinned = useNearMarker || (targetVisible && pinToVisibleTarget);
        bool hidden = playerNear || (hideWhenTargetVisible && targetVisible && !pinned);

        if (arrowImage != null)
            arrowImage.enabled = !hidden;

        if (hidden)
        {
            _state = MarkerState.None;
            return;
        }

        if (_canvasRect == null)
            return;

        MarkerState newState = useNearMarker ? MarkerState.NearMarker
                             : pinned        ? MarkerState.PinnedArrow
                             :                 MarkerState.EdgeArrow;

        if (newState != _state)
        {
            // Never let the marker slide across the whole screen when it swaps
            // between edge / pinned / near — snap once, then resume smoothing.
            _snapNextFrame = true;
            _state = newState;
            ApplySprite(useNearMarker ? nearSprite : ResolveFarSprite());
        }

        GetNavigationBounds(_canvasRect, out Vector2 boundsMin, out Vector2 boundsMax);

        Vector2 desired;
        float rotation;
        Vector2 bobAxis = Vector2.up;

        if (pinned)
        {
            Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, targetScreen3, eventCamera, out Vector2 localTarget))
                return;

            Vector2 wanted = localTarget + visibleTargetOffset;

            // A pinned marker that gets clamped is no longer above its objective,
            // so it would be pointing at empty floor. Fall back to the edge arrow.
            Vector2 clamped = ClampToBounds(wanted, boundsMin, boundsMax);
            if (!useNearMarker && (clamped - wanted).sqrMagnitude > 1f)
            {
                pinned = false;
            }
            else
            {
                desired = clamped;
                // The "!" marker has no direction, so it never takes the facing
                // correction — it just stays upright.
                rotation = useNearMarker
                    ? nearMarkerRotation
                    : visibleTargetRotation + GetFacingCorrection();
                ApplyTransform(desired, rotation, bobAxis, useNearMarker ? nearMarkerScale : 1f, distance);
                return;
            }
        }

        // Screen-edge indicators should point from the visible screen centre,
        // not from the player's on-screen position. The player can be offset
        // while the camera is clamped at a room boundary.
        Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        if (targetScreen3.z < 0f)
            direction = -direction;
        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        if (_state != MarkerState.EdgeArrow)
        {
            _state = MarkerState.EdgeArrow;
            _snapNextFrame = true;
            ApplySprite(ResolveFarSprite());
        }

        Vector2 boundsCenter = (boundsMin + boundsMax) * 0.5f;
        Vector2 usable = (boundsMax - boundsMin) * 0.5f;
        float scaleX = Mathf.Abs(direction.x) > 0.001f
            ? usable.x / Mathf.Abs(direction.x)
            : float.MaxValue;
        float scaleY = Mathf.Abs(direction.y) > 0.001f
            ? usable.y / Mathf.Abs(direction.y)
            : float.MaxValue;

        desired = boundsCenter + direction * Mathf.Min(scaleX, scaleY);
        rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f
                   + rotationOffset + GetFacingCorrection();

        // An edge arrow bobs along the way it points, not straight up.
        ApplyTransform(desired, rotation, direction, 1f, distance);
    }

    /// <summary>
    /// Degrees needed to turn the source artwork so it behaves like an
    /// up-facing arrow. Every rotation formula assumes "up = 0°", so this is
    /// added once and both the edge arrow and the pinned arrow stay correct.
    /// </summary>
    private float GetFacingCorrection()
    {
        SpriteFacing facing = spriteFacing;

        if (_profile != null && _profile.farSpriteFacing != SpriteFacingOverride.Inherit)
            facing = (SpriteFacing)((int)_profile.farSpriteFacing - 1);

        switch (facing)
        {
            case SpriteFacing.Down:  return 180f;
            case SpriteFacing.Left:  return -90f;
            case SpriteFacing.Right: return 90f;
            default:                 return 0f;
        }
    }

    private Sprite ResolveFarSprite()
    {
        if (_profile != null && _profile.farSprite != null) return _profile.farSprite;
        return farArrowSprite;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (arrowImage == null || sprite == null) return;
        if (arrowImage.sprite != sprite)
            arrowImage.sprite = sprite;
    }

    /// <summary>
    /// Final write to the RectTransform: smoothing, idle bob, distance scaling
    /// and tint all land here so every state behaves consistently.
    /// </summary>
    private void ApplyTransform(Vector2 desired, float rotation, Vector2 bobAxis, float stateScale, float distance)
    {
        if (_snapNextFrame || positionSmoothTime <= 0f)
        {
            _smoothedPosition = desired;
            _smoothVelocity = Vector2.zero;
            _snapNextFrame = false;
        }
        else
        {
            _smoothedPosition = Vector2.SmoothDamp(
                _smoothedPosition, desired, ref _smoothVelocity, positionSmoothTime);
        }

        float wave = 0f;
        if (idleBobEnabled && idleBobPeriod > 0.01f)
            wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / idleBobPeriod);

        // Bob rides on top of the smoothed position so SmoothDamp never eats it.
        _rect.anchoredPosition = _smoothedPosition + bobAxis.normalized * (wave * idleBobAmplitude);
        _rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        float scale = stateScale * (1f + wave * idleBobScalePulse) * GetDistanceScale(distance);
        _rect.localScale = _baseScale * scale;

        ApplyAlpha();
    }

    private float GetDistanceScale(float distance)
    {
        if (!scaleWithDistance) return 1f;
        if (scaleFarDistance <= scaleNearDistance) return maxDistanceScale;

        float t = Mathf.InverseLerp(scaleNearDistance, scaleFarDistance, distance);
        return Mathf.Lerp(maxDistanceScale, minDistanceScale, t);
    }

    private void ApplyAlpha()
    {
        if (arrowImage == null) return;

        Color c = _originalColor;
        if (_profile != null)
            c *= _profile.tint;

        c.a = _originalColor.a * _alpha;
        arrowImage.color = c;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float start = _alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
            ApplyAlpha();
            yield return null;
        }

        _alpha = targetAlpha;
        ApplyAlpha();
        _fadeRoutine = null;

        if (targetAlpha <= 0f)
        {
            _fadingOut = false;
            _alpha = 1f;
            ApplyAlpha();
            gameObject.SetActive(false);
        }
    }

    private void GetNavigationBounds(RectTransform canvasRect, out Vector2 boundsMin, out Vector2 boundsMax)
    {
        Vector2 halfArrow = _rect.rect.size * 0.5f;
        Rect canvasBounds = canvasRect.rect;
        boundsMin = new Vector2(
            canvasBounds.xMin + screenEdgeMargin + screenEdgeInsets.x + halfArrow.x,
            canvasBounds.yMin + screenEdgeMargin + screenEdgeInsets.y + halfArrow.y);
        boundsMax = new Vector2(
            canvasBounds.xMax - screenEdgeMargin - screenEdgeInsets.z - halfArrow.x,
            canvasBounds.yMax - screenEdgeMargin - screenEdgeInsets.w - halfArrow.y);
    }

    private static Vector2 ClampToBounds(Vector2 position, Vector2 boundsMin, Vector2 boundsMax)
    {
        return new Vector2(
            Mathf.Clamp(position.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(position.y, boundsMin.y, boundsMax.y));
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
