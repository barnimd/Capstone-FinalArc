using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the end-game flow:
/// 1. Tracks time from Start() until TriggerSummary() is called.
/// 2. Fades screen to black.
/// 3. Activates CanvasSummaryPanel and populates Score + Time.
///
/// Setup:
/// 1. Attach this script to a persistent GameObject (e.g. "GameManager").
/// 2. Assign all fields in the Inspector.
/// 3. Call SummaryManager.instance.TriggerSummary() as the finish trigger
///    — e.g. from a "Close" button UnityEvent, or from InstallerFlow / PasswordValidator.
/// </summary>
public class SummaryManager : MonoBehaviour
{
    public static SummaryManager instance;

    [Header("Fade Overlay")]
    [Tooltip("A full-screen black Image (Canvas → Panel with black color, alpha starts at 0). " +
             "Make sure its Canvas Sort Order is higher than all other canvases.")]
    public Image fadeOverlay;

    [Tooltip("How long the fade-to-black takes in seconds.")]
    public float fadeDuration = 1.0f;

    [Header("Summary Panel")]
    [Tooltip("The root CanvasSummaryPanel GameObject (set inactive in Inspector).")]
    public GameObject summaryPanel;

    [Tooltip("TextMeshPro text that shows the level-complete message.")]
    public TMP_Text levelCompleteText;

    [Tooltip("TextMeshPro text that shows the final score.")]
    public TMP_Text scoreText;

    [Tooltip("TextMeshPro text that shows the elapsed time.")]
    public TMP_Text timeText;

    [Header("Backend / Neon")]
    [Tooltip("Stage ID untuk disimpan ke Neon. Kosongkan kalau scene ini tidak menyimpan skor. " +
             "Contoh: Topic 2 (Office_Environment) = 2fa.")]
    public string stageId = "";
    [Tooltip("Server menolak skor di atas nilai ini. Skor akhir di-clamp ke [0, maxScore] sebelum dikirim.")]
    public int maxScore = 100;

