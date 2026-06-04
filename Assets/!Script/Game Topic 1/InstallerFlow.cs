using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstallerFlow : MonoBehaviour
{
    public event System.Action<bool> InstallerCompleted;

    public GameObject desktopCanvas;
    public GameObject confirmPopup;
    public GameObject progressPanel;
    public GameObject securityPopup;

    public Slider progressBar;
    public TMP_Text statusText;

    [System.Obsolete("Use ScoreManager.instance instead")]
    public int score = 0;

    [Header("Crash Animation")]
    public CrashOverlayController_Tp1_Installer crash;

    // Tambahan untuk konfirmasi cancel
    public GameObject confirmationPanel;
    public Button backButton;
    public Button cancelButton;

    private bool sessionActive;
    private bool installStarted;

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
        if (!sessionActive || installStarted)
            return;

        installStarted = true;
        confirmPopup.SetActive(false);
        progressPanel.SetActive(true);
        StartCoroutine(InstallProcess());
    }

    public void BeginInstaller()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        StopAllCoroutines();
        sessionActive = true;
        installStarted = false;

        if (desktopCanvas != null) desktopCanvas.SetActive(true);
        if (confirmPopup != null) confirmPopup.SetActive(true);
        if (progressPanel != null) progressPanel.SetActive(false);
        if (securityPopup != null) securityPopup.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (progressBar != null) progressBar.value = 0f;
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

        if (crash != null)
            crash.PlayCrash(() => securityPopup.SetActive(true));
        else
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
        if (!sessionActive)
            return;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (desktopCanvas != null) desktopCanvas.SetActive(false);

        Debug.Log("Instalasi dibatalkan. Semua popup ditutup. Score: " + (ScoreManager.instance != null ? ScoreManager.instance.score : score));
        CompleteInstaller(false);
    }

    public void CloseSecurityPopup()
    {
        if (securityPopup != null) securityPopup.SetActive(false);
        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        CompleteInstaller(true);
    }

    private void CompleteInstaller(bool installed)
    {
        if (!sessionActive)
            return;

        sessionActive = false;
        InstallerCompleted?.Invoke(installed);
    }
}
