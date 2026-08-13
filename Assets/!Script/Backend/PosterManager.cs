using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Server-owned dashboard poster slideshow.
///
/// An admin uploads, reorders, and deletes posters in Menu Admin &gt; Kelola Poster;
/// every player reads the result back from GET /api/posters. Changing the slideshow
/// no longer needs a Unity rebuild.
///
/// Image bytes come from /api/poster/image, not from Cloudflare's public r2.dev URL.
/// Indonesian ISPs DNS-hijack the whole r2.dev domain to the Internet Positif block
/// page, so a direct R2 URL fails for exactly the players this game is built for.
/// Routing through Vercel also means same-origin requests, so there is no CORS to get
/// wrong in a WebGL build.
///
/// MainMenuController.dashboardSlides stays as the offline fallback: if the fetch
/// fails or the server has no posters, the dashboard shows the sprites from the
/// Inspector rather than going blank.
/// </summary>
public static class PosterManager
{
    private static readonly List<PosterInfo> Posters = new List<PosterInfo>();
    private static readonly Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();

    private static bool _loading;

    /// <summary>True once a fetch finished with at least one usable poster.</summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>Number of posters that actually have a downloaded texture.</summary>
    public static int Count => Posters.Count;

    /// <summary>Poster metadata at slide position <paramref name="index"/>, or null.</summary>
    public static PosterInfo GetPoster(int index)
        => index >= 0 && index < Posters.Count ? Posters[index] : null;

    /// <summary>Texture at slide position <paramref name="index"/>, or null.</summary>
    public static Texture2D GetTexture(int index)
    {
        PosterInfo p = GetPoster(index);
        if (p == null) return null;
        return Textures.TryGetValue(p.id, out Texture2D tex) ? tex : null;
    }

    /// <summary>Poster ids in current display order — the payload /api/admin/poster/reorder expects.</summary>
    public static int[] GetOrderedIds()
    {
        int[] ids = new int[Posters.Count];
        for (int i = 0; i < Posters.Count; i++) ids[i] = Posters[i].id;
        return ids;
    }

    /// <summary>
    /// Fetch the list, then download every image. The callback reports whether there
    /// is anything worth showing — false means the caller should use its fallback.
    /// Concurrent calls are ignored; the second caller gets false rather than a
    /// duplicate download.
    /// </summary>
    public static void Load(Action<bool> callback)
    {
        if (_loading)
        {
            callback?.Invoke(false);
            return;
        }

        APIClient api = APIClient.Instance;
        if (api == null)
        {
            Debug.LogWarning("[PosterManager] APIClient.Instance is null — falling back to Inspector slides");
            callback?.Invoke(false);
            return;
        }

        _loading = true;
        api.GetPosters((ok, resp, raw) =>
        {
            if (!ok || resp == null || !resp.success || resp.posters == null || resp.posters.Length == 0)
            {
                // Not an error worth shouting about: an empty slideshow is a valid
                // state before the admin has uploaded anything.
                if (!ok) Debug.LogWarning("[PosterManager] Poster list fetch failed: " + raw);
                _loading = false;
                IsLoaded = false;
                callback?.Invoke(false);
                return;
            }

            api.StartCoroutine(DownloadAll(resp.posters, callback));
        });
    }

    private static IEnumerator DownloadAll(PosterInfo[] incoming, Action<bool> callback)
    {
        ClearTextures();
        Posters.Clear();

        foreach (PosterInfo p in incoming)
        {
            if (p == null || string.IsNullOrEmpty(p.imageUrl)) continue;

            // The server sends a relative path so it always matches whatever origin
            // the build is served from. UnityWebRequest needs it absolute.
            string url = p.imageUrl.StartsWith("http")
                ? p.imageUrl
                : SecMindAPI.BaseUrl + p.imageUrl;

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    // Skip this one and keep the rest — one dead image should not
                    // take the whole slideshow down.
                    Debug.LogWarning($"[PosterManager] Image {p.id} failed: HTTP {req.responseCode} {req.error}");
                    continue;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null) continue;

                Textures[p.id] = tex;
                Posters.Add(p);
            }
        }

        _loading = false;
        IsLoaded = Posters.Count > 0;
        callback?.Invoke(IsLoaded);
    }

    /// <summary>
    /// Drop the cache so the next Load() hits the server. Call after an admin
    /// upload, delete, or reorder — otherwise the dashboard keeps showing the old
    /// set until the game restarts.
    /// </summary>
    public static void Invalidate()
    {
        ClearTextures();
        Posters.Clear();
        IsLoaded = false;
    }

    private static void ClearTextures()
    {
        // Textures from UnityWebRequestTexture are not garbage collected on their
        // own. Without this, every reload leaks the previous set.
        foreach (Texture2D tex in Textures.Values)
        {
            if (tex != null) UnityEngine.Object.Destroy(tex);
        }
        Textures.Clear();
    }
}
