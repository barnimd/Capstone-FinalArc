using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashOverlayController_Tp1 : MonoBehaviour
{
    [Header("=== Overlay ===")]
    public GameObject crashCanvas;
    public Image mainBg;
    public TMP_Text mainText;

    [Header("=== Warning Icon ===")]
    public GameObject warningIcon;
    public Image warningImg;

    [Header("=== Timing ===")]
    public float fadeDarkDuration = 0.5f;
    public float warningScaleDuration = 0.8f;
    public float bsodDuration = 2f;
    public float fadeBlackDuration = 0.5f;

    private System.Action _onDone;

    void Awake()
    {
    }

    /// <summary>
    /// Panggil method ini dari GameManager setelah InstallationPanel selesai.
    /// </summary>
    public void PlayCrash(System.Action onDone)
    {
        Debug.Log("[CrashTp1] PlayCrash called");
        _onDone = onDone;

        if (!crashCanvas) crashCanvas = gameObject;
        if (!mainBg) mainBg = GetComponentInChildren<Image>(true);
        if (!mainText) mainText = GetComponentInChildren<TMP_Text>(true);

        crashCanvas.SetActive(true);
        crashCanvas.transform.SetAsLastSibling();
        Debug.Log("[CrashTp1] crashCanvas active=" + crashCanvas.activeSelf + " obj=" + crashCanvas.name);

        StartCoroutine(CrashRoutine());
    }

    IEnumerator CrashRoutine()
    {
        Debug.Log("[CrashTp1] CrashRoutine start");

        // Phase 1: Fade to dark
        Debug.Log("[CrashTp1] Phase 1: Fade to dark");
        if (mainBg)
        {
            mainBg.color = Color.clear;
            mainBg.gameObject.SetActive(true);
        }
        if (mainText) mainText.text = "";
        if (warningIcon) warningIcon.SetActive(false);

        float elapsed = 0f;
        Color darkColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        while (elapsed < fadeDarkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDarkDuration);
            if (mainBg) mainBg.color = Color.Lerp(Color.clear, darkColor, t);
            yield return null;
        }
        if (mainBg) mainBg.color = darkColor;

        //Phase 2: Warning icon scaleup 
        if (warningIcon == null)
        {
            // Buat warning icon secara prosedural jika belum ada
            warningIcon = new GameObject("WarningIcon",
                typeof(RectTransform), typeof(Image));
            warningIcon.transform.SetParent(crashCanvas.transform, false);

            var wrt = warningIcon.GetComponent<RectTransform>();
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.anchoredPosition = Vector2.zero;
            wrt.sizeDelta = new Vector2(120, 120);

            warningImg = warningIcon.GetComponent<Image>();
            warningImg.color = new Color(1f, 0.6f, 0f, 0f);

            // Teks "!" di tengah icon
            var wtxt = new GameObject("Txt", typeof(RectTransform));
            wtxt.transform.SetParent(warningIcon.transform, false);
            var ttmp = wtxt.AddComponent<TextMeshProUGUI>();
            ttmp.text = "!";
            ttmp.fontSize = 60;
            ttmp.fontStyle = TMPro.FontStyles.Bold;
            ttmp.alignment = TMPro.TextAlignmentOptions.Center;
            ttmp.color = Color.white;
            var trt = wtxt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        warningIcon.SetActive(true);
        warningIcon.transform.localScale = Vector3.one * 0.2f;

        elapsed = 0f;
        while (elapsed < warningScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / warningScaleDuration);

            // Scale overshoot: tumbuh cepat lalu sedikit mengecil di akhir
            float scale = Mathf.Lerp(0.2f, 2.5f, t)
                          * (t < 0.7f ? 1f : 1f - (t - 0.7f) * 1.6f);

            warningIcon.transform.localScale = Vector3.one * Mathf.Max(0f, scale);
            if (warningImg) warningImg.color = new Color(1f, 0.6f, 0f, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        warningIcon.transform.localScale = Vector3.one * 1.5f;

        yield return new WaitForSeconds(0.2f);

        // Phase 3: BSOD
        Debug.Log("[CrashTp1] Phase 3: BSOD");
        if (mainBg) mainBg.color = new Color(0f, 0.2f, 0.5f, 1f);
        if (warningIcon) warningIcon.SetActive(false);
        if (mainText)
        {
            mainText.text =
                "SYSTEM FAILURE\n\n" +
                "Error: 0xC000021A\n\n" +
                "A critical system process\n" +
                "has been terminated.\n\n" +
                "Security breach detected.\n" +
                "All operations halted.";
            mainText.color = Color.white;
            mainText.fontSize = 20;
            mainText.alignment = TextAlignmentOptions.Center;
        }

        yield return new WaitForSeconds(bsodDuration);

        // Phase 4: Fade ke hitam penuh 
        elapsed = 0f;
        Color bsodColor = new Color(0f, 0.2f, 0.5f, 1f);
        while (elapsed < fadeBlackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeBlackDuration);
            if (mainBg) mainBg.color = Color.Lerp(bsodColor, Color.black, t);
            yield return null;
        }

        if (mainBg) mainBg.color = Color.black;
        yield return new WaitForSeconds(0.3f);

        // Selesai sembunyikan canvas dan panggil callback
        crashCanvas.SetActive(false);
        Debug.Log("[CrashTp1] Done, invoking callback");
        _onDone?.Invoke();
    }
}