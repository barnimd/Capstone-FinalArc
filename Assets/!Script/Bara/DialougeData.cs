using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Informasi NPC")]
    public string npcName;

    [TextArea(3, 5)]
    [Tooltip("Isi percakapan secara berurutan")]
    public string[] dialogueLines;

    [Header("Tipe Dialog")]
    [Tooltip("Centang jika di akhir dialog player harus memilih Ya/Tidak")]
    public bool hasChoice;

    [Header("Pilihan Player")]
    public string acceptText = "Ya";
    public string rejectText = "Nu'uh";

    [Header("Reaksi NPC Setelah Memilih")]
    [Tooltip("Masukkan data dialog selanjutnya jika player setuju")]
    public DialogueData dialogueIfAccepted;

    [Tooltip("Masukkan data dialog selanjutnya jika player menolak")]
    public DialogueData dialogueIfRejected;

    [Header("Data Skor / Flag")]
    [Tooltip("ID unik untuk mencatat skor, misal: 'NPC 1'")]
    public string actionID;
}