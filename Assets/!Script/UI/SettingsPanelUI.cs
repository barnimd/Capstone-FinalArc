using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Nyambungin UI Settings ke <see cref="GameSettings"/>.
/// Dipakai di 2 tempat: panel Settings di All_Menu, dan panel Pause in-game.
///
/// Setup:
///   1. Taruh script ini di GameObject panel setting (mis. SettingsPanel).
///   2. Assign Slider movement + 2 Toggle (music, dialogue) di Inspector.
///   3. Selesai — nilai auto-load saat panel aktif, auto-save saat diubah.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Slider 0..1. Set Min Value = 0, Max Value = 1, Whole Numbers = off.")]
    [SerializeField] private Slider moveSpeedSlider;
    [Tooltip("Opsional. Nampilin nilai slider sebagai persen, mis. \"50%\".")]
    [SerializeField] private TMP_Text moveSpeedValueText;

    [Header("Sound")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle dialogueToggle;

    [Header("Optional")]
    [Tooltip("Opsional. Tombol balikin semua setting ke default.")]
    [SerializeField] private Button resetButton;

    // Guard supaya SetValueWithoutNotify tidak perlu — kita blokir callback
    // selama panel lagi nulis nilai awal ke UI.
    private bool _syncing;

    private void Awake()
    {
        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.minValue = 0f;
            moveSpeedSlider.maxValue = 1f;
            moveSpeedSlider.wholeNumbers = false;
            moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedChanged);
        }

        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicChanged);

        if (dialogueToggle != null)
            dialogueToggle.onValueChanged.AddListener(OnDialogueChanged);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
    }

    // Tiap panel dibuka, tarik ulang nilai terbaru dari GameSettings.
    private void OnEnable() => PullFromSettings();

    private void OnDestroy()
    {
        if (moveSpeedSlider != null) moveSpeedSlider.onValueChanged.RemoveListener(OnMoveSpeedChanged);
        if (musicToggle != null)     musicToggle.onValueChanged.RemoveListener(OnMusicChanged);
        if (dialogueToggle != null)  dialogueToggle.onValueChanged.RemoveListener(OnDialogueChanged);
        if (resetButton != null)     resetButton.onClick.RemoveListener(OnResetClicked);
    }

    // ── UI → Settings ────────────────────────────────────────────────────────

    private void OnMoveSpeedChanged(float value)
    {
        if (_syncing) return;
        GameSettings.MoveSpeed01 = value;
        UpdateSpeedLabel(value);
    }

    private void OnMusicChanged(bool on)
    {
        if (_syncing) return;
        GameSettings.MusicEnabled = on;
    }

    private void OnDialogueChanged(bool on)
    {
        if (_syncing) return;
        GameSettings.DialogueEnabled = on;
    }

    private void OnResetClicked()
    {
        GameSettings.ResetToDefault();
        PullFromSettings();
    }

    // ── Settings → UI ────────────────────────────────────────────────────────

    private void PullFromSettings()
    {
        _syncing = true;

        if (moveSpeedSlider != null) moveSpeedSlider.value = GameSettings.MoveSpeed01;
        if (musicToggle != null)     musicToggle.isOn      = GameSettings.MusicEnabled;
        if (dialogueToggle != null)  dialogueToggle.isOn   = GameSettings.DialogueEnabled;

        _syncing = false;

        UpdateSpeedLabel(GameSettings.MoveSpeed01);
    }

    private void UpdateSpeedLabel(float value)
    {
        if (moveSpeedValueText == null) return;
        moveSpeedValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
