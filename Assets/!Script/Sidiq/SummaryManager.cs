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

    // ── Internal ──────────────────────────────────────────────
    private float _startTime;
    private bool _triggered = false;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
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
        int finalScore = ScoreManager.instance != null ? ScoreManager.instance.score : 0;

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
            Transform summaryPanelChild = summaryPanel.transform.Find("SummaryPanel");
            if (summaryPanelChild != null)
                summaryPanelChild.gameObject.SetActive(true);
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
}
