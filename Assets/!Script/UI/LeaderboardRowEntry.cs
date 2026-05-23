using TMPro;
using UnityEngine;

/// <summary>
/// Component attached to a leaderboard row template (e.g. Row1 in CanvasLeaderboard).
/// LeaderboardUI instantiates clones of this prefab and calls SetData per entry.
///
/// 4 text slots — assign whichever ones your row layout actually uses:
///   • rankText   — "#1", "#2", ...
///   • nameText   — display name
///   • topicText  — stage name (only used in Global mode; null/blank otherwise)
///   • scoreText  — numeric score
/// </summary>
public class LeaderboardRowEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text topicText;
    [SerializeField] private TMP_Text scoreText;

    public void SetData(int rank, string displayName, int score, string topic = null)
    {
        if (rankText  != null) rankText.text  = "#" + rank;
        if (nameText  != null) nameText.text  = displayName;
        if (topicText != null) topicText.text = string.IsNullOrEmpty(topic) ? "—" : topic;
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public void Clear()
    {
        if (rankText  != null) rankText.text  = "";
        if (nameText  != null) nameText.text  = "";
        if (topicText != null) topicText.text = "";
        if (scoreText != null) scoreText.text = "";
    }
}
