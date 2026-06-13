using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable redesigned objective UI driver (used by Topics 1–6). Animates TWO panels
/// built under an objective Canvas by ObjectiveRedesignSetup:
///   1. ObjectivePopup_Center  -> fade + scale "punch" announce in the center, then fade out
///   2. ObjectiveDetail_Side    -> slides in from the right, rests top-right, stays
///
/// Implements IObjectivePanel (ShowObjective(string)) so it is a drop-in for the old
/// per-topic panels. The old panels forward to this when assigned (see their `redesign`
/// field) — so callers (VN system, progression controllers, GameManagers) need no edits.
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveRedesignUI : MonoBehaviour, IObjectivePanel
{
    [Header("=== Panel Roots (auto-found if empty) ===")]
    public GameObject popupRoot;   // ObjectivePopup_Center
    public GameObject sideRoot;    // ObjectiveDetail_Side

    [Header("=== Side card header ===")]
    public string header = "OBJECTIVE";

    [Header("=== Timing (seconds) ===")]
    public float popInDuration   = 0.35f;
    public float popHoldDuration = 1.4f;
    public float popOutDuration  = 0.30f;
    public float slideInDuration = 0.45f;
    public float slideOutDuration = 0.22f;

    [Header("=== Popup punch ===")]
    [Range(0.5f, 1f)] public float popupStartScale = 0.85f;

    // resolved refs
    private CanvasGroup _popCg, _sideCg;
    private RectTransform _sideRt, _cardRt;
    private TMP_Text _txtObjective, _txtHeader, _txtBody, _txtLocation;
    private Vector2 _sideRest;
    private float _slideDistance;
    private Coroutine _routine;
    private bool _resolved;

    private void Awake()
    {
        Resolve();
        if (popupRoot) popupRoot.SetActive(false);
        if (sideRoot)  sideRoot.SetActive(false);
    }

    private void Resolve()
    {
        if (_resolved) return;

        // Panels start inactive, so GameObject.Find won't see them — walk the canvas instead.
        if (popupRoot == null) popupRoot = FindUnderCanvas("ObjectivePopup_Center");
        if (sideRoot  == null) sideRoot  = FindUnderCanvas("ObjectiveDetail_Side");

        if (popupRoot)
        {
            _popCg = GetOrAddCanvasGroup(popupRoot);
            var card = popupRoot.transform.Find("Card");
            if (card)
            {
                _cardRt = card as RectTransform;
                var to = card.Find("txtObjective");
                if (to) _txtObjective = to.GetComponent<TMP_Text>();
            }
        }

        if (sideRoot)
        {
            _sideRt = sideRoot.GetComponent<RectTransform>();
            _sideCg = GetOrAddCanvasGroup(sideRoot);
            _sideRest = _sideRt.anchoredPosition;
            _slideDistance = _sideRt.sizeDelta.x + 60f;
            _txtHeader   = FindText(sideRoot.transform, "txtDetailHeader");
            _txtBody     = FindText(sideRoot.transform, "txtDetailBody");
            _txtLocation = FindText(sideRoot.transform, "txtDetailLocation");
        }

        _resolved = true;
    }

    // ── Public API ─────────────────────────────────────────────────────────
    public void ShowObjective(string text) => ShowObjective(text, null);

    public void ShowObjective(string text, string location)
    {
        Resolve();

        if (_txtObjective) _txtObjective.text = text;
        if (_txtHeader)    _txtHeader.text = header;
        if (_txtBody)      _txtBody.text = text;
        if (_txtLocation)
        {
            bool has = !string.IsNullOrEmpty(location);
            _txtLocation.gameObject.SetActive(has);
            if (has) _txtLocation.text = location;
        }

        // Some flows leave the objective Canvas inactive until the first objective.
        EnsureCanvasActive();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!isActiveAndEnabled) return; // can't run coroutine; caller will retry when enabled

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Sequence());
    }

    public void HideAll()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (popupRoot) popupRoot.SetActive(false);
        if (sideRoot)  sideRoot.SetActive(false);
    }

    // ── Sequence ───────────────────────────────────────────────────────────
    private IEnumerator Sequence()
    {
        if (sideRoot && sideRoot.activeSelf)
            yield return SlideSideOut();

        yield return PopupRoutine();
        yield return SlideSideIn();
        _routine = null;
    }

    private IEnumerator PopupRoutine()
    {
        if (popupRoot == null) yield break;

        popupRoot.SetActive(true);
        if (_popCg) _popCg.alpha = 0f;
        if (_cardRt) _cardRt.localScale = Vector3.one * popupStartScale;

        float t = 0f;
        while (t < popInDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popInDuration);
            if (_popCg) _popCg.alpha = EaseOutCubic(k);
            if (_cardRt) _cardRt.localScale = Vector3.one * Mathf.LerpUnclamped(popupStartScale, 1f, EaseOutBack(k));
            yield return null;
        }
        if (_popCg) _popCg.alpha = 1f;
        if (_cardRt) _cardRt.localScale = Vector3.one;

        float h = 0f;
        while (h < popHoldDuration) { h += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (t < popOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popOutDuration);
            if (_popCg) _popCg.alpha = 1f - EaseInCubic(k);
            if (_cardRt) _cardRt.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, k);
            yield return null;
        }

        popupRoot.SetActive(false);
        if (_cardRt) _cardRt.localScale = Vector3.one;
    }

    private IEnumerator SlideSideIn()
    {
        if (sideRoot == null || _sideRt == null) yield break;

        sideRoot.SetActive(true);
        if (_sideCg) _sideCg.alpha = 0f;
        Vector2 off = _sideRest + new Vector2(_slideDistance, 0f);
        _sideRt.anchoredPosition = off;

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideInDuration);
            _sideRt.anchoredPosition = Vector2.LerpUnclamped(off, _sideRest, EaseOutCubic(k));
            if (_sideCg) _sideCg.alpha = Mathf.Clamp01(k * 1.4f);
            yield return null;
        }
        _sideRt.anchoredPosition = _sideRest;
        if (_sideCg) _sideCg.alpha = 1f;
    }

    private IEnumerator SlideSideOut()
    {
        if (sideRoot == null || _sideRt == null) yield break;

        Vector2 off = _sideRest + new Vector2(_slideDistance, 0f);
        Vector2 start = _sideRt.anchoredPosition;

        float t = 0f;
        while (t < slideOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideOutDuration);
            _sideRt.anchoredPosition = Vector2.LerpUnclamped(start, off, EaseInCubic(k));
            if (_sideCg) _sideCg.alpha = 1f - k;
            yield return null;
        }
        sideRoot.SetActive(false);
        _sideRt.anchoredPosition = _sideRest;
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private void EnsureCanvasActive()
    {
        var cv = GetComponentInParent<Canvas>(true);
        if (cv != null && !cv.gameObject.activeSelf)
            cv.gameObject.SetActive(true);
    }

    private GameObject FindUnderCanvas(string childName)
    {
        var cv = GetComponentInParent<Canvas>(true);
        if (cv == null) return null;
        var t = cv.transform.Find(childName);
        return t ? t.gameObject : null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        return cg != null ? cg : go.AddComponent<CanvasGroup>();
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var t = root.Find(name);
        return t ? t.GetComponent<TMP_Text>() : null;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t)  => t * t * t;
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
