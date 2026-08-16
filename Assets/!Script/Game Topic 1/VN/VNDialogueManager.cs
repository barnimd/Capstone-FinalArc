using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shared Visual-Novel style dialogue manager used by Topic 1-6 maps.
///
/// When triggered:
///   1. Fades in a full-screen VN canvas.
///   2. Swaps the background to the VN scene image (e.g. reception room).
///   3. Shows player portrait on the LEFT and NPC portrait on the RIGHT.
///   4. Highlights the active speaker (the other portrait dims).
///   5. Types the line text in a bottom dialogue box with speaker name.
///   6. Click / Space advances; holding Ctrl fast-forwards. Choices always wait for input.
///   7. On end, hides the VN canvas and unlocks player movement,
///      so the 2D pixel game world is visible again.
/// </summary>
public class VNDialogueManager : MonoBehaviour
{
    public static VNDialogueManager Instance { get; private set; }

    [Header("Root Canvas")]
    [Tooltip("Root GameObject of the VN UI (Canvas or Panel). " +
             "It is enabled when a VN dialogue starts and disabled when it ends.")]
    public GameObject vnRoot;

    [Header("Background")]
    public Image backgroundImage;

    [Header("Portraits")]
    public Image playerPortraitImage;
    public Image npcPortraitImage;

    [Tooltip("Optional 'PLAYER' label child of the player portrait. " +
             "Auto-hidden when a real portrait sprite is assigned in the data.")]
    public GameObject playerPortraitLabel;

    [Tooltip("Optional 'NPC' label child of the NPC portrait. " +
             "Auto-hidden when a real portrait sprite is assigned in the data.")]
    public GameObject npcPortraitLabel;

    [Tooltip("Color when the speaker is active (usually white).")]
    public Color activePortraitColor = Color.white;

    [Tooltip("Color when the speaker is inactive (dimmed). " +
             "Tip: dark grey with full alpha looks great.")]
    public Color inactivePortraitColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Dialogue Box")]
    public GameObject dialogueBox;
    [Tooltip("Optional. The Image component on the dialogue box. " +
             "If assigned, the manager will guarantee a visible dark background " +
             "(handy if the box's sprite is null).")]
    public Image dialogueBoxImage;
    [Tooltip("Color used for the dialogue box background (only applied when " +
             "dialogueBoxImage has no sprite assigned).")]
    public Color dialogueBoxColor = new Color(0f, 0f, 0f, 0.85f);
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject continueIndicator; // optional little arrow that pulses when ready

    [Header("Choice Buttons")]
    public Button acceptButton;
    public Button rejectButton;
    public TextMeshProUGUI acceptButtonText;
    public TextMeshProUGUI rejectButtonText;

    [Header("Typing")]
    [Tooltip("Seconds per character.")]
    public float typingSpeed = 0.035f;
    [Tooltip("Earliest time after a line starts where a click is allowed to skip.")]
    public float skipDelay = 0.4f;

    [Header("Fast Forward")]
    [Tooltip("Seconds between lines while Left Ctrl or Right Ctrl is held.")]
    [Min(0.01f)]
    [SerializeField] private float fastForwardLineInterval = 0.15f;

    [Header("Fade In / Out (cinematic black overlay)")]
    [Tooltip("Optional. Fullscreen black Image used to fade the screen to black " +
             "between gameplay <-> VN. Auto-created at runtime if left empty.")]
    public Image fadeOverlay;

    [Tooltip("Color of the fade overlay (default = solid black).")]
    public Color fadeColor = Color.black;

    [Tooltip("Seconds for one HALF of the fade-in (gameplay -> black, OR black -> VN). " +
             "Total fade-in time = 2 x this value.")]
    [Range(0f, 2f)] public float fadeInDuration = 0.5f;

    [Tooltip("Seconds for one HALF of the fade-out (VN -> black, OR black -> gameplay). " +
             "Total fade-out time = 2 x this value.")]
    [Range(0f, 2f)] public float fadeOutDuration = 0.5f;