    // ── Internal ──────────────────────────────────────────────
    private float _startTime;
    private bool _triggered = false;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        ResolveSummaryTextReferences();
    }

    private void Start()
    {
        _startTime = Time.time;

        // Ensure overlay is invisible at game start
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Call this when all mini-games are done.
    /// Safe to call from UnityEvent, button onClick, or any other script.
    /// </summary>
    public void TriggerSummary()
    {
        if (_triggered) return; // prevent double-trigger
        _triggered = true;

        float elapsed = Time.time - _startTime;
        StartCoroutine(SummaryRoutine(elapsed));
    }

    private IEnumerator SummaryRoutine(float elapsed)
    {
        // ── 1. Fade to black ──────────────────────────────────
        if (fadeOverlay != null)
        {
            float t = 0f;
            Color c = fadeOverlay.color;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Clamp01(t / fadeDuration);
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeOverlay.color = c;
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        // ── 2. Populate Summary Panel ─────────────────────────
        ResolveSummaryTextReferences();

        int finalScore = ScoreManager.instance != null
            ? ScoreManager.instance.ClampScore(0, maxScore)
            : 0;

        // Simpan hasil ke Neon (kalau scene ini punya stageId)
        SubmitResultToNeon(finalScore);

        if (levelCompleteText != null)
            levelCompleteText.text = "Level Complete!";

        if (scoreText != null)
            scoreText.text = "Score: " + finalScore;

        if (timeText != null)
        {
            int m = Mathf.FloorToInt(elapsed / 60f);
            int s = Mathf.FloorToInt(elapsed % 60f);
            timeText.text = $"Time: {m:00}:{s:00}";
        }

        // ── 3. Show Summary Panel ─────────────────────────────
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true); // aktifkan CanvasSummaryPanel
                                          // Force aktifkan SummaryPannel (child index 1, sesuai hierarchy)
            Transform summaryPannel = summaryPanel.transform.Find("SummaryPannel");
            if (summaryPannel != null)
                summaryPannel.gameObject.SetActive(true);
        }

        // ── 4. Fade overlay back out so panel is visible ──────
        if (fadeOverlay != null)
        {
            float t = 0f;
            Color c = fadeOverlay.color;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Clamp01(1f - (t / fadeDuration));
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = 0f;
            fadeOverlay.color = c;
        }
    }

    // ── Neon score persistence ────────────────────────────────────────────────

    /// <summary>
    /// Kirim skor akhir ke Neon. No-op (aman) kalau stageId kosong, manager backend
    /// tidak ada, atau player belum login. Server menyimpan skor tertinggi per stage.
    /// </summary>
    private void ResolveSummaryTextReferences()
    {
        if (summaryPanel == null)
            return;

        TMP_Text[] labels = summaryPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;

            switch (label.gameObject.name)
            {
                case "LevelCompleteText":
                    if (levelCompleteText == null) levelCompleteText = label;
                    break;
                case "ScoreText":
                    if (scoreText == null) scoreText = label;
                    break;
                case "TimeText":
                    if (timeText == null) timeText = label;
                    break;
            }
        }
    }

    private void SubmitResultToNeon(int rawScore)
    {
        if (string.IsNullOrEmpty(stageId))
            return; // scene ini tidak menyimpan skor ke server

        int finalScore = Mathf.Clamp(rawScore, 0, maxScore);
        Debug.Log($"[Summary] 🏁 Stage '{stageId}' selesai — final score siap (raw={rawScore}, dikirim={finalScore}). Mulai simpan...");

        if (StageManager.Instance == null ||
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning($"[Summary] ⚠️ Save DIBATALKAN untuk '{stageId}' — backend manager hilang / belum login " +
                             $"(StageManager={(StageManager.Instance != null)}, " +
                             $"Firebase={(FirebaseManager.Instance != null)}, " +
                             $"login={(FirebaseManager.Instance != null && FirebaseManager.Instance.IsAuthenticated)}). " +
                             $"Skor {finalScore} cuma kesimpen lokal.");
            return;
        }

        if (APIClient.Instance != null)
        {
            Debug.Log("[Summary] 📤 Sedang menyimpan ke Vercel — langkah 1/2: sinkronisasi data user...");
            APIClient.Instance.UserSync(FirebaseManager.Instance.Username, (ok, resp, raw) =>
            {
                if (ok)
                    Debug.Log("[Summary] ✅ Langkah 1/2 OK — user tersinkron. Lanjut simpan hasil stage...");
                else
                    Debug.LogWarning("[Summary] ⚠️ Langkah 1/2 (UserSync) gagal, lanjut tetap coba simpan: " + raw);
                SendComplete(finalScore);
            });
        }
        else
        {
            SendComplete(finalScore);
        }
    }

    private void SendComplete(int finalScore)
    {
        Debug.Log($"[Summary] 📤 Sedang menyimpan ke Vercel — langkah 2/2: kirim hasil stage '{stageId}' (score={finalScore})...");
        if (APIClient.Instance != null)
        {
            APIClient.Instance.StageComplete(stageId, finalScore, (ok, resp, raw) =>
            {
                if (ok && resp != null && resp.success)
                    Debug.Log($"[Summary] ✅ SAVE BERHASIL MASUK DB — stage '{stageId}', finalScore={finalScore}. (Neon menyimpan skor tertinggi)");
                else
                    Debug.LogError($"[Summary] ❌ SAVE GAGAL ke DB untuk stage '{stageId}' (score {finalScore}). Respon server: {raw}");
            });
        }
        else
        {
            StageManager.Instance.Complete(stageId, finalScore, success =>
            {
                if (success)
                    Debug.Log($"[Summary] ✅ SAVE BERHASIL MASUK DB — stage '{stageId}', finalScore={finalScore}.");
                else
                    Debug.LogError($"[Summary] ❌ SAVE GAGAL ke DB untuk stage '{stageId}' — score {finalScore} cuma kesimpen lokal.");
            });
        }
    }
}
