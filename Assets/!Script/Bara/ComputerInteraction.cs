using System.Collections;
using UnityEngine;

/// <summary>
/// Specific interaction handler for the Computer object.
/// Attach to the Computer GameObject alongside InteractableObject.
/// Hook InteractableObject.onInteract → ComputerInteraction.OnInteract() in the Inspector.
/// </summary>
public class ComputerInteraction : MonoBehaviour
{
    public event System.Action DesktopOpening;

    [Header("Desktop UI")]
    [Tooltip("The desktop canvas to activate after the fade is fully black")]
    [SerializeField] private GameObject desktopCanvas;
    [Tooltip("How long the desktop canvas fades in (seconds)")]
    [SerializeField] private float desktopFadeInDuration = 0.4f;

    [Header("Camera Effect")]
    [Tooltip("Where the camera should zoom to — defaults to this object's position")]
    [SerializeField] private Transform zoomTarget;

    public MonoBehaviour playerMovementScript;

    private CanvasGroup _desktopCanvasGroup;

    private void Awake()
    {
        if (zoomTarget == null) zoomTarget = transform;

        if (desktopCanvas != null)
        {
            _desktopCanvasGroup = desktopCanvas.GetComponent<CanvasGroup>();
            if (_desktopCanvasGroup == null)
                _desktopCanvasGroup = desktopCanvas.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Called by InteractableObject.onInteract UnityEvent.
    /// Triggers camera zoom + fade to black.
    /// </summary>
    public void OnInteract()
    {
        if (CameraZoomFade.Instance == null)
        {
            Debug.LogWarning("[ComputerInteraction] CameraZoomFade instance not found.");
            return;
        }

        DesktopOpening?.Invoke();

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
            collider.enabled = false;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        CameraZoomFade.Instance.ZoomAndFade(zoomTarget.position, OnFadeComplete);
    }

    private void OnFadeComplete()
    {
        if (desktopCanvas != null)
            StartCoroutine(FadeInDesktop());
    }

    private IEnumerator FadeInDesktop()
    {
        _desktopCanvasGroup.alpha = 0f;
        desktopCanvas.SetActive(true);

        float elapsed = 0f;
        while (elapsed < desktopFadeInDuration)
        {
            elapsed += Time.deltaTime;
            _desktopCanvasGroup.alpha = Mathf.Clamp01(elapsed / desktopFadeInDuration);
            yield return null;
        }

        _desktopCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Call this when the desktop UI closes to restore Cinemachine player-follow camera.
    /// </summary>
    public void OnDesktopClosed()
    {
        CameraZoomFade.Instance?.RestoreCinemachine(0.5f, () =>
        {
            Debug.Log("[ComputerInteraction] Camera restored to player follow.");
        });
    }
}
