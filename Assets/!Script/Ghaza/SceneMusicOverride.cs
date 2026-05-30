using UnityEngine;

/// <summary>
/// Drop this on any GameObject in a scene to automatically switch
/// the global MusicManager to a specific BGM when the scene loads.
/// Leave clip empty to keep whatever track is already playing.
/// </summary>
public class SceneMusicOverride : MonoBehaviour
{
    [Tooltip("BGM clip to play when this scene loads. Leave empty = keep current music.")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Use crossfade when switching. Uncheck to switch instantly.")]
    [SerializeField] private bool fade = true;

    void Start()
    {
        if (clip == null) return;

        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusicOverride] MusicManager not found. Make sure it exists in the first scene.");
            return;
        }

        if (fade)
            MusicManager.Instance.FadeTo(clip);
        else
            MusicManager.Instance.Play(clip);
    }
}
