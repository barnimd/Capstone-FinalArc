using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject desktopCanvas;
    public GameObject ChatPanel;
    public GameObject AttachFilePanel;
    public GameObject ConfirmationPanel;
    public GameObject InstallationPanel;
    public GameObject SecurityAlertPanel;

    [Header("Installation UI")]
    public Slider progressBar;
    public TMP_Text statusText;

    [Header("Crash Animation")]
    public CrashOverlayController_Tp1 crash;

    public GameObject mainCanvas;      // Tarik Canvas kamu ke sini
    public Image blackBackground;      // Tarik Image Background Hitam ke sini
    public GameObject contentPanel;
    public float fadeDuration = 1.2f;
    public GameplayManager gameManager;
    void Start()
    {
        CloseAllPanels();

        // Guard: hanya jalankan jika field sudah di-assign dan BUKAN DesktopCanvas
        if (mainCanvas != null && mainCanvas != desktopCanvas)
            mainCanvas.SetActive(false);

        if (contentPanel != null)
            contentPanel.SetActive(false);

        if (blackBackground != null)
        {
            Color c = blackBackground.color;
            c.a = 0f;
            blackBackground.color = c;
        }
    }

    public void OpenUIWithTransition()
    {
        StartCoroutine(ExecuteTransition());
    }

    IEnumerator ExecuteTransition()
    {
        // 1. Aktifkan Canvas terlebih dahulu
        mainCanvas.SetActive(true);

        // 2. Transisi Background Hitam (0f ke 255f / 1.0f)
        float elapsedTime = 0f;
        Color tempColor = blackBackground.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Lerp dari 0f (Transparan) ke 1f (Hitam Pekat)
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);

            tempColor.a = alpha;
            blackBackground.color = tempColor;

            yield return null; // Tunggu frame berikutnya
        }

        // Pastikan alpha benar-benar 1 (255f) di akhir
        tempColor.a = 1f;
        blackBackground.color = tempColor;

        // 3. Setelah Fade-In selesai, Munculkan Konten Utama
        contentPanel.SetActive(true);
        gameManager.FinishGame();
        Debug.Log("Transition Complete: Canvas Active -> BG Faded -> Content Shown");
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
        desktopCanvas.SetActive(false);
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
        desktopCanvas.SetActive(false);
    }

    public void ConfirmSend()
    {
        // Penalty for accepting to send the file (bad decision)
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(-25);

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

        if (crash != null)
        {
            // FIX: Scale crashCanvas ter-set (0,0,0) di scene, paksa ke (1,1,1)
            if (crash.crashCanvas != null)
                crash.crashCanvas.transform.localScale = Vector3.one;

            crash.PlayCrash(() => SecurityAlertPanel.SetActive(true));
        }
        else
            SecurityAlertPanel.SetActive(true);
    }

    public void CloseSecurityPopup()
    {
        SecurityAlertPanel.SetActive(false);
        desktopCanvas.SetActive(false);
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