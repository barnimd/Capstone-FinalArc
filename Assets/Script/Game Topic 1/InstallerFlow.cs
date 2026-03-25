using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstallerFlow : MonoBehaviour
{
    public GameObject desktopCanvas;
    public GameObject confirmPopup;
    public GameObject progressPanel;
    public GameObject securityPopup;

    public Slider progressBar;
    public TMP_Text statusText;

    [System.Obsolete("Use ScoreManager.instance instead")]
    public int score = 0;

    // Tambahan untuk konfirmasi cancel
    public GameObject confirmationPanel;
    public Button backButton;
    public Button cancelButton;

    void Start()
    {
        // Panel konfirmasi awalnya tidak aktif
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        // Setup tombol konfirmasi
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelConfirmed);
    }

    public void StartInstall()
    {
        confirmPopup.SetActive(false);
        progressPanel.SetActive(true);
        StartCoroutine(InstallProcess());
    }

    IEnumerator InstallProcess()
    {
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime / 3f; // 3 detik loading
            progressBar.value = progress;

            if (progress < 0.3f)
                statusText.text = "Extracting files...";
            else if (progress < 0.6f)
                statusText.text = "Granting permissions...";
            else
                statusText.text = "Connecting to server...";

            yield return null;
        }

        progressPanel.SetActive(false);

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(-25);
        Debug.Log("Data Bocor! Score: " + (ScoreManager.instance != null ? ScoreManager.instance.score : score));

        securityPopup.SetActive(true);
    }

    // Update CancelInstall supaya tampil konfirmasi dulu
    public void CancelInstall()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);

    }

    // Tombol Back, hanya menutup panel
    public void OnBack()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        Debug.Log("User memilih kembali, instalasi dilanjutkan.");
    }

    // Tombol Cancel, benar-benar membatalkan
    public void OnCancelConfirmed()
    {

        confirmationPanel.SetActive(false);
        confirmPopup.SetActive(false);
        desktopCanvas.SetActive(false);

        // Update score - cancelling is the safe choice, no penalty
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(0);
        Debug.Log("Instalasi dibatalkan. Semua popup ditutup. Score: " + (ScoreManager.instance != null ? ScoreManager.instance.score : score));
    }

    public void CloseSecurityPopup()
    {
        securityPopup.SetActive(false);
        desktopCanvas.SetActive(false);
    }
}