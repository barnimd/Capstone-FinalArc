using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Adds a hover highlight effect to any UI element (sidebar buttons or email rows).
/// Attach this script to each sidebar button or email list item.
/// Implements IPointerEnterHandler and IPointerExitHandler.
/// </summary>
[RequireComponent(typeof(Image))]
public class HoverHighlight : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Colors")]
    public Color normalColor    = Color.white;
    public Color hoverColor     = new Color(0.95f, 0.88f, 0.6f, 1f);  // soft yellow
    public Color pressedColor   = new Color(0.85f, 0.72f, 0.2f, 1f);  // deeper gold

    [Header("Transition")]
    [Tooltip("Speed of the color lerp animation.")]
    public float transitionSpeed = 8f;

    private Image   _image;
    private Color   _targetColor;
    private bool    _isHovered;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _targetColor = normalColor;
        _image.color = normalColor;
    }

    private void Update()
    {
        // Smooth color transition each frame
        _image.color = Color.Lerp(_image.color, _targetColor, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _targetColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _targetColor = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Brief flash on click, then return to hover color
        _image.color = pressedColor;
        _targetColor = _isHovered ? hoverColor : normalColor;
    }

    /// <summary>
    /// Call this externally to force-reset to normal (e.g. when another menu is selected).
    /// </summary>
    public void ResetToNormal()
    {
        _isHovered = false;
        _targetColor = normalColor;
    }
}
