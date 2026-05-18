using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashOverlayController_Tp1_Installer : MonoBehaviour
{
    [Header("=== Overlay ===")]
    public GameObject crashCanvas;
    public Image mainBg;
    public TMP_Text mainText;

    [Header("=== Red Flash ===")]
    public Image redFlash;
    public TMP_Text alertText;

    [Header("=== Terminal Lines ===")]
    public string[] terminalLines = new string[]
    {
        "initializing connection...",
        "bypassing firewall... OK",
        "injecting payload... OK",
        "gaining root access... OK",
        "ACCESS GRANTED",
        "deploying ransomware module...",
        "encrypting system files..."
    };

    [Header("=== Timing ===")]
    public float fadeInDuration = 0.8f;
    public float charTypingDelay = 0.03f;
    public float lineDelay = 0.25f;
    public float redFlashDuration = 0.5f;
    public float fadeOutDuration = 0.8f;

    private System.Action _onDone;

    void Awake() { }

    public void PlayCrash(System.Action onDone)
    {
        Debug.Log("[CrashInstaller] PlayCrash called");
        _onDone = onDone;

        if (!crashCanvas) crashCanvas = gameObject;
        if (!mainBg) mainBg = GetComponentInChildren<Image>(true);
        if (!mainText) mainText = GetComponentInChildren<TMP_Text>(true);

        crashCanvas.SetActive(true);
        crashCanvas.transform.SetAsLastSibling();
        Debug.Log("[CrashInstaller] crashCanvas active=" + crashCanvas.activeSelf + " obj=" + crashCanvas.name);

        StartCoroutine(CrashRoutine());
    }

    IEnumerator CrashRoutine()
    {
        Debug.Log("[CrashInstaller] CrashRoutine start");

        // Phase 1: Fade to black
        Debug.Log("[CrashInstaller] Phase 1: Fade to black");
        if (mainBg)
        {
            mainBg.color = Color.clear;
            mainBg.gameObject.SetActive(true);
        }
        if (mainText)
        {
            mainText.text = "";
            mainText.color = new Color(0, 1, 0, 0);
            mainText.fontSize = 16;
            mainText.alignment = TextAlignmentOptions.Left;
        }
        if (redFlash) { redFlash.gameObject.SetActive(false); }
        if (alertText) { alertText.gameObject.SetActive(false); }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            if (mainBg) mainBg.color = Color.Lerp(Color.clear, new Color(0, 0, 0, 1), t);
            yield return null;
        }
        if (mainBg) mainBg.color = Color.black;

        // Phase 2: Terminal scrolling with typewriter effect
        Debug.Log("[CrashInstaller] Phase 2: Terminal scrolling");
        if (mainText) mainText.color = new Color(0, 1, 0, 1);

        for (int i = 0; i < terminalLines.Length; i++)
        {
            mainText.text = "";
            string prefix = "";
            for (int j = 0; j < i; j++)
                prefix += "> " + terminalLines[j] + "\n";
            prefix += "> ";

            mainText.text = prefix;

            string line = terminalLines[i];
            for (int c = 0; c < line.Length; c++)
            {
                mainText.text = prefix + line.Substring(0, c + 1);
                yield return new WaitForSeconds(charTypingDelay);
            }

            yield return new WaitForSeconds(lineDelay);
        }

        // Phase 3: Red flash + alert
        Debug.Log("[CrashInstaller] Phase 3: Red flash");
        if (redFlash)
        {
            redFlash.gameObject.SetActive(true);
            redFlash.color = new Color(1f, 0, 0, 0);

            elapsed = 0f;
            while (elapsed < redFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / redFlashDuration);
                float pulse = Mathf.Sin(t * Mathf.PI * 3f) * 0.5f + 0.5f;
                redFlash.color = new Color(1f, 0, 0, pulse * 0.7f);
                yield return null;
            }
            redFlash.color = new Color(1f, 0, 0, 0.8f);
            redFlash.gameObject.SetActive(false);
        }

        if (alertText)
        {
            alertText.gameObject.SetActive(true);
            alertText.text = "SYSTEM COMPROMISED";
            alertText.color = Color.white;
            alertText.fontSize = 28;
            alertText.fontStyle = FontStyles.Bold;
            alertText.alignment = TextAlignmentOptions.Center;
        }
        if (mainText) mainText.text = "";

        yield return new WaitForSeconds(0.8f);

        // Phase 4: Fade to black
        Debug.Log("[CrashInstaller] Phase 4: Fade to black");
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            if (mainBg) mainBg.color = Color.Lerp(Color.black, Color.black, t);
            yield return null;
        }

        if (mainBg) mainBg.color = Color.black;
        yield return new WaitForSeconds(0.3f);

        // Cleanup
        crashCanvas.SetActive(false);
        if (redFlash) redFlash.gameObject.SetActive(false);
        if (alertText) alertText.gameObject.SetActive(false);
        Debug.Log("[CrashInstaller] Done, invoking callback");
        _onDone?.Invoke();
    }
}
