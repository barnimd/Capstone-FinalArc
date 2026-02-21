using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Informasi NPC")]
    public string npcName;

    [TextArea(3, 5)]
    [Tooltip("Isi percakapan secara berurutan. Pilihan akan muncul setelah baris terakhir.")]
    public string[] dialogueLines;

    [Header("Pilihan Player")]
    public string acceptText = "Ya";
    public string rejectText = "Nu'uh";
}