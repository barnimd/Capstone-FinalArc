using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("Data Dialog NPC Ini")]
    public DialogueData dataDialogAwal;

    [Header("Referensi ke Manager")]
    public DialogueManager manager;

    void OnMouseDown()
    {
        if (manager != null && dataDialogAwal != null)
        {
            manager.StartDialogue(dataDialogAwal);
        }
        else
        {
            Debug.LogWarning("Manager atau Data Dialog belum dimasukkan ke Inspector NPC!");
        }
    }
}