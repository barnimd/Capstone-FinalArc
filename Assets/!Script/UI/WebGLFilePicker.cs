using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Image picker for the admin poster uploader.
///
/// In a WebGL build this opens the browser's file dialog through
/// Assets/Plugins/WebGL/FilePicker.jslib, which also resizes and re-encodes the
/// image on a canvas before Unity ever sees it. In the Editor it falls back to
/// EditorUtility.OpenFilePanel and does the same work in C#, so the admin UI can be
/// tested without producing a build every time.
///
/// Either way the result is the same: a JPEG, at most <see cref="MaxWidth"/> wide,
/// as raw base64 with no data-URL prefix.
///
/// Compression is not optional. Vercel rejects any request body over 4.5 MB, and
/// base64 inflates by 33%, so an untouched phone photo would never reach the API.
/// </summary>
public class WebGLFilePicker : MonoBehaviour
{
    /// <summary>Posters display at roughly 800px, so 1600 leaves headroom without waste.</summary>
    public const int MaxWidth = 1600;

    /// <summary>JPEG quality, 0-100. 85 is the usual point where artifacts stop being visible.</summary>
    public const int Quality = 85;

    private const string GameObjectName = "SecMindFilePicker";

    /// <summary>ok, mimeType, base64 (no prefix), error message when ok is false.</summary>
    public delegate void PickCallback(bool ok, string mimeType, string base64, string error);

    private PickCallback _callback;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FilePicker_Open(string goName, int maxWidth, int quality);
#endif

    private static WebGLFilePicker _instance;

    private static WebGLFilePicker Instance
    {
        get
        {
            if (_instance == null)
            {
                // Created on demand rather than wired into a scene: the jslib needs a
                // GameObject name to SendMessage back to, and a self-made one cannot
                // be renamed or deleted by accident in the Inspector.
                GameObject go = new GameObject(GameObjectName);
                _instance = go.AddComponent<WebGLFilePicker>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// Open the picker. The callback fires once — on success, on cancel, or on error.
    /// A second call while one dialog is already open is rejected rather than queued.
    /// </summary>
    public static void Open(PickCallback callback)
    {
        if (callback == null) return;
        Instance.Begin(callback);
    }

    private void Begin(PickCallback callback)
    {
        if (_callback != null)
        {
            callback(false, null, null, "Dialog file sudah terbuka");
            return;
        }
        _callback = callback;

#if UNITY_WEBGL && !UNITY_EDITOR
        FilePicker_Open(gameObject.name, MaxWidth, Quality);
#elif UNITY_EDITOR
        PickInEditor();
#else
        Finish(false, null, null, "File picker hanya tersedia di WebGL");
#endif
    }

    // ── Callbacks from FilePicker.jslib (names must match SendMessage) ───────────

    /// <summary>Receives a data URL: "data:image/jpeg;base64,...".</summary>
    public void OnFilePicked(string dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl))
        {
            Finish(false, null, null, "Data gambar kosong");
            return;
        }

        int comma = dataUrl.IndexOf(',');
        if (comma < 0)
        {
            Finish(false, null, null, "Format data URL tidak dikenal");
            return;
        }

        // The jslib always re-encodes to JPEG, whatever the source format was.
        Finish(true, "image/jpeg", dataUrl.Substring(comma + 1), null);
    }

    public void OnFilePickCancelled(string _)
    {
        Finish(false, null, null, null);
    }

    public void OnFilePickFailed(string error)
    {
        Finish(false, null, null, string.IsNullOrEmpty(error) ? "Gagal membaca file" : error);
    }

    private void Finish(bool ok, string mimeType, string base64, string error)
    {
        PickCallback cb = _callback;
        _callback = null;
        cb?.Invoke(ok, mimeType, base64, error);
    }

    // ── Editor fallback ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void PickInEditor()
    {
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "Pilih gambar poster", "", "png,jpg,jpeg,webp");

        if (string.IsNullOrEmpty(path))
        {
            Finish(false, null, null, null);   // cancelled
            return;
        }

        byte[] raw;
        try
        {
            raw = System.IO.File.ReadAllBytes(path);
        }
        catch (Exception e)
        {
            Finish(false, null, null, "Gagal baca file: " + e.Message);
            return;
        }

        Texture2D source = new Texture2D(2, 2);
        if (!source.LoadImage(raw))
        {
            Destroy(source);
            Finish(false, null, null, "File bukan gambar yang valid");
            return;
        }

        Texture2D scaled = source;
        if (source.width > MaxWidth)
        {
            int h = Mathf.Max(1, Mathf.RoundToInt(source.height * (MaxWidth / (float)source.width)));
            scaled = Resize(source, MaxWidth, h);
        }

        byte[] jpg = scaled.EncodeToJPG(Quality);

        if (scaled != source) Destroy(scaled);
        Destroy(source);

        Finish(true, "image/jpeg", Convert.ToBase64String(jpg), null);
    }

    private static Texture2D Resize(Texture2D src, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        Texture2D dst = new Texture2D(width, height, TextureFormat.RGB24, false);
        dst.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        dst.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }
#endif
}
