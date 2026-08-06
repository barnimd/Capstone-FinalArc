using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Untuk UI Text

public class GameplayManager : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Transform playerTransform; // Masukkan objek Player ke sini
    public DialogueManager dialogueManager; // Masukkan GameManager (yang ada DialogueManager-nya)

    [Header("Referensi UI Summary")]
    public GameObject summaryPanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;

    public GameObject canvasInstructor;

    // Variabel internal
    private Vector3 lastPlayerPosition;
    private bool isTimerRunning = false;
    private bool isGameFinished = false;
    private float elapsedTime = 0f;

    void Start()
    {
        ResolveSummaryTextReferences();

        // Opsional: topik yang summary-nya diurus SummaryManager (mis. Topic 2)
        // memakai script ini cuma sebagai timer, jadi summaryPanel sengaja kosong.
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        // Simpan posisi awal player & langsung mulai timer
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }

        // Mulai timer langsung — lebih reliable dari cek movement
        isTimerRunning = true;
        Debug.Log("[GameplayManager] Timer dimulai.");
    }

    void Update()
    {
        if (isGameFinished) return;

        // Tambahkan waktu selama timer berjalan
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    // Fungsi ini akan dipanggil oleh objek garis finis (collider)
    public void FinishGame()
    {
        if (isGameFinished) return; // Cegah terpanggil 2 kali

        isGameFinished = true;
        isTimerRunning = false;

        // Freeze the player the moment the game ends / summary panel appears, so they can't
        // keep moving behind the panel. Handles whichever movement controller the topic uses.
        PlayerFreezer.FreezeAll();

        // Ambil skor gabungan dari ScoreManager
        ScoreManager sm = ScoreManager.instance != null ? ScoreManager.instance : FindObjectOfType<ScoreManager>();
        if (sm == null)
            Debug.LogError("[GameplayManager] ScoreManager NOT FOUND in scene! Add a ScoreManager component to a GameObject.");

        int finalScore = (sm != null) ? sm.score : 0;
        Debug.Log("[GameplayManager] FinishGame — Final Score = " + finalScore);

        // Score, Time, Accuracy, and the player's Name are all filled by the shared summary
        // populator so every topic's CanvasSummaryPanel stays consistent. Topic 1 has no
        // quiz, so accuracy is derived from the final score (0–100 scale — score starts at 100
        // and drops on mistakes). Passing "" clears the "X / N correct" sub-text (no quiz here).
        int accuracy = Mathf.Clamp(finalScore, 0, 100);
        SummaryManager.PopulateSummaryTexts(summaryPanel, finalScore, elapsedTime, accuracy, null, "");

        // Tampilkan summary panel — sekaligus pastikan SEMUA parent-nya aktif.
        // Kalau nggak, panel "aktif" tapi tetap kehalang parent (root Canvas) yang non-aktif.
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);

            Transform p = summaryPanel.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                {
                    Debug.Log($"[GameplayManager] Mengaktifkan parent non-aktif agar panel terlihat: {p.name}");
                    p.gameObject.SetActive(true);
                }
                p = p.parent;
            }
            Debug.Log("[GameplayManager] Summary panel ditampilkan.");
        }
        else
            Debug.LogError("[GameplayManager] summaryPanel not assigned — cannot show summary panel!");
    }

    // Dipanggil oleh tombol di summary panel untuk kembali ke Dashboard
    public void GoToDashboard()
    {
        SceneManager.LoadScene("All_Menu");
    }

    public void CloseCanvas()
    {
        canvasInstructor.SetActive(false);
    }

    private void ResolveSummaryTextReferences()
    {
        if (summaryPanel == null)
            return;

        TextMeshProUGUI[] labels = summaryPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null)
                continue;

            if (scoreText == null && label.gameObject.name == "ScoreText")
                scoreText = label;
            else if (timeText == null && label.gameObject.name == "TimeText")
                timeText = label;
        }
    }
}
