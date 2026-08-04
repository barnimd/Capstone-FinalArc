using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Versi UI Toolkit dari panel Settings, buat halaman "page-settings" di MenuView.uxml.
///
/// Nyambungin elemen UXML ke <see cref="GameSettings"/> — sumber datanya sama persis
/// dengan panel Settings in-game (<see cref="SettingsPanelUI"/>), cuma beda tampilan.
/// Jadi ubah di menu = kebawa ke semua topic, dan sebaliknya.
///
/// Setup: taruh di GameObject yang punya UIDocument-nya MenuView (yaitu MenuShell).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SettingsPageUIToolkit : MonoBehaviour
{
    private UIDocument _document;

    private Slider _speed;
    private Label  _speedValue;
    private Toggle _music;
    private Toggle _dialogue;
    private Button _reset;

    // Blokir callback selama kita nulis nilai awal ke UI
    private bool _syncing;

    private void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        VisualElement root = _document.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[SettingsPageUIToolkit] rootVisualElement null — cek UIDocument.sourceAsset");
            return;
        }

        _speed      = root.Q<Slider>("settings-move-speed");
        _speedValue = root.Q<Label>("settings-speed-value");
        _music      = root.Q<Toggle>("settings-music");
        _dialogue   = root.Q<Toggle>("settings-dialogue");
        _reset      = root.Q<Button>("settings-reset");

        if (_speed != null)    _speed.RegisterValueChangedCallback(OnSpeedChanged);
        if (_music != null)    _music.RegisterValueChangedCallback(OnMusicChanged);
        if (_dialogue != null) _dialogue.RegisterValueChangedCallback(OnDialogueChanged);
        if (_reset != null)    _reset.clicked += OnResetClicked;

        GameSettings.OnChanged += PullFromSettings;
        PullFromSettings();
    }

    private void OnDisable()
    {
        GameSettings.OnChanged -= PullFromSettings;

        if (_speed != null)    _speed.UnregisterValueChangedCallback(OnSpeedChanged);
        if (_music != null)    _music.UnregisterValueChangedCallback(OnMusicChanged);
        if (_dialogue != null) _dialogue.UnregisterValueChangedCallback(OnDialogueChanged);
        if (_reset != null)    _reset.clicked -= OnResetClicked;
    }

    // ── UI → Settings ────────────────────────────────────────────────────────

    private void OnSpeedChanged(ChangeEvent<float> evt)
    {
        if (_syncing) return;
        GameSettings.MoveSpeed01 = evt.newValue;
        UpdateSpeedLabel(evt.newValue);
    }

    private void OnMusicChanged(ChangeEvent<bool> evt)
    {
        if (_syncing) return;
        GameSettings.MusicEnabled = evt.newValue;
    }

    private void OnDialogueChanged(ChangeEvent<bool> evt)
    {
        if (_syncing) return;
        GameSettings.DialogueEnabled = evt.newValue;
    }

    private void OnResetClicked() => GameSettings.ResetToDefault();

    // ── Settings → UI ────────────────────────────────────────────────────────

    private void PullFromSettings()
    {
        _syncing = true;

        if (_speed != null)    _speed.SetValueWithoutNotify(GameSettings.MoveSpeed01);
        if (_music != null)    _music.SetValueWithoutNotify(GameSettings.MusicEnabled);
        if (_dialogue != null) _dialogue.SetValueWithoutNotify(GameSettings.DialogueEnabled);

        _syncing = false;

        UpdateSpeedLabel(GameSettings.MoveSpeed01);
    }

    private void UpdateSpeedLabel(float value)
    {
        if (_speedValue == null) return;
        _speedValue.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
