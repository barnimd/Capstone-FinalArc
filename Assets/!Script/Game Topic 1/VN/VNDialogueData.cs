using UnityEngine;

/// <summary>
/// Speaker on a VN line. PLAYER lights up the left portrait,
/// NPC lights up the right portrait, NARRATOR dims both.
/// </summary>
public enum VNSpeaker
{
    Player,
    NPC,
    Narrator,
    NPC2   // karakter kanan ke-2, pakai slot portrait yang sama dengan NPC
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

    [Tooltip("Voice clip for this line. Leave empty = no voice (silent line).")]
    public AudioClip voiceClip;

    [Tooltip("Female voice for PLAYER lines. NPC lines continue using voiceClip. " +
             "Leave empty to keep the female player line silent; it never falls back to the male voice.")]
    public AudioClip femalePlayerVoiceClip;
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

    [Header("Female Player Portraits — Expressions")]
    [Tooltip("Female default / idle sprite. If empty, the legacy player portrait is used with a one-time warning.")]
    public Sprite femalePlayerPortrait;

    [Tooltip("Female mouth-open / speaking sprite. Falls back to the female default sprite.")]
    public Sprite femalePlayerTalkingPortrait;

    [Tooltip("Female thinking sprite. Falls back to the female default sprite.")]
    public Sprite femalePlayerThinkingPortrait;

    [Tooltip("Female smiling sprite. Falls back to the female default sprite.")]
    public Sprite femalePlayerSmilingPortrait;

    [Tooltip("Display name for NPC 2 (optional, hanya diisi kalau dialog punya 2 NPC).")]
    public string npc2Name = "";

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

    [Header("NPC 2 Portraits — Expressions (opsional, untuk dialog 2 NPC)")]
    [Tooltip("Default / idle sprite untuk NPC 2. Kosongkan kalau cuma 1 NPC.")]
    public Sprite npc2Portrait;

    [Tooltip("Sprite bicara untuk NPC 2.")]
    public Sprite npc2TalkingPortrait;

    [Tooltip("Sprite thinking untuk NPC 2. (Opsional)")]
    public Sprite npc2ThinkingPortrait;

    [Tooltip("Sprite smiling untuk NPC 2. (Opsional)")]
    public Sprite npc2SmilingPortrait;

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
        return GetExpressionSprite(speaker, mood, PlayerGender.Male);
    }

    /// <summary>
    /// Gender-aware portrait lookup. Gender only affects the Player slot;
    /// NPC, NPC2, and Narrator keep their existing portraits.
    /// </summary>
    public Sprite GetExpressionSprite(VNSpeaker speaker, VNExpression mood, PlayerGender playerGender)
    {
        if (speaker == VNSpeaker.Player)
        {
            if (playerGender == PlayerGender.Female)
            {
                Sprite femaleDefault = femalePlayerPortrait != null
                    ? femalePlayerPortrait
                    : playerPortrait;

                switch (mood)
                {
                    case VNExpression.Talking:  return femalePlayerTalkingPortrait  != null ? femalePlayerTalkingPortrait  : femaleDefault;
                    case VNExpression.Thinking: return femalePlayerThinkingPortrait != null ? femalePlayerThinkingPortrait : femaleDefault;
                    case VNExpression.Smiling:  return femalePlayerSmilingPortrait  != null ? femalePlayerSmilingPortrait  : femaleDefault;
                    default: return femaleDefault;
                }
            }

            switch (mood)
            {
                case VNExpression.Talking:  return playerTalkingPortrait  != null ? playerTalkingPortrait  : playerPortrait;
                case VNExpression.Thinking: return playerThinkingPortrait != null ? playerThinkingPortrait : playerPortrait;
                case VNExpression.Smiling:  return playerSmilingPortrait  != null ? playerSmilingPortrait  : playerPortrait;
                default: return playerPortrait;
            }
        }
        else if (speaker == VNSpeaker.NPC2)
        {
            switch (mood)
            {
                case VNExpression.Talking:  return npc2TalkingPortrait  != null ? npc2TalkingPortrait  : npc2Portrait;
                case VNExpression.Thinking: return npc2ThinkingPortrait != null ? npc2ThinkingPortrait : npc2Portrait;
                case VNExpression.Smiling:  return npc2SmilingPortrait  != null ? npc2SmilingPortrait  : npc2Portrait;
                default: return npc2Portrait;
            }
        }
        else // NPC atau Narrator
        {
            switch (mood)
            {
                case VNExpression.Talking:  return npcTalkingPortrait  != null ? npcTalkingPortrait  : npcPortrait;
                case VNExpression.Thinking: return npcThinkingPortrait != null ? npcThinkingPortrait : npcPortrait;
                case VNExpression.Smiling:  return npcSmilingPortrait  != null ? npcSmilingPortrait  : npcPortrait;
                default: return npcPortrait;
            }
        }
    }
}
