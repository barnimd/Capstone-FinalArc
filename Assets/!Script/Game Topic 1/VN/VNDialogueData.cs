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
/// Mood / facial expression for a portrait.
///   Default  = idle face (used when not the active speaker, or after a line finishes typing).
///   Talking  = mouth-open / speaking face (auto-applied to the active speaker WHILE text is typing).
///   Thinking = pensive face (set per-line via VNLine.mood).
///   Smiling  = happy face   (set per-line via VNLine.mood).
/// Add more values here as your portrait library grows (Sad, Angry, Surprised, ...).
/// </summary>
public enum VNExpression
{
    Default,
    Talking,
    Thinking,
    Smiling,
}

/// <summary>
/// One line of VN dialogue. Each line has its own speaker,
/// an optional mood (auto-applied) and an optional direct sprite override.
/// </summary>
[System.Serializable]
public class VNLine
{
    public VNSpeaker speaker = VNSpeaker.NPC;

    [Tooltip("Mood for this line.\n" +
             "  Default = AUTO: shows the speaker's Talking sprite while typing, " +
             "then reverts to their Default sprite.\n" +
             "  Thinking / Smiling = uses the matching sprite from the data " +
             "(no auto-revert; stays until next line).")]
    public VNExpression mood = VNExpression.Default;

    [Tooltip("Optional. Direct Sprite override for this single line — bypasses the mood lookup. " +
             "Leave empty to use the mood-based sprite.")]
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

    [Header("Player Portraits — Expressions")]
    [Tooltip("Default / idle sprite for the PLAYER. Used when not actively speaking " +
             "and as the fallback when an expression sprite below is empty.")]
    public Sprite playerPortrait;

    [Tooltip("Mouth-open / speaking sprite for the PLAYER. Auto-shown while typing. " +
             "Leave empty to keep the default sprite during talking.")]
    public Sprite playerTalkingPortrait;

    [Tooltip("Pensive / thinking sprite for the PLAYER. Used when a line's mood = Thinking. (Optional)")]
    public Sprite playerThinkingPortrait;

    [Tooltip("Happy / smiling sprite for the PLAYER. Used when a line's mood = Smiling. (Optional)")]
    public Sprite playerSmilingPortrait;

    [Header("NPC Portraits — Expressions")]
    [Tooltip("Default / idle sprite for the NPC.")]
    public Sprite npcPortrait;

    [Tooltip("Mouth-open / speaking sprite for the NPC. Auto-shown while typing. " +
             "Leave empty to keep the default sprite during talking.")]
    public Sprite npcTalkingPortrait;

    [Tooltip("Pensive / thinking sprite for the NPC. Used when a line's mood = Thinking. (Optional)")]
    public Sprite npcThinkingPortrait;

    [Tooltip("Happy / smiling sprite for the NPC. Used when a line's mood = Smiling. (Optional)")]
    public Sprite npcSmilingPortrait;

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

    /// <summary>
    /// Returns the portrait sprite for <paramref name="speaker"/> in the given
    /// <paramref name="mood"/>, falling back to the speaker's Default sprite
    /// if no expression-specific sprite is assigned.
    /// </summary>
    public Sprite GetExpressionSprite(VNSpeaker speaker, VNExpression mood)
    {
        // Narrator has no portrait of its own — fall through to NPC sprite.
        if (speaker == VNSpeaker.Player)
        {
            switch (mood)
            {
                case VNExpression.Talking:
                    return playerTalkingPortrait != null ? playerTalkingPortrait : playerPortrait;
                case VNExpression.Thinking:
                    return playerThinkingPortrait != null ? playerThinkingPortrait : playerPortrait;
                case VNExpression.Smiling:
                    return playerSmilingPortrait != null ? playerSmilingPortrait : playerPortrait;
                default:
                    return playerPortrait;
            }
        }
        else
        {
            switch (mood)
            {
                case VNExpression.Talking:
                    return npcTalkingPortrait != null ? npcTalkingPortrait : npcPortrait;
                case VNExpression.Thinking:
                    return npcThinkingPortrait != null ? npcThinkingPortrait : npcPortrait;
                case VNExpression.Smiling:
                    return npcSmilingPortrait != null ? npcSmilingPortrait : npcPortrait;
                default:
                    return npcPortrait;
            }
        }
    }
}
