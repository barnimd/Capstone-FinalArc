using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject ChatPanel;
    public GameObject AttachFilePanel;
    public GameObject ConfirmationPanel;
    public GameObject InstallationPanel;
    public GameObject SecurityAlertPanel;

    [Header("Installation UI")]
    public Slider progressBar;
    public TMP_Text statusText;

    void Start()
    {
        CloseAllPanels();
    }

    // =============================
    // ICON CLICK
    // =============================
    public void OpenChat()
    {
        CloseAllPanels();
        ChatPanel.SetActive(true);
    }

    // =============================
    // ATTACH FILE
    // =============================
    public void OpenAttachFile()
    {
        AttachFilePanel.SetActive(true);
    }

    public void CancelAttach()
    {
        CloseAllPanels();
    }

    // =============================
    // CONFIRMATION
    // =============================
    public void OpenConfirmation()
    {
        AttachFilePanel.SetActive(false);
        ConfirmationPanel.SetActive(true);
    }

    public void CancelConfirmation()
    {
        CloseAllPanels();
    }

    public void ConfirmSend()
    {
        ConfirmationPanel.SetActive(false);
        InstallationPanel.SetActive(true);
        StartCoroutine(SendProgress());
    }

    // =============================
    // PROGRESS BAR
    // =============================
    IEnumerator SendProgress()
    {
        progressBar.value = 0;
        statusText.text = "Sending file...";

        while (progressBar.value < 1f)
        {
            progressBar.value += Time.deltaTime * 0.25f;
            yield return null;
        }

        statusText.text = "File Sent!";
        yield return new WaitForSeconds(1f);

        InstallationPanel.SetActive(false);
        ChatPanel.SetActive(false);
        SecurityAlertPanel.SetActive(true);
    }

    // =============================
    // CLOSE ALL
    // =============================
    public void CloseAllPanels()
    {
        ChatPanel.SetActive(false);
        AttachFilePanel.SetActive(false);
        ConfirmationPanel.SetActive(false);
        InstallationPanel.SetActive(false);
        SecurityAlertPanel.SetActive(false);
    }
}