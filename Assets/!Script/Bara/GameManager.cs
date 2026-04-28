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

        if (scoreText != null)
            scoreText.text = "Score: " + finalScore;
        else
            Debug.LogWarning("[GameplayManager] scoreText (summary panel) not assigned in Inspector!");

        // Format waktu ke mm:ss (menit:detik)
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // {0:00} memastikan formatnya selalu dua digit (contoh: 05:09)
        timeText.text = "Waktu Penyelesaian: " + string.Format("{0:00}:{1:00}", minutes, seconds);

        // Tampilkan summary panel
        summaryPanel.SetActive(true);
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
}