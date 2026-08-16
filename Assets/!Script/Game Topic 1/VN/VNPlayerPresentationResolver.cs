using System.Text;
using UnityEngine;

/// <summary>
/// Resolves player-specific VN presentation without changing dialogue flow.
/// PlayerProfile remains the single source of truth for username and gender.
/// </summary>
public static class VNPlayerPresentationResolver
{
    public const string PlayerNameToken = "{PLAYER_NAME}";
    public const string DefaultPlayerName = "Kamu";

    public static PlayerGender CurrentGender => PlayerProfile.Gender;

    public static string ResolveDisplayName(string assetFallback = DefaultPlayerName)
    {
        string raw = PlayerProfile.ResolvePlayerName(assetFallback);
        return SanitizeDisplayName(raw);
    }

    public static string SanitizeDisplayName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            raw = DefaultPlayerName;

        var clean = new StringBuilder(raw.Length);
        foreach (char character in raw.Trim())
        {
            if (char.IsControl(character))
                continue;

            // Do not allow a profile name to become TextMesh Pro rich-text markup.
            if (character == '<') clean.Append('‹');
            else if (character == '>') clean.Append('›');
            else clean.Append(character);
        }

        string result = clean.ToString().Trim();
        return string.IsNullOrEmpty(result) ? DefaultPlayerName : result;
    }

    public static string ResolveLineText(string template, string resolvedPlayerName)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        string safeName = string.IsNullOrWhiteSpace(resolvedPlayerName)
            ? DefaultPlayerName
            : resolvedPlayerName;
        return template.Replace(PlayerNameToken, safeName);
    }

    public static bool ContainsDynamicPlayerName(string template)
    {
        return !string.IsNullOrEmpty(template) && template.Contains(PlayerNameToken);
    }

    public static AudioClip ResolveVoiceClip(VNLine line, PlayerGender gender)
    {
        if (line == null || ContainsDynamicPlayerName(line.text))
            return null;

        if (line.speaker != VNSpeaker.Player)
            return line.voiceClip;

        return gender == PlayerGender.Female
            ? line.femalePlayerVoiceClip
            : line.voiceClip;
    }
}
