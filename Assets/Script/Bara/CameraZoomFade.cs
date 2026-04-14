using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zoom camera toward a target and fade to black simultaneously.
/// Supports both Orthographic (orthographicSize) and Perspective (fieldOfView) cameras.
/// </summary>
public class CameraZoomFade : MonoBehaviour
{
    public static CameraZoomFade Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Image fadeImage;

    [Header("Zoom Settings")]
    [Tooltip("How much to zoom in. Smaller = more zoomed. For orthographic: target orthographicSize. For perspective: target FOV.")]
    [SerializeField] private float zoomAmount = 3f;
    [SerializeField] private float duration = 1f;

    [Header("Fade Delay")]
    [Tooltip("Fade starts after this fraction of the duration (0 = immediately, 0.5 = halfway through zoom)")]
    [SerializeField][Range(0f, 1f)] private float fadeStartFraction = 0.3f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
        SetFadeAlpha(0f);
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
    }

    // ---- Public API ----

    /// <summary>Zoom toward targetPosition and fade to black simultaneously. onComplete fires after.</summary>
    public void ZoomAndFade(Vector3 targetPosition, Action onComplete = null)
    {
        StartCoroutine(ZoomFadeRoutine(targetPosition, onComplete));
    }

    /// <summary>Fade back from black to transparent.</summary>
    public void FadeIn(float fadeDuration = 0.5f, Action onComplete = null)
    {
        StartCoroutine(FadeInRoutine(fadeDuration, onComplete));
    }

    // ---- Coroutines ----

    private IEnumerator ZoomFadeRoutine(Vector3 targetPosition, Action onComplete)
    {
        bool isOrtho = mainCamera.orthographic;

        // Capture start values
        float startOrthoSize = mainCamera.orthographicSize;
        float startFOV = mainCamera.fieldOfView;
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, mainCamera.transform.position.z);

        // Enable fade image hidden (alpha 0)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            SetFadeAlpha(0f);
        }

        float elapsed = 0f;
        float fadeDelay = duration * fadeStartFraction;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            // --- Zoom ---
            if (isOrtho)
                mainCamera.orthographicSize = Mathf.Lerp(startOrthoSize, zoomAmount, t);
            else
                mainCamera.fieldOfView = Mathf.Lerp(startFOV, zoomAmount, t);

            // --- Move toward target ---
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);

            // --- Fade (starts after fadeStartFraction of total duration) ---
            if (elapsed >= fadeDelay)
            {
                float fadeT = Mathf.Clamp01((elapsed - fadeDelay) / (duration - fadeDelay));
                SetFadeAlpha(Mathf.Lerp(0f, 1f, fadeT));
            }

            yield return null;
        }

        // Snap to final values
        if (isOrtho)
            mainCamera.orthographicSize = zoomAmount;
        else
            mainCamera.fieldOfView = zoomAmount;

        mainCamera.transform.position = endPos;
        SetFadeAlpha(1f);

        onComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(float fadeDuration, Action onComplete)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(0f);
        fadeImage.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}
