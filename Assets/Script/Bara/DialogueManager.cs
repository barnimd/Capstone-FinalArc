using UnityEngine;
using TMPro; // Wajib ditambahkan untuk menggunakan TextMeshPro
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

    // Variabel internal
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;

    [Header("Data Skor Sementara")]
    public int totalDiterima = 0;
    public int totalDitolak = 0;

    void Start()
    {
        // Pastikan panel dialog dan tombol mati saat game baru mulai
        dialoguePanel.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        // Memasang fungsi ke tombol agar saat diklik menjalankan void di bawah
        acceptButton.onClick.AddListener(OnAcceptClicked);
        rejectButton.onClick.AddListener(OnRejectClicked);
    }

    // Fungsi utama untuk memulai dialog
    public void StartDialogue(DialogueData data)
    {
        currentDialogue = data;
        currentLineIndex = 0;

        dialoguePanel.SetActive(true);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);

        npcNameText.text = currentDialogue.npcName;

        ShowLine();
    }

    void ShowLine()
    {
        // Menampilkan teks sesuai urutan array
        dialogueText.text = currentDialogue.dialogueLines[currentLineIndex];

        // Mengecek apakah ini adalah baris terakhir dari percakapan
        if (currentLineIndex == currentDialogue.dialogueLines.Length - 1)
        {
            // Jika ini baris terakhir dan NPC meminta sesuatu (hasChoice = true)
            if (currentDialogue.hasChoice)
            {
                acceptButton.gameObject.SetActive(true);
                rejectButton.gameObject.SetActive(true);

                // Ubah teks di tombol sesuai data
                acceptButtonText.text = currentDialogue.acceptText;
                rejectButtonText.text = currentDialogue.rejectText;
            }
        }
    }

    void Update()
    {
        // Jika panel aktif, tombol pilihan belum muncul, dan player klik kiri/spasi
        if (dialoguePanel.activeInHierarchy && !acceptButton.gameObject.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                NextLine();
            }
        }
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            ShowLine(); // Lanjut ke kalimat berikutnya
        }
        else
        {
            EndDialogue(); // Percakapan biasa selesai (tidak ada pilihan)
        }
    }

    // Dipanggil saat tombol "Terima" ditekan
    public void OnAcceptClicked()
    {
        totalDiterima++;
        Debug.Log("Pilihan: Terima. ActionID: " + currentDialogue.actionID + " | Total Diterima: " + totalDiterima);

        if (currentDialogue.dialogueIfAccepted != null)
        {
            StartDialogue(currentDialogue.dialogueIfAccepted); // Langsung lanjut ke dialog respon
        }
        else
        {
            EndDialogue();
        }
    }

    // Dipanggil saat tombol "Tolak" ditekan
    public void OnRejectClicked()
    {
        totalDitolak++;
        Debug.Log("Pilihan: Tolak. ActionID: " + currentDialogue.actionID + " | Total Ditolak: " + totalDitolak);

        if (currentDialogue.dialogueIfRejected != null)
        {
            StartDialogue(currentDialogue.dialogueIfRejected); // Langsung lanjut ke dialog respon
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentDialogue = null;
    }
}