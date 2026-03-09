using System.Collections; // Wajib ditambahkan untuk menggunakan Coroutine
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Button acceptButton;
    public Button rejectButton;

    [Header("Teks di Dalam Tombol")]
    public TextMeshProUGUI acceptButtonText;
    public TextMeshProUGUI rejectButtonText;

    [Header("Pengaturan Efek Mengetik")]
    public float typingSpeed = 0.04f; // Kecepatan ngetik per huruf
    public float skipDelay = 1.5f;    // Waktu tunggu sebelum teks boleh di-skip

    [Header("Legacy Interactable (backward compat)")]
    public Interactable bagi;

    [Header("Scoring")]
    [Tooltip("Score added when player accepts (e.g. -10 for penalty)")]
    public int acceptScore = 0;
    [Tooltip("Score added when player rejects (e.g. +10 for safe choice)")]
    public int rejectScore = 0;

    // Variabel internal
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private float nextClickTime = 0f;
    private PlayerMovement playerMovement;

    // Reference to the active NPCBubbleInteractable for callback
    private NPCBubbleInteractable activeNPCInteractable;

    // Variabel baru untuk efek ngetik
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string currentSentence;
    private float lineStartTime; // Mencatat kapan baris teks mulai diketik

    [Header("Data Skor Sementara")]
    public int totalDiterima = 0;
    public int totalDitolak = 0;

    void Start()
    {
        dialoguePanel.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        acceptButton.onClick.AddListener(OnAcceptClicked);
        rejectButton.onClick.AddListener(OnRejectClicked);
    }

    /// <summary>
    /// Called by NPCBubbleInteractable to register itself before starting dialogue.
    /// This way DialogueManager can call back OnDialogueEnd() when done.
    /// </summary>
    public void SetActiveInteractable(NPCBubbleInteractable npc)
    {
        activeNPCInteractable = npc;
    }

    public void StartDialogue(DialogueData data)
    {
        if (playerMovement != null)
            playerMovement.movementLocked = true;

        // Find player if we haven't yet
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerMovement = player.GetComponent<PlayerMovement>();
        }

        // Lock movement
        if (playerMovement != null)
            playerMovement.movementLocked = true;

        currentDialogue = data;
        currentLineIndex = 0;

        dialoguePanel.SetActive(true);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        npcNameText.text = currentDialogue.npcName;
        nextClickTime = Time.time + 0.2f;
        ShowLine();
    }

    void ShowLine()
    {
        // Ambil kalimat saat ini
        currentSentence = currentDialogue.dialogueLines[currentLineIndex];

        // Pastikan tombol mati saat sedang ngetik
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        // Hentikan coroutine sebelumnya jika ada, lalu mulai yang baru
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    // Fungsi Coroutine untuk mengetik per huruf
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; // Kosongkan teks dulu
        lineStartTime = Time.time; // Catat waktu mulai ngetik

        // Looping untuk memasukkan huruf satu per satu
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed); // Jeda antar huruf
        }

        // Kalau sudah selesai ngetik semua huruf secara natural
        FinishTyping();
    }

    // Fungsi untuk menyelesaikan ketikan (dipanggil saat selesai natural atau di-skip)
    void FinishTyping()
    {
        isTyping = false;
        dialogueText.text = currentSentence; // Pastikan teks tampil utuh

        // Mengecek apakah ini baris terakhir
        if (currentLineIndex == currentDialogue.dialogueLines.Length - 1)
        {
            // Jika baris terakhir dan butuh pilihan
            if (currentDialogue.hasChoice)
            {
                acceptButton.gameObject.SetActive(true);
                rejectButton.gameObject.SetActive(true);

                acceptButtonText.text = currentDialogue.acceptText;
                rejectButtonText.text = currentDialogue.rejectText;
            }
        }
    }

    void Update()
    {
        // Cek input jika panel aktif dan tombol belum muncul
        if (dialoguePanel.activeInHierarchy && !acceptButton.gameObject.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                if (Time.time < nextClickTime) return;

                if (isTyping)
                {
                    // Sedang ngetik: Cek apakah sudah lewat 1.5 detik sejak mulai ngetik
                    if (Time.time >= lineStartTime + skipDelay)
                    {
                        // Hentikan efek ngetik dan langsung tampilkan teks utuh
                        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                        FinishTyping();

                        // Beri jeda sedikit agar klik ini tidak terhitung sebagai klik "NextLine"
                        nextClickTime = Time.time + 0.5f;
                    }
                }
                else
                {
                    // Tidak sedang ngetik (teks sudah utuh): Lanjut ke baris berikutnya
                    NextLine();
                    nextClickTime = Time.time + 0.5f;
                }
            }
        }
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    public void OnAcceptClicked()
    {
        totalDiterima++;
        Debug.Log("Pilihan: Terima. ActionID: " + currentDialogue.actionID + " | Total Diterima: " + totalDiterima);

        // Apply score via ScoreManager if available
        if (ScoreManager.instance != null && acceptScore != 0)
            ScoreManager.instance.AddScore(acceptScore);

        if (currentDialogue.dialogueIfAccepted != null) StartDialogue(currentDialogue.dialogueIfAccepted);
        else EndDialogue();
    }

    public void OnRejectClicked()
    {
        totalDitolak++;
        Debug.Log("Pilihan: Tolak. ActionID: " + currentDialogue.actionID + " | Total Ditolak: " + totalDitolak);

        // Apply score via ScoreManager if available
        if (ScoreManager.instance != null && rejectScore != 0)
            ScoreManager.instance.AddScore(rejectScore);

        if (currentDialogue.dialogueIfRejected != null) StartDialogue(currentDialogue.dialogueIfRejected);
        else EndDialogue();
    }

    void EndDialogue()
    {
        // Legacy callback for old Interactable system
        if (bagi != null)
        {
            bagi.CloseDialogue();
        }

        // New callback for NPCBubbleInteractable system
        if (activeNPCInteractable != null)
        {
            activeNPCInteractable.OnDialogueEnd();
            activeNPCInteractable = null;
        }

        // Unlock player movement
        if (playerMovement != null)
            playerMovement.movementLocked = false;

        dialoguePanel.SetActive(false);
        currentDialogue = null;
    }
}