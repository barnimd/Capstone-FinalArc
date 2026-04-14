using UnityEngine;

/// <summary>
/// Specific interaction handler for the Computer object.
/// Attach to the Computer GameObject alongside InteractableObject.
/// Hook InteractableObject.onInteract → ComputerInteraction.OnInteract() in the Inspector.
/// </summary>
public class ComputerInteraction : MonoBehaviour
{
    [Header("Camera Effect")]
    [Tooltip("Where the camera should zoom to — defaults to this object's position")]
    [SerializeField] private Transform zoomTarget;

    private void Awake()
    {
        if (zoomTarget == null) zoomTarget = transform;
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

        CameraZoomFade.Instance.ZoomAndFade(zoomTarget.position, OnFadeComplete);
    }

    private void OnFadeComplete()
    {
        // TODO: Load computer mini-game scene or open desktop UI here
        Debug.Log("[ComputerInteraction] Fade complete — ready to load next scene or UI.");
    }
}
