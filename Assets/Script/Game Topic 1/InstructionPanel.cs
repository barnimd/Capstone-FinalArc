using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows an instruction panel at the start of the level.
/// Player must click "OK" or press E/Space to dismiss.
/// Freezes player movement during display.
/// </summary>
public class InstructionPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The instruction panel GameObject to show/hide")]
    public GameObject instructionPanel;

    [Tooltip("Optional: Button to dismiss the panel")]
    public Button okButton;

    [Header("Settings")]
    [Tooltip("Instruction text to display")]
    [TextArea(3, 5)]
    public string instructionText = "Welcome to your first day at the office!\nPlease proceed to your desk.\nTalk to the receptionist first.";

    [Tooltip("Optional: TMP text component to set the instruction text")]
    public TextMeshProUGUI instructionTextComponent;

    [Tooltip("Delay before showing the panel (seconds)")]
    public float showDelay = 0.5f;

    private PlayerMovement playerMovement;
    private bool isShowing = false;

    void Start()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (okButton != null)
            okButton.onClick.AddListener(DismissPanel);

        // Find player and freeze movement
        StartCoroutine(ShowAfterDelay());
    }

    System.Collections.IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        ShowPanel();
    }

    void ShowPanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            isShowing = true;

            if (instructionTextComponent != null)
                instructionTextComponent.text = instructionText;

            // Freeze player
            if (playerMovement != null)
                playerMovement.movementLocked = true;
        }
    }

    public void DismissPanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
            isShowing = false;

            // Unfreeze player
            if (playerMovement != null)
                playerMovement.movementLocked = false;
        }
    }

    void Update()
    {
        if (isShowing)
        {
            // Allow dismissing with E or Space
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                DismissPanel();
            }
        }
    }
}
