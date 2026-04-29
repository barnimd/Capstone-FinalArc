using UnityEngine;

/// <summary>
/// Speaker on a VN line. PLAYER lights up the left portrait,
/// NPC lights up the right portrait, NARRATOR dims both.
/// </summary>
public enum VNSpeaker
{
    Player,
    NPC,
    Narrator
}

/// <summary>
/// One line of VN dialogue. Each line has its own speaker,
/// optional expression override (face sprite), and text.
/// </summary>
[System.Serializable]
public class VNLine
{
    public VNSpeaker speaker = VNSpeaker.NPC;

    [Tooltip("Optional. Override the default portrait/expression for this speaker " +
             "(e.g. angry, sad, surprised). If empty, the default portrait is used.")]
    public Sprite expression;

    [TextArea(2, 5)]
    public string text;
}

/// <summary>
/// VN-style dialogue ScriptableObject for Topic 1.
/// Drives the VNDialogueManager: which background, which portraits,
/// the line-by-line script, and end-of-dialogue choices.
/// </summary>
[CreateAssetMenu(fileName = "NewVNDialogue", menuName = "Dialogue System/VN Dialogue Data")]
public class VNDialogueData : ScriptableObject
{
    [Header("Speakers")]
    [Tooltip("Display name for the player (e.g. 'You', 'Andi').")]
    public string playerName = "You";

    [Tooltip("Display name for the NPC (e.g. 'Receptionist').")]
    public string npcName = "Receptionist";

    [Header("Default Portraits (Image 3 = male-vn, Image 4 = female-vn)")]
    [Tooltip("Default full-body / portrait sprite for the PLAYER (male-vn).")]
    public Sprite playerPortrait;

    [Tooltip("Default full-body / portrait sprite for the NPC (female-vn).")]
    public Sprite npcPortrait;

    [Header("Background (Image 8 = reception scene)")]
    [Tooltip("Background image shown behind the VN scene during this dialogue.")]
    public Sprite background;

    [Header("Script")]
    [Tooltip("Lines played in order. Set Speaker per line for portrait highlight.")]
    public VNLine[] lines;

    [Header("End-of-Dialogue Choice")]
    public bool hasChoice = false;
    public string acceptText = "Ya";
    public string rejectText = "Tidak";

    [Tooltip("Optional next VN dialogue if the player chooses Accept.")]
    public VNDialogueData dialogueIfAccepted;

    [Tooltip("Optional next VN dialogue if the player chooses Reject.")]
    public VNDialogueData dialogueIfRejected;

    [Header("Scoring / Flag")]
    [Tooltip("ID for scoring + analytics, e.g. 'VN_Receptionist_Topic1'.")]
    public string actionID;
}
