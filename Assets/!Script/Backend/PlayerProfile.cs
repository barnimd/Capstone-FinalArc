using System;
using UnityEngine;

/// <summary>Which MC the player picked. Male = BIMA, Female = AYU.</summary>
public enum PlayerGender
{
    Male,
    Female,
}

/// <summary>
/// The player's identity as far as the game is concerned: display name and
/// chosen character.
///
/// Everything MC-shaped reads from here — VN speaker name, VN portraits, and the
/// walking sprite in the maps. Keeping it in one place means a new gender-aware
/// surface only has to ask this class, not re-derive the answer from Firebase or
/// PlayerPrefs on its own.
///
/// Neon is the source of truth; PlayerPrefs is a cache so the first frame after a
/// scene load already has the right character instead of flickering from the
/// default. Static class mirroring LessonLocks.
/// </summary>
public static class PlayerProfile
{
    private const string GenderKey = "SecMind.playerGender";
    private const string ChosenKey = "SecMind.genderChosen";
    private const string NameKey   = "SecMind.playerName";

    /// <summary>Chosen character. Defaults to Male — matches the DB default.</summary>
    public static PlayerGender Gender { get; private set; }

    /// <summary>
    /// False means the player has never seen the CharacterSelect screen. Note this
    /// is NOT the same as "Gender == Male": an account that never picked also reads
    /// Male, and without this flag it would silently be locked to BIMA forever.
    /// </summary>
    public static bool HasChosenGender { get; private set; }

    /// <summary>Username / callsign. Empty when not signed in.</summary>
    public static string DisplayName { get; private set; } = string.Empty;

    static PlayerProfile()
    {
        Gender          = PlayerPrefs.GetInt(GenderKey, 0) == 1 ? PlayerGender.Female : PlayerGender.Male;
        HasChosenGender = PlayerPrefs.GetInt(ChosenKey, 0) == 1;
        DisplayName     = PlayerPrefs.GetString(NameKey, string.Empty);
    }

    /// <summary>Wire string used by the API. Must match the CHECK in schema_gender.sql.</summary>
    public static string ToWire(PlayerGender g) => g == PlayerGender.Female ? "female" : "male";

    /// <summary>Refresh the cache from a /api/user/sync response.</summary>
    public static void ApplyFromServer(UserRecord user)
    {
        if (user == null) return;

        Gender          = user.gender == "female" ? PlayerGender.Female : PlayerGender.Male;
        HasChosenGender = user.gender_chosen;

        if (!string.IsNullOrEmpty(user.display_name))
            DisplayName = user.display_name;

        Save();
    }

    /// <summary>
    /// Record the player's pick. Writes the cache immediately so the next scene is
    /// already correct, then persists to Neon.
    ///
    /// The callback reports whether the server accepted it. A false here means the
    /// choice only lives on this device — worth surfacing, because the pick is
    /// permanent and the player should not silently get a different character on
    /// another browser.
    /// </summary>
    public static void SetGender(PlayerGender gender, Action<bool> callback)
    {
        Gender          = gender;
        HasChosenGender = true;
        Save();

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[PlayerProfile] APIClient.Instance is null — gender saved locally only");
            callback?.Invoke(false);
            return;
        }

        APIClient.Instance.UserSyncWithGender(DisplayName, ToWire(gender), (ok, resp, raw) =>
        {
            if (!ok || resp == null || resp.user == null)
            {
                Debug.LogWarning("[PlayerProfile] Gender sync failed: " + raw);
                callback?.Invoke(false);
                return;
            }

            // Trust the server's answer over our own. If this account had already
            // picked, the server keeps the original and we must not disagree.
            ApplyFromServer(resp.user);
            callback?.Invoke(true);
        });
    }

    /// <summary>
    /// The name to show for the player. Falls back to whatever the dialogue asset
    /// carries ("Kamu", "Raka", ...) when there is no signed-in name, so the Editor
    /// and any not-signed-in path keep reading exactly like they do today.
    /// </summary>
    public static string ResolvePlayerName(string assetFallback)
        => string.IsNullOrEmpty(DisplayName) ? assetFallback : DisplayName;

    /// <summary>Called by the auth flow once a username is known.</summary>
    public static void SetDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        DisplayName = name;
        Save();
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(GenderKey, Gender == PlayerGender.Female ? 1 : 0);
        PlayerPrefs.SetInt(ChosenKey, HasChosenGender ? 1 : 0);
        PlayerPrefs.SetString(NameKey, DisplayName);
        PlayerPrefs.Save();
    }

    /// <summary>Clears the cache on sign-out so the next player does not inherit it.</summary>
    public static void Clear()
    {
        Gender          = PlayerGender.Male;
        HasChosenGender = false;
        DisplayName     = string.Empty;

        PlayerPrefs.DeleteKey(GenderKey);
        PlayerPrefs.DeleteKey(ChosenKey);
        PlayerPrefs.DeleteKey(NameKey);
        PlayerPrefs.Save();
    }
}
