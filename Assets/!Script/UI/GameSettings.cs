using UnityEngine;

/// <summary>
/// Penyimpanan setting global game. Static — tidak perlu GameObject.
/// Nilai disimpan di PlayerPrefs, jadi ikut kebawa antar scene & antar sesi.
///
/// Ada 3 setting:
///   - MoveSpeed01      : slider 0..1. 0 = lambat banget, 1 = paling cepat.
///   - MusicEnabled     : checkbox background music.
///   - DialogueEnabled  : checkbox suara dialog (voice + klik ketik).
///
/// Consumer tinggal baca nilainya, atau subscribe <see cref="OnChanged"/>
/// kalau perlu react langsung saat player ubah setting.
/// </summary>
public static class GameSettings
{
    // ── PlayerPrefs keys ─────────────────────────────────────────────────────
    private const string KEY_MOVE_SPEED = "settings_move_speed";
    private const string KEY_MUSIC      = "settings_music_enabled";
    private const string KEY_DIALOGUE   = "settings_dialogue_enabled";

    // ── Default ──────────────────────────────────────────────────────────────
    public const float DEFAULT_MOVE_SPEED = 0.5f;

    /// <summary>Pengali kecepatan saat slider di 0 (paling lambat).</summary>
    public const float MIN_SPEED_MULTIPLIER = 0.4f;
    /// <summary>Pengali kecepatan saat slider di 1 (paling cepat).</summary>
    public const float MAX_SPEED_MULTIPLIER = 1.6f;

    /// <summary>Dipanggil setiap kali ada setting yang berubah.</summary>
    public static event System.Action OnChanged;

    private static bool _loaded;

    private static float _moveSpeed01;
    private static bool  _musicEnabled;
    private static bool  _dialogueEnabled;

    // ── Properties ───────────────────────────────────────────────────────────

    /// <summary>Nilai mentah slider, 0..1.</summary>
    public static float MoveSpeed01
    {
        get { EnsureLoaded(); return _moveSpeed01; }
        set
        {
            EnsureLoaded();
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, _moveSpeed01)) return;

            _moveSpeed01 = v;
            PlayerPrefs.SetFloat(KEY_MOVE_SPEED, v);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    /// <summary>
    /// Pengali yang dipakai script movement. Slider 0.5 = 1.0x (kecepatan asli).
    /// </summary>
    public static float MoveSpeedMultiplier =>
        Mathf.Lerp(MIN_SPEED_MULTIPLIER, MAX_SPEED_MULTIPLIER, MoveSpeed01);

    public static bool MusicEnabled
    {
        get { EnsureLoaded(); return _musicEnabled; }
        set
        {
            EnsureLoaded();
            if (value == _musicEnabled) return;

            _musicEnabled = value;
            PlayerPrefs.SetInt(KEY_MUSIC, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool DialogueEnabled
    {
        get { EnsureLoaded(); return _dialogueEnabled; }
        set
        {
            EnsureLoaded();
            if (value == _dialogueEnabled) return;

            _dialogueEnabled = value;
            PlayerPrefs.SetInt(KEY_DIALOGUE, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>Balikin semua setting ke default.</summary>
    public static void ResetToDefault()
    {
        EnsureLoaded();

        _moveSpeed01     = DEFAULT_MOVE_SPEED;
        _musicEnabled    = true;
        _dialogueEnabled = true;

        PlayerPrefs.SetFloat(KEY_MOVE_SPEED, _moveSpeed01);
        PlayerPrefs.SetInt(KEY_MUSIC, 1);
        PlayerPrefs.SetInt(KEY_DIALOGUE, 1);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _moveSpeed01     = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MOVE_SPEED, DEFAULT_MOVE_SPEED));
        _musicEnabled    = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        _dialogueEnabled = PlayerPrefs.GetInt(KEY_DIALOGUE, 1) == 1;
    }
}
