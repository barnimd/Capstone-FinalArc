using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages sidebar menu navigation: Kotak Masuk, Terkirim, Draft.
/// Attach this script to the Sidebar GameObject.
/// Assign the panels and buttons in the Inspector.
/// </summary>
public class SidebarMenuManager : MonoBehaviour
{
    [Header("Sidebar Buttons")]
    public Button btnKotakMasuk;
    public Button btnTerkirim;
    public Button btnDraft;
    public Button btnSelengkapnya; // optional expand

    [Header("Content Panels")]
    public GameObject panelKotakMasuk;
    public GameObject panelTerkirim;
    public GameObject panelDraft;

    [Header("Active Button Style")]
    public Color activeButtonColor = new Color(1f, 0.76f, 0.2f);   // golden highlight
    public Color inactiveButtonColor = Color.white;

    private Button _currentActiveButton;

    private void Start()
    {
        // Wire up button listeners
        btnKotakMasuk.onClick.AddListener(() => OpenPanel(panelKotakMasuk, btnKotakMasuk));
        btnTerkirim.onClick.AddListener(()   => OpenPanel(panelTerkirim,   btnTerkirim));
        btnDraft.onClick.AddListener(()      => OpenPanel(panelDraft,      btnDraft));

        // Open Kotak Masuk by default
        OpenPanel(panelKotakMasuk, btnKotakMasuk);
    }

    /// <summary>
    /// Hides all panels, then shows the requested one and highlights its button.
    /// </summary>
    public void OpenPanel(GameObject targetPanel, Button sourceButton)
    {
        // Hide all panels
        SetPanelActive(panelKotakMasuk, false);
        SetPanelActive(panelTerkirim,   false);
        SetPanelActive(panelDraft,      false);

        // Reset previous active button color
        if (_currentActiveButton != null)
            SetButtonColor(_currentActiveButton, inactiveButtonColor);

        // Show target panel
        SetPanelActive(targetPanel, true);

        // Highlight active button
        SetButtonColor(sourceButton, activeButtonColor);
        _currentActiveButton = sourceButton;
    }

    private void SetPanelActive(GameObject panel, bool state)
    {
        if (panel != null)
            panel.SetActive(state);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
