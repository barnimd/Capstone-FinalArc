using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent background music manager. Lives across all scenes (DontDestroyOnLoad).
///
/// Setup (one-time):
///   1. Create an empty GameObject in your FIRST scene (e.g. LoginScene or Dashboard).
///   2. Add this component + an AudioSource component to it.
///   3. Assign the AudioSource in the Inspector.
///   4. Assign your default BGM clip in the Inspector.
///   5. That's it — the GameObject survives every scene load.
///
/// Per-scene override:
///   Place a SceneMusicOverride component in any scene to switch tracks
///   automatically when that scene loads.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Source")]
    [Tooltip("AudioSource on this GameObject. Set Loop = true, Play On Awake = false.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Default Track")]
    [Tooltip("Clip that plays from the very start (menu / lobby music).")]
    [SerializeField] private AudioClip defaultClip;

    [Header("Fade")]
    [Tooltip("Duration (seconds) of the crossfade when switching tracks.")]
    [Range(0f, 3f)] public float fadeDuration = 0.8f;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    private Coroutine _fadeCo;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        // Singleton — destroy duplicate if a second one appears after scene load
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.loop   = true;
        audioSource.volume = masterVolume;

        if (defaultClip != null)
            Play(defaultClip);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Switch immediately to <paramref name="clip"/>. No fade.</summary>
    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return; // already playing

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        audioSource.clip = clip;
        audioSource.volume = masterVolume;
        audioSource.Play();
    }

    /// <summary>Crossfade to <paramref name="clip"/> over <see cref="fadeDuration"/> seconds.</summary>
    public void FadeTo(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossFade(clip));
    }

    /// <summary>Fade out and stop.</summary>
    public void FadeOut()
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutCoroutine());
    }

    public void Stop()
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        audioSource.volume = masterVolume;
    }

    public void Pause()  => audioSource.Pause();
    public void Resume() => audioSource.UnPause();

    // ── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator CrossFade(AudioClip nextClip)
    {
        float half = fadeDuration * 0.5f;

        // Fade out current
        float start = audioSource.volume;
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, 0f, t / half);
            yield return null;
        }
        audioSource.volume = 0f;

        // Swap track
        audioSource.clip = nextClip;
        audioSource.Play();

        // Fade in new
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, masterVolume, t / half);
            yield return null;
        }
        audioSource.volume = masterVolume;
        _fadeCo = null;
    }

    private IEnumerator FadeOutCoroutine()
    {
        float start = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = masterVolume;
        _fadeCo = null;
    }
}
