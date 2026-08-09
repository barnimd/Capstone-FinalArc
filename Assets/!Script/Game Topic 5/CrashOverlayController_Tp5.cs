using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashOverlayController_Tp5 : MonoBehaviour
{
    [Header("=== Overlay ===")]
    public GameObject crashCanvas;
    public Image mainBg;
    public TMP_Text mainText;

    [Header("=== Alert Text ===")]
    public TMP_Text alertLabel;

    [Header("=== Intrusion Messages ===")]
    public string[] intrusionLines = new string[]
    {
        "WARNING: Unauthorized access detected",
        "Firewall bypassed",
        "Data connection established: 192.168.1.105",
        "Credentials exposed: admin / ********",
        "Personal data: Name, Email, Password",
        "DATABASE COMPROMISED"
    };

    [Header("=== Timing ===")]
    public float fadeInDuration = 0.6f;
    public float lineShowDuration = 0.9f;
    public float lineFadeDuration = 0.3f;
    public float redFlashCount = 3;
    public float flashInterval = 0.2f;
    public float finalFadeDuration = 0.8f;

    [Header("=== Animasi Alternatif ===")]
    [Tooltip("Gunakan animasi phishing-hack (dari Topic 3) alih-alih crash biasa. Tidak mengubah alur game.")]
    public bool usePhishingHackAnimation;

    private System.Action _onDone;
    private bool _playing;

    void Awake() { }

    private void OnDisable()
    {
        _playing = false;
    }

    public void PlayCrash(System.Action onDone)
    {
        Debug.Log("[CrashTp5] PlayCrash called");
        _onDone = onDone;

        // Ganti animasi crash dengan phishing-hack (dari Topic 3) bila diaktifkan —
        // callback tetap dipanggil sama, sehingga alur game tidak berubah.
        if (usePhishingHackAnimation)
        {
            PhishingHackAnimation anim = PhishingHackAnimation.Cari();
            if (anim != null)
            {
                anim.Mainkan(onDone);
                return;
            }
            Debug.LogWarning("[CrashTp5] PhishingHackAnimation tidak ada di scene — fallback ke crash biasa.");
        }

        // Jaga agar animasi tidak berjalan dua kali (mencegah teks terduplikasi).
        if (_playing)
            return;
        _playing = true;

        if (!crashCanvas) crashCanvas = gameObject;
        if (!mainBg) mainBg = GetComponentInChildren<Image>(true);
        if (!mainText) mainText = GetComponentInChildren<TMP_Text>(true);

        crashCanvas.SetActive(true);
        crashCanvas.transform.SetAsLastSibling();
        Debug.Log("[CrashTp5] canvas active=" + crashCanvas.activeSelf);

        StartCoroutine(CrashRoutine());
    }

    IEnumerator CrashRoutine()
    {
        Debug.Log("[CrashTp5] CrashRoutine start");

        // Bersihkan semua child canvas agar tidak ada sisa ikon/teks dari canvas lain
        // menutupi animasi (mis. ikon yang dibuat prosedural di run sebelumnya).
        ClearCanvasChildren();

        // Phase 1: Fade to dark red-black
        Debug.Log("[CrashTp5] Phase 1: Fade in");
        if (mainBg)
        {
            mainBg.color = Color.clear;
            mainBg.gameObject.SetActive(true);
        }
        if (mainText) mainText.text = "";
        if (alertLabel) alertLabel.gameObject.SetActive(false);

        float elapsed = 0f;
        Color targetBg = new Color(0.12f, 0.02f, 0.02f, 1f);
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            if (mainBg) mainBg.color = Color.Lerp(Color.clear, targetBg, t);
            yield return null;
        }
        if (mainBg) mainBg.color = targetBg;

        // Phase 2: Intrusion messages cascade
        Debug.Log("[CrashTp5] Phase 2: Intrusion messages");
        if (mainText)
        {
            mainText.color = new Color(1f, 0.3f, 0.3f, 1f);
            mainText.fontSize = 15;
            mainText.alignment = TextAlignmentOptions.Left;
            mainText.text = "";

            for (int i = 0; i < intrusionLines.Length; i++)
            {
                // Show line with fade-in
                mainText.text = mainText.text + "\n" + intrusionLines[i];
                if (mainText.text.StartsWith("\n")) mainText.text = mainText.text.Substring(1);

                // Flash the background slightly for emphasis
                if (mainBg)
                {
                    mainBg.color = new Color(0.25f, 0.04f, 0.04f, 1f);
                    float flashElapsed = 0f;
                    while (flashElapsed < 0.15f)
                    {
                        flashElapsed += Time.deltaTime;
                        mainBg.color = Color.Lerp(new Color(0.25f, 0.04f, 0.04f, 1f), targetBg, flashElapsed / 0.15f);
                        yield return null;
                    }
                    mainBg.color = targetBg;
                }

                yield return new WaitForSeconds(lineShowDuration);
            }
        }

        // Phase 3: Red flash pulses + "SECURITY BREACH"
        Debug.Log("[CrashTp5] Phase 3: Red flash");
        if (alertLabel)
        {
            alertLabel.gameObject.SetActive(true);
            alertLabel.text = "SECURITY BREACH";
            alertLabel.color = Color.white;
            alertLabel.fontSize = 30;
            alertLabel.fontStyle = FontStyles.Bold;
            alertLabel.alignment = TextAlignmentOptions.Center;
        }
        if (mainText) mainText.text = "";

        for (int i = 0; i < redFlashCount; i++)
        {
            if (mainBg)
            {
                mainBg.color = new Color(0.9f, 0, 0, 0.85f);
                yield return new WaitForSeconds(flashInterval);
                mainBg.color = Color.black;
                yield return new WaitForSeconds(flashInterval * 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(flashInterval * 1.5f);
            }
        }

        // Phase 4: Fade to full black
        Debug.Log("[CrashTp5] Phase 4: Fade out");
        if (mainBg) mainBg.color = Color.black;
        if (alertLabel) alertLabel.gameObject.SetActive(false);

        elapsed = 0f;
        while (elapsed < finalFadeDuration)
        {
            elapsed += Time.deltaTime;
            if (mainBg) mainBg.color = Color.Lerp(Color.black, Color.black, 1f);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        crashCanvas.SetActive(false);
        Debug.Log("[CrashTp5] Done, invoking callback");
        _playing = false;
        _onDone?.Invoke();
    }

    void ClearCanvasChildren()
    {
        if (crashCanvas == null) return;
        foreach (Transform child in crashCanvas.transform)
            child.gameObject.SetActive(false);
        if (mainBg != null) mainBg.gameObject.SetActive(true);
        if (mainText != null) mainText.gameObject.SetActive(true);
    }
}
