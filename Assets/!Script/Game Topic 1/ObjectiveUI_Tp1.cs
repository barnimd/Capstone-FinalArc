using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Objective panel for Topic 1 (Privasi_Keamanan).
/// Call ShowObjective(text) to display the panel at the centre of the screen,
/// then after <see cref="highlightDuration"/> seconds it smoothly slides to the
/// top-left corner and shrinks to a compact size.
/// </summary>
public class ObjectiveUI_Tp1 : MonoBehaviour, IObjectivePanel
{
    [Header("UI References")]
    public TMP_Text objectiveText;
    [Tooltip("Root panel RectTransform. Leave empty to use this GameObject.")]
    public GameObject panelRoot;

    [Tooltip("If set, objectives are shown via the redesigned 2-panel UI instead of this panel.")]
    public ObjectiveRedesignUI redesign;

    [Header("Animation Settings")]
    [Tooltip("Seconds the panel stays large at screen centre before sliding away.")]
    public float highlightDuration = 3f;
    [Tooltip("Seconds the slide-to-corner animation takes.")]
    public float moveDuration = 0.6f;

    [Header("Centre Position (large)")]
    [Tooltip("AnchoredPosition while the panel is centred. (0,0) = screen centre.")]
    public Vector2 centerPosition = Vector2.zero;
    public Vector2 centerSize = new Vector2(650, 110);

    [Header("Corner Position (compact — top-left)")]
    [Tooltip("AnchoredPosition relative to the top-left anchor once the panel has moved.")]
    public Vector2 cornerPosition = new Vector2(10, -10);
    public Vector2 cornerSize = new Vector2(320, 60);

    // ---- runtime ----
    private RectTransform _rect;
    private Coroutine _routine;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        _rect = panelRoot.GetComponent<RectTransform>();
        if (_rect == null) _rect = GetComponent<RectTransform>();

        panelRoot.SetActive(false);
    }

    /// <summary>
    /// Display the objective panel. Safe to call multiple times — cancels any
    /// in-progress animation and restarts from the centre.
    /// </summary>
    public void ShowObjective(string text)
    {
        if (redesign != null) { redesign.ShowObjective(text); HideOwnGraphics(); return; }

        if (objectiveText != null) objectiveText.text = text;

        // Ensure the Canvas (and whole hierarchy) is active before activating
        // child panels — StartCoroutine requires activeInHierarchy == true.
        Canvas rootCanvas = GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(true);

        panelRoot.SetActive(true);

        if (_rect != null)
        {
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot     = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = centerPosition;
            _rect.sizeDelta        = centerSize;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(MoveToCornerRoutine());
    }

    /// <summary>Immediately hide the panel without animation.</summary>
    public void HideImmediate()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Start()
    {
        if (redesign == null) redesign = FindObjectOfType<ObjectiveRedesignUI>(true);
    }

    private void OnDisable()
    {
        if (redesign != null) redesign.HideAll();
    }

    private void HideOwnGraphics()
    {
        foreach (var g in GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) g.enabled = false;
    }

    // ----------------------------------------------------------------
    private IEnumerator MoveToCornerRoutine()
    {
        yield return new WaitForSeconds(highlightDuration);
        if (_rect == null) yield break;

        // Keep anchor at centre throughout the lerp — changing anchors mid-animation
        // shifts the anchoredPosition origin and breaks the start position.
        // Instead, compute where the panel centre should be at the corner in
        // centre-anchor space, lerp there, then snap to the proper top-left config.
        Canvas canvas = GetComponentInParent<Canvas>();
        Vector2 canvasSize = canvas != null
            ? canvas.GetComponent<RectTransform>().rect.size
            : new Vector2(Screen.width, Screen.height);

        // Top-left corner in centre-anchor space = (-w/2, +h/2).
        // Offset by cornerPosition plus half the final panel size to get panel CENTRE.
        Vector2 endPos = new Vector2(
            -canvasSize.x * 0.5f + cornerPosition.x + cornerSize.x * 0.5f,
             canvasSize.y * 0.5f + cornerPosition.y - cornerSize.y * 0.5f
        );

        Vector2 startPos  = _rect.anchoredPosition;
        Vector2 startSize = _rect.sizeDelta;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            _rect.anchoredPosition = Vector2.Lerp(startPos,  endPos,      t);
            _rect.sizeDelta        = Vector2.Lerp(startSize, cornerSize,  t);
            yield return null;
        }

        // Snap to proper top-left anchor configuration
        _rect.anchorMin        = new Vector2(0f, 1f);
        _rect.anchorMax        = new Vector2(0f, 1f);
        _rect.pivot            = new Vector2(0f, 1f);
        _rect.anchoredPosition = cornerPosition;
        _rect.sizeDelta        = cornerSize;
        _routine = null;
    }
}