    [Header("Voice Acting")]
    [Tooltip("AudioSource used to play per-line voice clips. " +
             "Add an AudioSource component on this GameObject and assign it here.")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Typewriter SFX")]
    [Tooltip("AudioSource for the per-character keypress click. Use a separate AudioSource from voiceSource.")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("Short keypress clip played on every character typed. Assign sfx_keypress.wav from Audio/SFX/.")]
    [SerializeField] private AudioClip keypressClip;
    [Tooltip("Volume for the keypress click (0–1). Keep low, e.g. 0.4.")]
    [Range(0f, 1f)] public float keypressVolume = 0.4f;

    [Header("Scoring")]
    [Tooltip("Score added on Accept. Set per-DialogueManager (e.g. -10 if Accept is unsafe).")]
    public int acceptScore = 0;
    [Tooltip("Score added on Reject. Set per-DialogueManager (e.g. +10 if Reject is safe).")]
    public int rejectScore = 0;

    [Header("Placeholder Fallback (used when sprite is missing on the data)")]
    [Tooltip("Tint applied to the Background image when the data has no background sprite.")]
    public Color placeholderBackgroundColor = new Color(0.20f, 0.25f, 0.32f, 1f);
    [Tooltip("Tint applied to the Player portrait when the data has no player sprite.")]
    public Color placeholderPlayerColor = new Color(0.30f, 0.55f, 0.85f, 1f);
    [Tooltip("Tint applied to the NPC portrait when the data has no NPC sprite.")]
    public Color placeholderNpcColor = new Color(0.85f, 0.45f, 0.65f, 1f);

    // Built-in fallback (1x1 white pixel sprite). Generated on demand.
    private static Sprite _whitePixelSprite;
    private static Sprite WhitePixelSprite
    {
        get
        {
            if (_whitePixelSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                _whitePixelSprite.name = "VN_WhitePixel";
            }
            return _whitePixelSprite;
        }
    }

    // ---- runtime state ----
    private VNDialogueData current;
    private int lineIndex;
    private bool isTyping;
    private Coroutine typingCo;
    private string fullSentence;
    private float lineStartTime;
    private float nextClickTime;
    private float nextFastForwardTime;
    private bool wasFastForwardHeld;
    private bool fastForwardBlockedUntilRelease;
    private string resolvedPlayerName = VNPlayerPresentationResolver.DefaultPlayerName;
    private readonly HashSet<string> warnedMissingFemaleAssets = new HashSet<string>();

    private PlayerMovement playerMovement;
    private VNNPCInteractable activeNPC;

    // Base colors per portrait — set in StartDialogue, then highlight modulates brightness.
    private Color playerBaseColor = Color.white;
    private Color npcBaseColor = Color.white;

    // Tracks siapa yang terakhir tampil di slot kanan (NPC atau NPC2).
    private VNSpeaker lastRightSpeaker = VNSpeaker.NPC;

    // Tracks the currently typing line so FinishTyping can revert the active
    // speaker's portrait from Talking back to Default (auto mode only).
    private VNSpeaker currentLineSpeaker;
    private bool autoRevertOnFinish;

    // Fade state
    private Coroutine fadeCo;
    private bool isFadingOut;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // IMPORTANT: do all initial UI state in Awake (NOT Start).
        //
        // If this GameObject was inactive at scene start, Awake/Start are
        // skipped. When the first E press calls StartDialogue and we
        // SetActive(true), Awake fires immediately (good). Awake resets
        // the UI to the closed state — then StartDialogue continues and
        // re-opens it correctly.
        //
        // If we did this in Start(), Start would fire AFTER StartDialogue
        // already activated vnRoot, instantly hiding the dialogue and
        // forcing the player to press E twice.
        if (vnRoot != null) vnRoot.SetActive(false);

        // Build / find the fullscreen fade overlay used for cinematic transitions.
        EnsureFadeOverlay();

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(false);
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(false);
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(OnRejectClicked);
        }
    }

    /// <summary>
    /// Called by VNNPCInteractable to register itself before starting dialogue.
    /// </summary>
    public void SetActiveInteractable(VNNPCInteractable npc)
    {
        activeNPC = npc;
    }

    /// <summary>
    /// Entry point: begin a VN dialogue. Locks the player, shows the VN UI,
    /// sets background + portraits, and types the first line.
    /// </summary>
    public void StartDialogue(VNDialogueData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[VNDialogueManager] StartDialogue called with null data.");
            return;
        }

        // The manager's own GameObject MUST be active or coroutines (typing effect)
        // and Update() (click-to-advance) won't run. Auto-recover if a user disabled it.
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("[VNDialogueManager] '" + gameObject.name +
                "' was inactive — auto-enabling so the VN dialogue can run. " +
                "Tip: only toggle VN_Root, never the VN_Canvas itself.");
            gameObject.SetActive(true);
        }

        if (playerMovement == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerMovement = p.GetComponentInParent<PlayerMovement>() ?? p.GetComponent<PlayerMovement>();
        }
        if (playerMovement != null) playerMovement.movementLocked = true;

        isFadingOut = false;

        // Branch: if VN is ALREADY visible (chained dialogue from Accept/Reject),
        // just swap content directly without any fade. Feels continuous.
        bool chained = vnRoot != null && vnRoot.activeInHierarchy
                       && (fadeOverlay == null || fadeOverlay.color.a < 0.01f);

        if (chained)
        {
            ApplyDialogueData(data);
            nextClickTime = Time.time + 0.2f;
            ShowLine();
            return;
        }

        // Fresh entry — animate the cinematic fade.
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeInAndStart(data));
    }

    /// <summary>
    /// Applies the data to the UI (sprites, background, labels, dialogue box).
    /// Does NOT call ShowLine — caller decides when typing should begin.
    /// </summary>
    void ApplyDialogueData(VNDialogueData data)
    {
        current = data;
        lineIndex = 0;
        resolvedPlayerName = VNPlayerPresentationResolver.ResolveDisplayName(data.playerName);
        PlayerGender playerGender = VNPlayerPresentationResolver.CurrentGender;
        Sprite resolvedPlayerPortrait = data.GetExpressionSprite(
            VNSpeaker.Player, VNExpression.Default, playerGender);

        if (playerGender == PlayerGender.Female && data.femalePlayerPortrait == null)
            WarnMissingFemaleAssetOnce(data, -1, "portrait; using the legacy player portrait");

        if (vnRoot != null) vnRoot.SetActive(true);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);

        // Hide PLAYER / NPC placeholder labels when real portrait sprites are provided.
        if (playerPortraitLabel != null)
            playerPortraitLabel.SetActive(resolvedPlayerPortrait == null);
        if (npcPortraitLabel != null)
            npcPortraitLabel.SetActive(data.npcPortrait == null);

        // Guarantee the dialogue box is visible — fill in a dark background
        // even if the box was created without a sprite.
        if (dialogueBoxImage != null)
        {
            if (dialogueBoxImage.sprite == null)
            {
                dialogueBoxImage.sprite = WhitePixelSprite;
                dialogueBoxImage.color = dialogueBoxColor;
            }
            dialogueBoxImage.enabled = true;
        }

        // Background — fall back to a tinted placeholder if missing
        if (backgroundImage != null)
        {
            if (data.background != null)
            {
                backgroundImage.sprite = data.background;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.sprite = WhitePixelSprite;
                backgroundImage.color = placeholderBackgroundColor;
            }
            backgroundImage.enabled = true;
        }

        // Default portraits — fall back to tinted placeholders if missing
        if (playerPortraitImage != null)
        {
            if (resolvedPlayerPortrait != null)
            {
                playerPortraitImage.sprite = resolvedPlayerPortrait;
                playerBaseColor = Color.white;
            }
            else
            {
                playerPortraitImage.sprite = WhitePixelSprite;
                playerBaseColor = placeholderPlayerColor;
            }
            playerPortraitImage.enabled = true;
        }
        if (npcPortraitImage != null)
        {
            if (data.npcPortrait != null)
            {
                npcPortraitImage.sprite = data.npcPortrait;
                npcBaseColor = Color.white;
            }
            else
            {
                npcPortraitImage.sprite = WhitePixelSprite;
                npcBaseColor = placeholderNpcColor;
            }
            npcPortraitImage.enabled = true;
        }
    }

    IEnumerator FadeInAndStart(VNDialogueData data)
    {
        // Phase 1: gameplay -> black
        SetOverlayActive(true);
        yield return FadeOverlayTo(1f, fadeInDuration);

        // Screen is fully black — safe moment for room transitions (teleport is invisible).
        if (activeNPC != null) activeNPC.OnFadeToBlack();

        // Setup VN content while the screen is fully black.
        ApplyDialogueData(data);

        // Phase 2: black -> VN (typing starts the moment we begin revealing).
        nextClickTime = Time.time + fadeInDuration + 0.1f;
        ShowLine();
        yield return FadeOverlayTo(0f, fadeInDuration);

        SetOverlayActive(false);
        fadeCo = null;
    }

    void ShowLine()
    {
        if (current == null || current.lines == null || lineIndex >= current.lines.Length)
        {
            EndDialogue();
            return;
        }

        VNLine line = current.lines[lineIndex];
        PlayerGender playerGender = VNPlayerPresentationResolver.CurrentGender;

        // Resolve portraits based on the line's mood + per-line override.
        // The INACTIVE speaker always uses the Default sprite (listening face).
        // The ACTIVE speaker uses:
        //   - line.expression (Sprite) if assigned (highest priority manual override), OR
        //   - the Talking sprite when mood == Default (auto-revert to Default after typing), OR
        //   - the matching mood sprite otherwise (Thinking / Smiling — stays put).
        currentLineSpeaker = line.speaker;
        bool femalePlayerLine = line.speaker == VNSpeaker.Player && playerGender == PlayerGender.Female;
        autoRevertOnFinish = (line.mood == VNExpression.Default &&
                              (line.expression == null || femalePlayerLine));

        Sprite activeSprite;
        if (line.expression != null && !femalePlayerLine)
            activeSprite = line.expression;
        else if (line.mood == VNExpression.Default)
            activeSprite = current.GetExpressionSprite(line.speaker, VNExpression.Talking, playerGender);
        else
            activeSprite = current.GetExpressionSprite(line.speaker, line.mood, playerGender);

        Sprite playerSprite = (line.speaker == VNSpeaker.Player)
            ? activeSprite
            : current.GetExpressionSprite(VNSpeaker.Player, VNExpression.Default, playerGender);

        // Slot kanan dipakai bersama NPC dan NPC2 — swap sprite tergantung siapa yang bicara.
        Sprite npcSprite;
        if (line.speaker == VNSpeaker.NPC || line.speaker == VNSpeaker.NPC2)
        {
            npcSprite = activeSprite;
            lastRightSpeaker = line.speaker;
            // Update base color sesuai siapa yang sekarang di slot kanan.
            bool hasSprite = line.speaker == VNSpeaker.NPC2
                ? current.npc2Portrait != null
                : current.npcPortrait != null;
            npcBaseColor = hasSprite ? Color.white : placeholderNpcColor;
        }
        else
        {
            // Player sedang bicara — slot kanan tetap tampilkan NPC terakhir dalam pose default.
            npcSprite = current.GetExpressionSprite(lastRightSpeaker, VNExpression.Default);
        }

        if (playerPortraitImage != null && playerSprite != null)
            playerPortraitImage.sprite = playerSprite;
        if (npcPortraitImage != null && npcSprite != null)
            npcPortraitImage.sprite = npcSprite;

        // Highlight active speaker
        ApplySpeakerHighlight(line.speaker);

        // Speaker name
        if (speakerNameText != null)
        {
            switch (line.speaker)
            {
                case VNSpeaker.Player: speakerNameText.text = resolvedPlayerName; break;
                case VNSpeaker.NPC:    speakerNameText.text = current.npcName; break;
                case VNSpeaker.NPC2:   speakerNameText.text = current.npc2Name; break;
                default: speakerNameText.text = ""; break;
            }
        }

        // Hide buttons while typing
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Voice acting — stop previous clip then play this line's clip (if assigned)
        bool fastForwarding = IsFastForwardActive();

        // Checkbox "Dialogue" di Settings mematikan voice + klik ketik
        bool dialogueAudioOn = GameSettings.DialogueEnabled;
        AudioClip resolvedVoiceClip = VNPlayerPresentationResolver.ResolveVoiceClip(line, playerGender);

        if (playerGender == PlayerGender.Female &&
            line.speaker == VNSpeaker.Player &&
            !VNPlayerPresentationResolver.ContainsDynamicPlayerName(line.text) &&
            line.femalePlayerVoiceClip == null)
        {
            WarnMissingFemaleAssetOnce(current, lineIndex, "voice; line will be silent");
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            if (dialogueAudioOn && !fastForwarding && resolvedVoiceClip != null)
                voiceSource.PlayOneShot(resolvedVoiceClip);
        }

        // Single click SFX on each new line (not per character)
        if (dialogueAudioOn && !fastForwarding && sfxSource != null && keypressClip != null)
            sfxSource.PlayOneShot(keypressClip, keypressVolume);

        // Type
        fullSentence = VNPlayerPresentationResolver.ResolveLineText(line.text, resolvedPlayerName);
        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeSentence(fullSentence));
    }

    void ApplySpeakerHighlight(VNSpeaker speaker)
    {
        if (playerPortraitImage != null)
        {
            bool active = (speaker == VNSpeaker.Player);
            playerPortraitImage.color = ModulatePortrait(playerBaseColor, active);
        }
        if (npcPortraitImage != null)
        {
            // NPC dan NPC2 keduanya pakai slot kanan yang sama.
            bool active = (speaker == VNSpeaker.NPC || speaker == VNSpeaker.NPC2);
            npcPortraitImage.color = ModulatePortrait(npcBaseColor, active);
        }
    }

    /// <summary>
    /// Multiply the base portrait color by the active/inactive tint so placeholders
    /// keep their identity color while the speaker highlight still works.
    /// </summary>
    Color ModulatePortrait(Color baseColor, bool isActive)
    {
        Color tint = isActive ? activePortraitColor : inactivePortraitColor;
        return new Color(baseColor.r * tint.r,
                         baseColor.g * tint.g,
                         baseColor.b * tint.b,
                         baseColor.a * tint.a);
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (dialogueText != null) dialogueText.text = "";
        lineStartTime = Time.time;

        foreach (char c in sentence)
        {
            if (dialogueText != null) dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    void FinishTyping()
    {
        typingCo = null;
        isTyping = false;
        if (dialogueText != null) dialogueText.text = fullSentence;
        if (continueIndicator != null) continueIndicator.SetActive(true);

        // Auto-revert the active speaker from Talking back to Default once the
        // line is fully typed (only when this line was in auto / Default mood).
        // Lines with mood = Thinking / Smiling keep their expression until the
        // next line replaces it.
        if (autoRevertOnFinish && current != null)
        {
            Sprite defaultSprite = current.GetExpressionSprite(
                currentLineSpeaker,
                VNExpression.Default,
                VNPlayerPresentationResolver.CurrentGender);
            if (defaultSprite != null)
            {
                if (currentLineSpeaker == VNSpeaker.Player && playerPortraitImage != null)
                    playerPortraitImage.sprite = defaultSprite;
                else if ((currentLineSpeaker == VNSpeaker.NPC || currentLineSpeaker == VNSpeaker.NPC2) && npcPortraitImage != null)
                    npcPortraitImage.sprite = defaultSprite;
            }
        }

        // If this is the last line and the dialogue has a choice, show buttons
        bool isLastLine = (current != null && lineIndex == current.lines.Length - 1);
        if (isLastLine && current.hasChoice)
        {
            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(true);
                if (acceptButtonText != null) acceptButtonText.text = current.acceptText;
            }
            if (rejectButton != null)
            {
                rejectButton.gameObject.SetActive(true);
                if (rejectButtonText != null) rejectButtonText.text = current.rejectText;
            }
            if (continueIndicator != null) continueIndicator.SetActive(false);

            // Never cross a choice automatically. If Ctrl is still held,
            // require a release before fast-forward can resume in the branch.
            if (IsFastForwardKeyHeld())
                fastForwardBlockedUntilRelease = true;
        }
    }

    void WarnMissingFemaleAssetOnce(VNDialogueData data, int affectedLineIndex, string detail)
    {
        string assetName = data != null ? data.name : "UnknownDialogue";
        string key = assetName + ":" + affectedLineIndex + ":" + detail;
        if (!warnedMissingFemaleAssets.Add(key))
            return;

        string lineLabel = affectedLineIndex >= 0 ? " line " + (affectedLineIndex + 1) : string.Empty;
        Debug.LogWarning("[VNDialogueManager] Missing female " + detail + " in " +
                         assetName + lineLabel + ".");
    }

    void Update()
    {
        bool fastForwardHeld = IsFastForwardKeyHeld();

        if (!fastForwardHeld)
        {
            wasFastForwardHeld = false;
            fastForwardBlockedUntilRelease = false;
        }

        if (vnRoot == null || !vnRoot.activeInHierarchy) return;

        if (AreChoiceButtonsVisible())
        {
            if (fastForwardHeld)
                fastForwardBlockedUntilRelease = true;
            return;
        }

        // Don't accept input while fading in or out.
        if (isFadingOut) return;
        if (fadeOverlay != null && fadeOverlay.color.a > 0.01f) return;

        if (fastForwardHeld)
        {
            if (fastForwardBlockedUntilRelease) return;

            if (!wasFastForwardHeld)
            {
                wasFastForwardHeld = true;
                nextFastForwardTime = Time.time;
                StopFastForwardAudio();
            }

            // Ctrl bypasses the normal skip delay. Reveal the current sentence
            // first, then advance at a controlled interval while it stays held.
            if (isTyping)
            {
                if (typingCo != null)
                {
                    StopCoroutine(typingCo);
                    typingCo = null;
                }

                StopFastForwardAudio();
                FinishTyping();
                nextFastForwardTime = Time.time + fastForwardLineInterval;
            }
            else if (Time.time >= nextFastForwardTime)
            {
                NextLine();
                nextFastForwardTime = Time.time + fastForwardLineInterval;
            }

            return;
        }

        if (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space)) return;
        if (Time.time < nextClickTime) return;

        // Two-stage click (classic VN feel):
        //   - While typing AND skipDelay has passed: finish typing (skip animation),
        //     stay on the same line so the player can read the full text.
        //   - When typing is finished: advance to the next line.
        if (isTyping)
        {
            if (Time.time >= lineStartTime + skipDelay)
            {
                if (typingCo != null)
                {
                    StopCoroutine(typingCo);
                    typingCo = null;
                }
                FinishTyping();
                nextClickTime = Time.time + 0.25f;
            }
        }
        else
        {
            NextLine();
            nextClickTime = Time.time + 0.25f;
        }
    }

    bool IsFastForwardKeyHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    bool IsFastForwardActive()
    {
        return IsFastForwardKeyHeld() && !fastForwardBlockedUntilRelease;
    }

    bool AreChoiceButtonsVisible()
    {
        bool acceptVisible = acceptButton != null && acceptButton.gameObject.activeInHierarchy;
        bool rejectVisible = rejectButton != null && rejectButton.gameObject.activeInHierarchy;
        return acceptVisible || rejectVisible;
    }

    void StopFastForwardAudio()
    {
        if (voiceSource != null)
            voiceSource.Stop();

        if (sfxSource != null && sfxSource != voiceSource)
            sfxSource.Stop();
    }

    void NextLine()
    {
        lineIndex++;
        if (current != null && lineIndex < current.lines.Length) ShowLine();
        else EndDialogue();
    }

    public void OnAcceptClicked()
    {
        PlayerRunRecorder.Record("vn." + (current != null ? current.name : "unknown"), "accept");
        if (ScoreManager.instance != null && acceptScore != 0)
            ScoreManager.instance.AddScore(acceptScore);

        if (activeNPC != null) activeNPC.OnDialogueAccepted();

        if (current != null && current.dialogueIfAccepted != null)
            StartDialogue(current.dialogueIfAccepted);
        else
            EndDialogue();
    }

    public void OnRejectClicked()
    {
        PlayerRunRecorder.Record("vn." + (current != null ? current.name : "unknown"), "reject");
        if (ScoreManager.instance != null && rejectScore != 0)
            ScoreManager.instance.AddScore(rejectScore);

        if (activeNPC != null) activeNPC.OnDialogueRejected();

        if (current != null && current.dialogueIfRejected != null)
            StartDialogue(current.dialogueIfRejected);
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        if (isFadingOut) return; // already closing — ignore re-entry

        // Hide buttons / continue indicator immediately so the fade looks clean.
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (rejectButton != null) rejectButton.gameObject.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (fadeOverlay != null && vnRoot != null && vnRoot.activeInHierarchy && fadeOutDuration > 0f)
        {
            isFadingOut = true;
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeOutAndClose());
        }
        else
        {
            FinalizeEnd();
        }
    }

    IEnumerator FadeOutAndClose()
    {
        // Phase 1: VN -> black
        SetOverlayActive(true);
        yield return FadeOverlayTo(1f, fadeOutDuration);

        // Tear down VN while fully black.
        if (typingCo != null) { StopCoroutine(typingCo); typingCo = null; }
        isTyping = false;
        if (vnRoot != null) vnRoot.SetActive(false);

        // Phase 2: black -> gameplay
        yield return FadeOverlayTo(0f, fadeOutDuration);
        SetOverlayActive(false);

        FinalizeEnd();
    }

    void FinalizeEnd()
    {
        if (typingCo != null) { StopCoroutine(typingCo); typingCo = null; }
        isTyping = false;
        if (voiceSource != null) voiceSource.Stop();

        if (vnRoot != null) vnRoot.SetActive(false);
        if (playerMovement != null) playerMovement.movementLocked = false;

        if (activeNPC != null)
        {
            activeNPC.OnDialogueEnd();
            activeNPC = null;
        }

        current = null;
        isFadingOut = false;
        fadeCo = null;
        nextFastForwardTime = 0f;
        wasFastForwardHeld = false;
        fastForwardBlockedUntilRelease = false;
    }

    // -----------------------------------------------------------------
    // Fade overlay helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Builds the fullscreen black overlay used for cinematic fades and
    /// caches it. Called from Awake so the overlay is ready before any
    /// dialogue starts. Safe to call multiple times.
    /// </summary>
    void EnsureFadeOverlay()
    {
        if (fadeOverlay != null) return;

        // Place the overlay as a child of THIS canvas (VN_Canvas) so it
        // renders on top of vnRoot. As the last sibling it draws above
        // every VN element, including portraits and the dialogue box.
        Transform parent = (vnRoot != null && vnRoot.transform.parent != null)
            ? vnRoot.transform.parent
            : transform;

        GameObject go = new GameObject("VN_FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeOverlay = go.GetComponent<Image>();
        fadeOverlay.raycastTarget = false; // never block clicks underneath
        Color c = fadeColor; c.a = 0f;
        fadeOverlay.color = c;
        go.SetActive(false);
    }

    void SetOverlayActive(bool active)
    {
        if (fadeOverlay == null) return;
        if (fadeOverlay.gameObject.activeSelf != active)
            fadeOverlay.gameObject.SetActive(active);
        fadeOverlay.transform.SetAsLastSibling(); // always render on top
    }

    /// <summary>
    /// Lerps the overlay alpha to <paramref name="targetAlpha"/> over
    /// <paramref name="duration"/> seconds. Uses unscaled time so the
    /// transition still plays while the game is paused.
    /// </summary>
    IEnumerator FadeOverlayTo(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        Color baseColor = fadeColor;
        float startAlpha = fadeOverlay.color.a;

        if (duration <= 0f)
        {
            baseColor.a = targetAlpha;
            fadeOverlay.color = baseColor;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            baseColor.a = a;
            fadeOverlay.color = baseColor;
            yield return null;
        }
        baseColor.a = targetAlpha;
        fadeOverlay.color = baseColor;
    }
}
