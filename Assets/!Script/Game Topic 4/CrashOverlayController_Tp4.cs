using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashOverlayController_Tp4 : MonoBehaviour
{
    [Header("=== Overlay ===")]
    public GameObject crashCanvas;
    public Image mainBg;
    public TMP_Text mainText;

    [Header("=== Icons ===")]
    public GameObject shieldIcon;
    public GameObject alertIcon;
    public GameObject lockIcon;

    [Header("=== Screen Tear ===")]
    public Image tearLine;

    [Header("=== Timing ===")]
    public float fadeInDuration = 0.5f;
    public float glitchLoopInterval = 0.5f;
    public int glitchLoopCount = 3;
    public float iconScaleDuration = 0.6f;
    public float pulseCount = 3;
    public float pulseInterval = 0.3f;
    public float fadeOutDuration = 0.5f;

    private System.Action _onDone;
    private bool _playing;
    private static readonly char[] _scramble = { '@','#','$','%','&','*','¥','§','∆','∑','∏','█','▓','▒' };

    void Awake() { }

    private void OnDisable()
    {
        _playing = false;
    }

    public void PlayCrash(System.Action onDone)
    {
        Debug.Log("[CrashTp4] PlayCrash called");
        _onDone = onDone;

        // Jaga agar animasi tidak berjalan dua kali (mencegah teks terduplikasi).
        if (_playing)
            return;
        _playing = true;

        if (!crashCanvas) crashCanvas = gameObject;
        if (!mainBg) mainBg = GetComponentInChildren<Image>(true);
        if (!mainText) mainText = GetComponentInChildren<TMP_Text>(true);

        crashCanvas.SetActive(true);
        crashCanvas.transform.SetAsLastSibling();

        StartCoroutine(CrashRoutine());
    }

    IEnumerator CrashRoutine()
    {
        // Bersihkan semua child canvas agar tidak ada sisa ikon/teks dari canvas lain
        // menutupi animasi (mis. ShieldIcon/LockIcon yang aktif di scene Topic 5).
        ClearCanvasChildren();

        // Phase 1: Fade to dark purple
        Color purple = new Color(0.1f, 0.02f, 0.15f, 1f);
        if (mainBg)
        {
            mainBg.color = Color.clear;
            mainBg.gameObject.SetActive(true);
        }
        if (mainText) mainText.text = "";
        HideAllIcons();
        if (tearLine) tearLine.gameObject.SetActive(false);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (mainBg) mainBg.color = Color.Lerp(Color.clear, purple, elapsed / fadeInDuration);
            yield return null;
        }

        // Phase 2: Glitch loop with screen tear + text scramble
        Color[] glitchColors = { purple, new Color(0.6f, 0, 0, 1f), new Color(0.55f, 0, 0.2f, 1f), purple };
        string[] glitchLines = { "WARNING", "INTRUSION", "BREACH", "EXPOSED", "COMPROMISED" };

        for (int i = 0; i < glitchLoopCount; i++)
        {
            // Screen tear: horizontal red line jumps to random Y
            if (tearLine)
            {
                tearLine.gameObject.SetActive(true);
                var trt = tearLine.GetComponent<RectTransform>();
                if (trt) trt.anchoredPosition = new Vector2(0, Random.Range(-200f, 200f));
                tearLine.color = new Color(1f, 0.1f, 0.1f, Random.Range(0.6f, 1f));
            }

            // Background flash
            if (mainBg) mainBg.color = glitchColors[i % glitchColors.Length];
            yield return new WaitForSeconds(0.08f);

            // Text scramble: show random glitch line with random symbols
            if (mainText)
            {
                mainText.text = glitchLines[i % glitchLines.Length];
                mainText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                mainText.fontSize = 34;
                mainText.alignment = TextAlignmentOptions.Center;

                // Scramble the text with random chars
                for (int s = 0; s < 4; s++)
                {
                    string original = glitchLines[i % glitchLines.Length];
                    char[] chars = original.ToCharArray();
                    for (int c = 0; c < chars.Length; c++)
                        if (Random.value < 0.3f) chars[c] = _scramble[Random.Range(0, _scramble.Length)];
                    mainText.text = new string(chars);
                    yield return new WaitForSeconds(0.04f);
                }
                mainText.text = glitchLines[i % glitchLines.Length];
            }

            if (mainBg) mainBg.color = purple;
            if (tearLine) tearLine.gameObject.SetActive(false);
            yield return new WaitForSeconds(glitchLoopInterval);
        }

        // Phase 3: Icon transform — shield cracks then alert replaces it
        if (mainText) { mainText.text = ""; mainText.fontSize = 15; }

        // Create icons if null
        if (!shieldIcon) shieldIcon = CreateIcon("ShieldIcon", new Color(0.2f, 0.9f, 0.3f), new Vector2(100, 120));
        if (!alertIcon) alertIcon = CreateIcon("AlertIcon", new Color(0.95f, 0.1f, 0.1f), new Vector2(100, 100));
        if (!lockIcon) lockIcon = CreateIcon("LockIcon", new Color(1f, 0.76f, 0.2f), new Vector2(40, 50));

        // Show shield scaling up
        shieldIcon.SetActive(true);
        shieldIcon.transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < iconScaleDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1.4f, t / iconScaleDuration);
            shieldIcon.transform.localScale = new Vector3(s, s, s);
            if (mainBg) mainBg.color = Color.Lerp(purple, new Color(0.05f, 0.15f, 0.05f, 1f), t / iconScaleDuration);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        // Shield "cracks" → alert icon replaces it
        shieldIcon.SetActive(false);
        alertIcon.SetActive(true);
        alertIcon.transform.localScale = Vector3.zero;

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1.2f, t / 0.4f);
            alertIcon.transform.localScale = new Vector3(s, s, s);
            if (mainBg) mainBg.color = Color.Lerp(new Color(0.05f, 0.15f, 0.05f, 1f), purple, t / 0.4f);
            yield return null;
        }

        // Phase 4: Warning pulse with lock icon
        if (lockIcon)
        {
            lockIcon.SetActive(true);
            lockIcon.transform.localScale = Vector3.one * 0.5f;
        }

        for (int i = 0; i < pulseCount; i++)
        {
            if (mainBg) mainBg.color = new Color(0.75f, 0, 0, 0.85f);
            if (alertIcon) alertIcon.transform.localScale = Vector3.one * 1.3f;
            if (lockIcon) lockIcon.transform.localPosition = new Vector3(0, -80f, 0);
            yield return new WaitForSeconds(pulseInterval * 0.5f);

            if (mainBg) mainBg.color = new Color(0.05f, 0, 0.05f, 1f);
            if (alertIcon) alertIcon.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(pulseInterval * 0.5f);
        }

        if (mainBg) mainBg.color = Color.black;
        HideAllIcons();

        // Phase 5: Fade to black
        yield return new WaitForSeconds(fadeOutDuration);

        crashCanvas.SetActive(false);
        Debug.Log("[CrashTp4] Done");
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

    void HideAllIcons()
    {
        if (shieldIcon) shieldIcon.SetActive(false);
        if (alertIcon) alertIcon.SetActive(false);
        if (lockIcon) lockIcon.SetActive(false);
    }

    GameObject CreateIcon(string name, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(crashCanvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        go.SetActive(false);

        // Add "!" text for alert icon
        if (name == "AlertIcon")
        {
            var atxt = new GameObject("Txt", typeof(RectTransform));
            atxt.transform.SetParent(go.transform, false);
            var tmp = atxt.AddComponent<TextMeshProUGUI>();
            tmp.text = "!";
            tmp.fontSize = 50;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var trt = atxt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        return go;
    }
}
