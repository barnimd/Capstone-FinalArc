using UnityEngine;
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

    // Variabel internal
    private Vector3 lastPlayerPosition;
    private bool isTimerRunning = false;
    private bool isGameFinished = false;
    private float elapsedTime = 0f;

    void Start()
    {
        summaryPanel.SetActive(false);

        // Simpan posisi awal player saat game mulai
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
    }

    void Update()
    {
        if (isGameFinished) return;

        // 1. Cek apakah waktu belum jalan & player mulai bergerak
        if (!isTimerRunning && playerTransform != null)
        {
            // Jika posisi sekarang beda dengan posisi awal, mulai waktu!
            if (Vector3.Distance(playerTransform.position, lastPlayerPosition) > 0.01f)
            {
                isTimerRunning = true;
                Debug.Log("Player bergerak! Waktu dimulai.");
            }
        }

        // 2. Jika waktu berjalan, tambahkan terus hitungannya
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

        // Munculkan panel
        summaryPanel.SetActive(true);

        // Ambil data skor "Terima/Ya" dari DialogueManager
        int skorYa = dialogueManager.totalDiterima;
        scoreText.text = "Keputusan 'Ya' yang dipilih: " + skorYa;

        // Format waktu ke mm:ss (menit:detik)
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // {0:00} memastikan formatnya selalu dua digit (contoh: 05:09)
        timeText.text = "Waktu Penyelesaian: " + string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}