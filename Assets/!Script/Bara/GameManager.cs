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

        // Ambil skor gabungan dari ScoreManager
        ScoreManager sm = ScoreManager.instance != null ? ScoreManager.instance : FindObjectOfType<ScoreManager>();
        if (sm == null)
            Debug.LogError("[GameplayManager] ScoreManager NOT FOUND in scene! Add a ScoreManager component to a GameObject.");

        int finalScore = (sm != null) ? sm.score : 0;
        Debug.Log("[GameplayManager] FinishGame — Final Score = " + finalScore);

        ResolveSummaryTextReferences();

        if (scoreText != null)
            scoreText.text = "Score: " + finalScore;
        else
            Debug.LogWarning("[GameplayManager] scoreText (summary panel) not assigned in Inspector!");

        // Format waktu ke mm:ss (menit:detik)
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // {0:00} memastikan formatnya selalu dua digit (contoh: 05:09)
        // timeText optional — panel summary boleh gak punya label waktu (jangan sampai NPE blokir panel)
        if (timeText != null)
            timeText.text = "Waktu Penyelesaian: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        else
            Debug.LogWarning("[GameplayManager] timeText not assigned — skipping time display.");

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
