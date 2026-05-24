using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Replaces SidebarNavigation. Drives the entire main menu shell via UI Toolkit.
///
/// Page swap strategy during migration:
///   - Migrated pages (UI Toolkit): show their UXML VisualElement (#page-{name})
///   - Unmigrated pages (still uGUI): activate their old Canvas GameObject reference
///
/// Once all pages are migrated to UI Toolkit, the old Canvas refs can be removed.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Old uGUI Canvases (fallback for not-yet-migrated pages)")]
    public GameObject canvasDashboard;
    public GameObject canvasClass;
    public GameObject canvasProfile;
    // canvasLeaderboard intentionally omitted — leaderboard is fully UI Toolkit now
    public GameObject canvasSettings;
    public GameObject canvasGetHelp;

    [Header("Leaderboard config")]
    public int leaderboardLimit = 10;

    [Header("Page migration flags (toggle when each page is ported)")]
    public bool dashboardMigrated   = false;
    public bool classMigrated       = true;
    public bool profileMigrated     = false;
    public bool leaderboardMigrated = true;
    public bool settingsMigrated    = false;
    public bool helpMigrated        = false;

    private UIDocument _document;
    private VisualElement _root;

    // Sidebar buttons
    private Button _btnDashboard, _btnClass, _btnProfile, _btnLeaderboard, _btnSettings, _btnHelp;

    // Page VisualElements (UXML)
    private VisualElement _pageDashboard, _pageClass, _pageProfile, _pageLeaderboard, _pageSettings, _pageHelp;

    // Leaderboard refs
    private Label _yourRankValue, _yourRankSubtitle;
    private Label _yourBestValue, _yourBestSubtitle;
    private Label _topScoreValue, _topScoreSubtitle;
    private ScrollView _rowList;
    private Button _tabGlobal, _tabFriends, _tabWeek;

    // Navbar
    private Label _navbarGreeting;
    private Label _navbarProfileText;

    // Class page
    private VisualElement _unlockedGrid;
    private VisualElement _lockedGrid;
    private bool          _classCardsBuilt;

    private string _currentPage = "dashboard";

    void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        _root     = _document.rootVisualElement;
        if (_root == null)
        {
            Debug.LogWarning("[MainMenuController] rootVisualElement is null");
            return;
        }

        // Sidebar buttons
        _btnDashboard   = _root.Q<Button>("btn-dashboard");
        _btnClass       = _root.Q<Button>("btn-class");
        _btnProfile     = _root.Q<Button>("btn-profile");
        _btnLeaderboard = _root.Q<Button>("btn-leaderboard");
        _btnSettings    = _root.Q<Button>("btn-settings");
        _btnHelp        = _root.Q<Button>("btn-help");

        // Pages
        _pageDashboard   = _root.Q<VisualElement>("page-dashboard");
        _pageClass       = _root.Q<VisualElement>("page-class");
        _pageProfile     = _root.Q<VisualElement>("page-profile");
        _pageLeaderboard = _root.Q<VisualElement>("page-leaderboard");
        _pageSettings    = _root.Q<VisualElement>("page-settings");
        _pageHelp        = _root.Q<VisualElement>("page-help");

        // Leaderboard sub-refs
        _yourRankValue    = _root.Q<Label>("your-rank-value");
        _yourRankSubtitle = _root.Q<Label>("your-rank-subtitle");
        _yourBestValue    = _root.Q<Label>("your-best-value");
        _yourBestSubtitle = _root.Q<Label>("your-best-subtitle");
        _topScoreValue    = _root.Q<Label>("top-score-value");
        _topScoreSubtitle = _root.Q<Label>("top-score-subtitle");
        _rowList          = _root.Q<ScrollView>("row-list");

        _tabGlobal  = _root.Q<Button>("tab-global");
        _tabFriends = _root.Q<Button>("tab-friends");
        _tabWeek    = _root.Q<Button>("tab-week");

        // Navbar
        _navbarGreeting    = _root.Q<Label>("navbar-greeting");
        _navbarProfileText = _root.Q<Label>("navbar-profile-text");

        // Class page grids
        _unlockedGrid = _root.Q<VisualElement>("unlocked-grid");
        _lockedGrid   = _root.Q<VisualElement>("locked-grid");

        // Wire button clicks
        if (_btnDashboard   != null) _btnDashboard.clicked   += () => ShowPage("dashboard");
        if (_btnClass       != null) _btnClass.clicked       += () => ShowPage("class");
        if (_btnProfile     != null) _btnProfile.clicked     += () => ShowPage("profile");
        if (_btnLeaderboard != null) _btnLeaderboard.clicked += () => ShowPage("leaderboard");
        if (_btnSettings    != null) _btnSettings.clicked    += () => ShowPage("settings");
        if (_btnHelp        != null) _btnHelp.clicked        += () => ShowPage("help");

        if (_tabGlobal  != null) _tabGlobal.clicked  += () => SelectFilterTab(_tabGlobal);
        if (_tabFriends != null) _tabFriends.clicked += () => { SelectFilterTab(_tabFriends); Debug.Log("[MainMenuController] Friends tab — not implemented"); };
        if (_tabWeek    != null) _tabWeek.clicked    += () => { SelectFilterTab(_tabWeek);    Debug.Log("[MainMenuController] This Week tab — not implemented"); };

        UpdateNavbarGreeting();

        // Default to dashboard
        ShowPage(_currentPage);
    }

    private void UpdateNavbarGreeting()
    {
        if (_navbarGreeting == null) return;
        string raw = FirebaseManager.Instance != null && !string.IsNullOrEmpty(FirebaseManager.Instance.Username)
            ? FirebaseManager.Instance.Username
            : "Player";
        string display = CapitalizeFirst(raw);
        _navbarGreeting.text = $"Hi, {display}";

        if (_navbarProfileText != null)
            _navbarProfileText.text = string.IsNullOrEmpty(display) ? "?" : display.Substring(0, 1).ToUpper();
    }

    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    public void ShowPage(string pageName)
    {
        _currentPage = pageName;

        // 1. Update sidebar button active state
        SetActiveButton(pageName);

        // 2. Hide all UXML pages
        HideElement(_pageDashboard);
        HideElement(_pageClass);
        HideElement(_pageProfile);
        HideElement(_pageLeaderboard);
        HideElement(_pageSettings);
        HideElement(_pageHelp);

        // 3. Hide all old uGUI canvases
        if (canvasDashboard != null) canvasDashboard.SetActive(false);
        if (canvasClass     != null) canvasClass.SetActive(false);
        if (canvasProfile   != null) canvasProfile.SetActive(false);
        if (canvasSettings  != null) canvasSettings.SetActive(false);
        if (canvasGetHelp   != null) canvasGetHelp.SetActive(false);

        // 4. Show the target page (UXML if migrated, else activate old canvas)
        switch (pageName)
        {
            case "dashboard":
                if (dashboardMigrated) ShowElement(_pageDashboard);
                else if (canvasDashboard != null) canvasDashboard.SetActive(true);
                else ShowElement(_pageDashboard); // placeholder
                break;
            case "class":
                if (classMigrated)
                {
                    ShowElement(_pageClass);
                    BuildClassCardsIfNeeded();
                }
                else if (canvasClass != null) canvasClass.SetActive(true);
                else ShowElement(_pageClass);
                break;
            case "profile":
                if (profileMigrated) ShowElement(_pageProfile);
                else if (canvasProfile != null) canvasProfile.SetActive(true);
                else ShowElement(_pageProfile);
                break;
            case "leaderboard":
                ShowElement(_pageLeaderboard);
                RefreshLeaderboard();
                break;
            case "settings":
                if (settingsMigrated) ShowElement(_pageSettings);
                else if (canvasSettings != null) canvasSettings.SetActive(true);
                else ShowElement(_pageSettings);
                break;
            case "help":
                if (helpMigrated) ShowElement(_pageHelp);
                else if (canvasGetHelp != null) canvasGetHelp.SetActive(true);
                else ShowElement(_pageHelp);
                break;
        }
    }

    private void SetActiveButton(string pageName)
    {
        SetButtonActive(_btnDashboard,   pageName == "dashboard");
        SetButtonActive(_btnClass,       pageName == "class");
        SetButtonActive(_btnProfile,     pageName == "profile");
        SetButtonActive(_btnLeaderboard, pageName == "leaderboard");
        SetButtonActive(_btnSettings,    pageName == "settings");
        SetButtonActive(_btnHelp,        pageName == "help");
    }

    private void SetButtonActive(Button btn, bool isActive)
    {
        if (btn == null) return;
        if (isActive) btn.AddToClassList("active");
        else          btn.RemoveFromClassList("active");
    }

    private void ShowElement(VisualElement el) { if (el != null) el.RemoveFromClassList("hidden"); }
    private void HideElement(VisualElement el) { if (el != null) el.AddToClassList("hidden"); }

    // ── Leaderboard logic ───────────────────────────────────────────────────

    private void RefreshLeaderboard()
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[MainMenuController] LeaderboardManager missing");
            return;
        }
        LeaderboardManager.Instance.FetchGlobal(leaderboardLimit, OnLeaderboardLoaded);
    }

    private void SelectFilterTab(Button selected)
    {
        if (_tabGlobal  != null) _tabGlobal.RemoveFromClassList("active");
        if (_tabFriends != null) _tabFriends.RemoveFromClassList("active");
        if (_tabWeek    != null) _tabWeek.RemoveFromClassList("active");
        if (selected != null) selected.AddToClassList("active");
    }

    private void OnLeaderboardLoaded(bool ok, GlobalLeaderboardEntryDTO[] entries)
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

        if (_topScoreValue    != null) _topScoreValue.text    = entries[0].score.ToString();
        if (_topScoreSubtitle != null) _topScoreSubtitle.text = $"by {entries[0].displayName}";

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

        foreach (GlobalLeaderboardEntryDTO e in entries)
            _rowList.Add(BuildRow(e, isYou: e.userId == myUid));
    }

    // ── Class page ──────────────────────────────────────────────────────────

    private void BuildClassCardsIfNeeded()
    {
        if (_classCardsBuilt) return;
        if (_unlockedGrid == null || _lockedGrid == null)
        {
            Debug.LogWarning("[MainMenuController] unlocked-grid / locked-grid not found in UXML");
            return;
        }

        // Load all LessonData from Resources/Lessons folder (existing ScriptableObjects)
        LessonData[] lessons = Resources.LoadAll<LessonData>("Lessons");
        if (lessons == null || lessons.Length == 0)
        {
            Debug.LogWarning("[MainMenuController] No LessonData found in Resources/Lessons");
            return;
        }

        // Sort by name for stable order (Lesson_01..Lesson_06)
        System.Array.Sort(lessons, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        int unlockedIdx = 1, lockedIdx = 1;
        foreach (LessonData data in lessons)
        {
            if (data == null) continue;
            int displayIdx = data.isUnlocked ? unlockedIdx++ : lockedIdx++;
            VisualElement card = BuildLessonCard(data, displayIdx);
            (data.isUnlocked ? _unlockedGrid : _lockedGrid).Add(card);
        }

        _classCardsBuilt = true;
        Debug.Log($"[MainMenuController] Built {lessons.Length} lesson cards");
    }

    private VisualElement BuildLessonCard(LessonData data, int index)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("lesson-card");
        if (!data.isUnlocked) card.AddToClassList("locked");

        Label number = new Label(index.ToString("D2"));
        number.AddToClassList("lesson-number");
        card.Add(number);

        VisualElement icon = new VisualElement();
        icon.AddToClassList("lesson-icon");
        if (data.icon != null)
            icon.style.backgroundImage = new StyleBackground(data.icon);
        card.Add(icon);

        Label title = new Label(data.title);
        title.AddToClassList("lesson-title");
        card.Add(title);

        if (!data.isUnlocked)
        {
            VisualElement lockBadge = new VisualElement();
            lockBadge.AddToClassList("lock-badge");
            Label lockText = new Label(""); // lock unicode glyph — fallback if font supports it
            lockText.text = "L";
            lockText.AddToClassList("lock-badge-text");
            lockBadge.Add(lockText);
            card.Add(lockBadge);
        }
        else if (!string.IsNullOrEmpty(data.sceneName))
        {
            string scene = data.sceneName;
            card.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log($"[MainMenuController] Loading scene: {scene}");
                SceneManager.LoadScene(scene);
            });
            // Visual feedback (cursor)
            card.style.cursor = new StyleCursor();
        }

        return card;
    }

    private VisualElement BuildRow(GlobalLeaderboardEntryDTO e, bool isYou)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("row");
        if (isYou) row.AddToClassList("you");

        VisualElement rankBadge = new VisualElement();
        rankBadge.AddToClassList("rank-badge");
        if (e.rank == 1) rankBadge.AddToClassList("first");
        Label rankText = new Label(e.rank.ToString());
        rankText.AddToClassList("rank-badge-text");
        rankBadge.Add(rankText);
        row.Add(rankBadge);

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

        Label topic = new Label(e.bestStageName);
        topic.AddToClassList("topic-text");
        row.Add(topic);

        Label score = new Label(e.score.ToString());
        score.AddToClassList("score-text");
        row.Add(score);

        return row;
    }
}
