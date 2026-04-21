using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ComputerInteraction2 - Handles the player's desk computer.
/// When the player sits at their desk and opens the computer,
/// an NPC requests crucial finance files via the desktop UI.
/// This is the second computer interaction in the Topic 1 flow.
/// </summary>
public class ComputerInteraction2 : MonoBehaviour
{
    [Header("Desktop UI")]
    public GameObject desktopCanvas;
    public GameObject interactText;

    [Header("File Request UI")]
    [Tooltip("Panel that shows the finance file request")]
    public GameObject fileRequestPanel;
    [Tooltip("Text showing who is requesting the file")]
    public TextMeshProUGUI requestText;

    [Header("Request Settings")]
    [TextArea(2, 4)]
    public string requestMessage = "URGENT: Please send the Q3 Finance Report.\nThis file contains sensitive financial data.";

    [Header("Choice Buttons")]
    public Button sendFileButton;
    public Button refuseButton;

    [Header("Scoring")]
    [Tooltip("Penalty for sending the file (bad choice)")]
    public int sendPenalty = -25;
    [Tooltip("Reward for refusing to send (safe choice)")]
    public int refuseReward = 0;

    [Header("Feedback Panels")]
    public GameObject penaltyFeedbackPanel;
    public GameObject safeFeedbackPanel;

    private bool canInteract = false;
    private bool desktopOpen = false;
    private PlayerMovement playerMov;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);

        if (desktopCanvas != null)
            desktopCanvas.SetActive(false);

        if (fileRequestPanel != null)
            fileRequestPanel.SetActive(false);

        if (penaltyFeedbackPanel != null)
            penaltyFeedbackPanel.SetActive(false);

        if (safeFeedbackPanel != null)
            safeFeedbackPanel.SetActive(false);

        if (sendFileButton != null)
            sendFileButton.onClick.AddListener(OnSendFile);

        if (refuseButton != null)
            refuseButton.onClick.AddListener(OnRefuseFile);
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(KeyCode.E) && !desktopOpen)
        {
            OpenDesktop();
        }

        if (desktopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDesktop();
        }
    }

    void OpenDesktop()
    {
        if (desktopCanvas != null)
            desktopCanvas.SetActive(true);

        desktopOpen = true;

        if (playerMov != null)
            playerMov.movementLocked = true;

        // Show the file request after a short delay
        StartCoroutine(ShowFileRequestDelayed());
    }

    IEnumerator ShowFileRequestDelayed()
    {
        yield return new WaitForSeconds(1f);

        if (fileRequestPanel != null)
        {
            fileRequestPanel.SetActive(true);

            if (requestText != null)
                requestText.text = requestMessage;
        }
    }

    public void OnSendFile()
    {
        Debug.Log("[ComputerInteraction2] Player sent the file! BAD CHOICE.");

        // Penalty score
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(sendPenalty);

        if (fileRequestPanel != null)
            fileRequestPanel.SetActive(false);

        // Show penalty feedback
        if (penaltyFeedbackPanel != null)
        {
            penaltyFeedbackPanel.SetActive(true);
            StartCoroutine(CloseFeedbackAfterDelay(penaltyFeedbackPanel, 3f));
        }
        else
        {
            CloseDesktop();
        }
    }

    public void OnRefuseFile()
    {
        Debug.Log("[ComputerInteraction2] Player refused to send the file! SAFE CHOICE.");

        // Reward score
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(refuseReward);

        if (fileRequestPanel != null)
            fileRequestPanel.SetActive(false);

        // Show safe feedback
        if (safeFeedbackPanel != null)
        {
            safeFeedbackPanel.SetActive(true);
            StartCoroutine(CloseFeedbackAfterDelay(safeFeedbackPanel, 3f));
        }
        else
        {
            CloseDesktop();
        }
    }

    IEnumerator CloseFeedbackAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null)
            panel.SetActive(false);
        CloseDesktop();
    }

    public void CloseDesktop()
    {
        if (desktopCanvas != null)
            desktopCanvas.SetActive(false);

        desktopOpen = false;

        if (playerMov != null)
            playerMov.movementLocked = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            playerMov = other.GetComponent<PlayerMovement>();

            if (interactText != null)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;

            if (desktopOpen)
                CloseDesktop();

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}
