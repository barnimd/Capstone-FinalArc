using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit version of the leaderboard page.
/// Attach to a GameObject that also has a UIDocument component referencing LeaderboardView.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LeaderboardUIToolkit : MonoBehaviour
{
    [Header("Config")]
    public int  limit         = 10;
    public bool fetchOnEnable = true;

    private UIDocument _document;

    // Stat strip
    private Label _yourRankValue, _yourRankSubtitle;
    private Label _yourBestValue, _yourBestSubtitle;
    private Label _topScoreValue, _topScoreSubtitle;

    // Filter tabs
    private Button _tabGlobal, _tabFriends, _tabWeek;

    // Table
    private ScrollView _rowList;

    void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        VisualElement root = _document.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[LeaderboardUIToolkit] rootVisualElement is null — check UIDocument.sourceAsset");
            return;
        }

        _yourRankValue    = root.Q<Label>("your-rank-value");
        _yourRankSubtitle = root.Q<Label>("your-rank-subtitle");
        _yourBestValue    = root.Q<Label>("your-best-value");
        _yourBestSubtitle = root.Q<Label>("your-best-subtitle");
        _topScoreValue    = root.Q<Label>("top-score-value");
        _topScoreSubtitle = root.Q<Label>("top-score-subtitle");
        _rowList          = root.Q<ScrollView>("row-list");

        _tabGlobal  = root.Q<Button>("tab-global");
        _tabFriends = root.Q<Button>("tab-friends");
        _tabWeek    = root.Q<Button>("tab-week");

        if (_tabGlobal  != null) _tabGlobal.clicked  += () => SelectTab(_tabGlobal);
        if (_tabFriends != null) _tabFriends.clicked += () => { SelectTab(_tabFriends); Debug.Log("[LeaderboardUIToolkit] Friends tab — not implemented"); };
        if (_tabWeek    != null) _tabWeek.clicked    += () => { SelectTab(_tabWeek);    Debug.Log("[LeaderboardUIToolkit] This Week tab — not implemented"); };

        if (fetchOnEnable) Refresh();
    }

    public void Refresh()
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUIToolkit] LeaderboardManager.Instance missing");
            return;
        }
        LeaderboardManager.Instance.FetchGlobal(limit, OnLoaded);
    }

    private void SelectTab(Button selected)
    {
        if (_tabGlobal  != null) _tabGlobal.RemoveFromClassList("active");
        if (_tabFriends != null) _tabFriends.RemoveFromClassList("active");
        if (_tabWeek    != null) _tabWeek.RemoveFromClassList("active");
        if (selected != null) selected.AddToClassList("active");
    }

    private void OnLoaded(bool ok, GlobalLeaderboardEntryDTO[] entries)
    {
        if (_rowList == null) return;
        _rowList.Clear();

        if (!ok || entries == null || entries.Length == 0)
        {
            VisualElement empty = new VisualElement();
            empty.AddToClassList("empty-state");
            Label emptyText = new Label("No scores yet — be the first!");
            emptyText.AddToClassList("empty-text");
            empty.Add(emptyText);
            _rowList.Add(empty);
            return;
        }

        string myUid = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : null;

        // Top score card (#1)
        if (_topScoreValue    != null) _topScoreValue.text    = entries[0].score.ToString();
        if (_topScoreSubtitle != null) _topScoreSubtitle.text = $"by {entries[0].displayName}";

        // Your rank + best
        GlobalLeaderboardEntryDTO mine = !string.IsNullOrEmpty(myUid)
            ? entries.FirstOrDefault(e => e.userId == myUid)
            : null;

        if (mine != null)
        {
            if (_yourRankValue    != null) _yourRankValue.text    = "#" + mine.rank;
            if (_yourRankSubtitle != null) _yourRankSubtitle.text = $"out of {entries.Length} players";
            if (_yourBestValue    != null) _yourBestValue.text    = mine.score.ToString();
            if (_yourBestSubtitle != null) _yourBestSubtitle.text = mine.bestStageName;
        }
        else
        {
            if (_yourRankValue    != null) _yourRankValue.text    = "—";
            if (_yourRankSubtitle != null) _yourRankSubtitle.text = "not on leaderboard yet";
            if (_yourBestValue    != null) _yourBestValue.text    = "—";
            if (_yourBestSubtitle != null) _yourBestSubtitle.text = "no completion yet";
        }

        // Rows
        foreach (GlobalLeaderboardEntryDTO e in entries)
            _rowList.Add(CreateRow(e, isYou: e.userId == myUid));
    }

    private VisualElement CreateRow(GlobalLeaderboardEntryDTO e, bool isYou)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("row");
        if (isYou) row.AddToClassList("you");

        // Rank badge
        VisualElement rankBadge = new VisualElement();
        rankBadge.AddToClassList("rank-badge");
        if (e.rank == 1) rankBadge.AddToClassList("first");
        Label rankText = new Label(e.rank.ToString());
        rankText.AddToClassList("rank-badge-text");
        rankBadge.Add(rankText);
        row.Add(rankBadge);

        // Player cell (avatar + name + optional "YOU")
        VisualElement playerCell = new VisualElement();
        playerCell.AddToClassList("player-cell");

        VisualElement avatar = new VisualElement();
        avatar.AddToClassList("avatar");
        string initial = string.IsNullOrEmpty(e.displayName) ? "?" : e.displayName.Substring(0, 1).ToUpper();
        Label avatarText = new Label(initial);
        avatarText.AddToClassList("avatar-text");
        avatar.Add(avatarText);
        playerCell.Add(avatar);

        Label nameLabel = new Label(e.displayName);
        nameLabel.AddToClassList("player-name");
        playerCell.Add(nameLabel);

        if (isYou)
        {
            Label youBadge = new Label("YOU");
            youBadge.AddToClassList("you-badge");
            playerCell.Add(youBadge);
        }
        row.Add(playerCell);

        // Topic
        Label topic = new Label(e.bestStageName);
        topic.AddToClassList("topic-text");
        row.Add(topic);

        // Score
        Label score = new Label(e.score.ToString());
        score.AddToClassList("score-text");
        row.Add(score);

        return row;
    }
}
