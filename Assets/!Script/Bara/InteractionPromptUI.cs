using TMPro;
using UnityEngine;

/// <summary>
/// Manages the "Press E" interaction prompt UI.
/// Place this script on the InteractionPrompt UI panel.
/// Called automatically by PlayerInteraction.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        HidePrompt();
    }

    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptPanel.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }
}
