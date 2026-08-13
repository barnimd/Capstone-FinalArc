using System.Collections;
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
    public bool dashboardMigrated = true;
    public bool classMigrated = true;
    public bool profileMigrated = true;
    public bool leaderboardMigrated = true;
    public bool settingsMigrated = false;
    public bool helpMigrated = true;

    [Header("Dashboard slides (drag banners or auto-loaded)")]
    public Sprite[] dashboardSlides;

    [Header("Auto Slide Settings")]
    public float slideInterval = 3f;

    private UIDocument _document;
    private VisualElement _root;

    // Sidebar buttons
    private Button _btnDashboard, _btnClass, _btnProfile, _btnLeaderboard, _btnSettings, _btnHelp;

    // Page VisualElements (UXML)
    private VisualElement _pageDashboard, _pageClass, _pageProfile, _pageLeaderboard, _pageSettings, _pageHelp;

    // Leaderboard refs
    private Label _yourRankValue;
    private Label _topScoreValue, _topScoreSubtitle;
    private ScrollView _rowList;
    private Button _tabGlobal;

    // Navbar
    private Label _navbarGreeting;
    private Label _navbarProfileText;

    // Class page
    private VisualElement _unlockedGrid;
    private VisualElement _lockedGrid;
    private bool _classCardsBuilt;

    // Admin page (role-gated)
    private const string RolePrefKey = "SecMind.role";
    // Manage Class: tombol kartu "Manage Class" + subview grid level — sudah berfungsi penuh.
    // Manage User: DI-NONAKTIFKAN (comment out). Menu "Manage Users" sengaja dihilangkan dari
    //   Menu Admin — lihat UXML MenuView.uxml (kartu btn-manage-users & subview admin-manage-users
    //   ikut di-comment). Kalau mau diaktifkan lagi, uncomment baris bertanda [Manage User] di
    //   file ini dan di UXML. Field asli yang dinonaktifkan:
    //     private Button _btnManageUsers;            // [Manage User] tombol kartu "Manage Users"
    //     private Button _btnAdminBackUsers;         // [Manage User] tombol "← Kembali" subview users
    //     private VisualElement _adminManageUsers;   // [Manage User] subview daftar user
    private Button _btnAdmin, _btnManageClass, _btnAdminBackClass;
    private VisualElement _pageAdmin, _adminHome, _adminManageClass, _adminLevelGrid;
    // Manage Class: element ikon PNG (background-image) yang dimuat dari Resources/Icons/icon-manage-class.png.
    private VisualElement _manageClassIcon;
    private bool _adminGridBuilt;
    private bool _isAdmin;

    // Kelola Poster — slideshow dashboard yang datanya dari server (lihat PosterManager).
    private Button _btnManagePosters, _btnAdminBackPosters, _btnPosterUpload;
    private VisualElement _adminManagePosters, _posterList;
    private Label _posterStatus;
    // Satu operasi poster jalan pada satu waktu. Upload bisa 2–5 detik, dan klik
    // kedua di tengah itu bakal ngirim urutan yang dihitung dari data basi.
    private bool _posterBusy;

    // Dashboard page
    private VisualElement _slideshowImage;
    private Button _slideshowPrev;
    private Button _slideshowNext;
    private VisualElement _slideshowDots;
    private int _slideIndex;
    private bool _dashboardWired;
    // True once PosterManager has slides; drives whether SlideCount/UpdateSlide read
    // from the server set or fall back to the dashboardSlides array above.
    private bool _usingServerPosters;
    private Coroutine _autoSlideCoroutine;

    // Help page
    private VisualElement _faqList;
    private VisualElement _comingSoonModal;
    private Label _modalDesc;
    private bool _helpWired;

    // Profile page
    private Label _profileGreeting;
    private Label _profileAvatarText;
    private Label _profileName;
    private Label _profileMeta;
    private Label _profileLevelCleared;
    private Label _profileAchievements;
    private Label _profilePlaytime;
    private Label _profileRank;
    private VisualElement _courseProgressList;
    private VisualElement _gameProgressList;
    private bool _profilePopulated;

    // Profile — backend stage data (stage_completions). The 6 stages that count toward
    // "Level Cleared". Lesson → stage id mapping lives in LessonLocks.
    private static readonly string[] ProfileStageIds =
        { "phishing", "2fa", "password-security", "malware-awareness", "wifi-security", "ransomware" };

    private readonly Dictionary<string, int> _stageScores = new Dictionary<string, int>();
    private readonly HashSet<string> _stageCompleted = new HashSet<string>();
    private int _stageResponsesPending;

    private string _currentPage = "dashboard";

    void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        if (_root == null)
        {
            Debug.LogWarning("[MainMenuController] rootVisualElement is null");
            return;
        }

        // Sidebar buttons
        _btnDashboard = _root.Q<Button>("btn-dashboard");
        _btnClass = _root.Q<Button>("btn-class");
        _btnProfile = _root.Q<Button>("btn-profile");
        _btnLeaderboard = _root.Q<Button>("btn-leaderboard");
        _btnSettings = _root.Q<Button>("btn-settings");
        _btnHelp = _root.Q<Button>("btn-help");
        _btnAdmin = _root.Q<Button>("btn-admin");

        // Pages
        _pageDashboard = _root.Q<VisualElement>("page-dashboard");
        _pageClass = _root.Q<VisualElement>("page-class");
        _pageProfile = _root.Q<VisualElement>("page-profile");
        _pageLeaderboard = _root.Q<VisualElement>("page-leaderboard");
        _pageSettings = _root.Q<VisualElement>("page-settings");
        _pageHelp = _root.Q<VisualElement>("page-help");
        _pageAdmin = _root.Q<VisualElement>("page-admin");

        // Admin sub-views + buttons
        _adminHome = _root.Q<VisualElement>("admin-home");
        // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan:
        //   _adminManageUsers = _root.Q<VisualElement>("admin-manage-users");
        _adminManageClass = _root.Q<VisualElement>("admin-manage-class");
        _adminLevelGrid = _root.Q<VisualElement>("admin-level-grid");
        _adminManagePosters = _root.Q<VisualElement>("admin-manage-posters");
        _posterList = _root.Q<VisualElement>("poster-list");
        _posterStatus = _root.Q<Label>("poster-status");
        // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan:
        //   _btnManageUsers = _root.Q<Button>("btn-manage-users");
        _btnManageClass = _root.Q<Button>("btn-manage-class");
        // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan:
        //   _btnAdminBackUsers = _root.Q<Button>("btn-admin-back-users");
        _btnAdminBackClass = _root.Q<Button>("btn-admin-back-class");
        _btnManagePosters = _root.Q<Button>("btn-manage-posters");
        _btnAdminBackPosters = _root.Q<Button>("btn-admin-back-posters");
        _btnPosterUpload = _root.Q<Button>("btn-poster-upload");

        // Leaderboard sub-refs
        _yourRankValue = _root.Q<Label>("your-rank-value");
        _topScoreValue = _root.Q<Label>("top-score-value");
        _topScoreSubtitle = _root.Q<Label>("top-score-subtitle");
        _rowList = _root.Q<ScrollView>("row-list");

        _tabGlobal = _root.Q<Button>("tab-global");

        // Navbar
        _navbarGreeting = _root.Q<Label>("navbar-greeting");
        _navbarProfileText = _root.Q<Label>("navbar-profile-text");

        // Dashboard slideshow refs
        _slideshowImage = _root.Q<VisualElement>("slideshow-image");
        _slideshowPrev = _root.Q<Button>("slideshow-prev");
        _slideshowNext = _root.Q<Button>("slideshow-next");
        _slideshowDots = _root.Q<VisualElement>("slideshow-dots");

        // Wire slideshow buttons (separate methods so RestartAutoSlide only fires on manual click)
        if (_slideshowPrev != null) _slideshowPrev.clicked += OnPrevClicked;
        if (_slideshowNext != null) _slideshowNext.clicked += OnNextClicked;

        // Class page grids
        _unlockedGrid = _root.Q<VisualElement>("unlocked-grid");
        _lockedGrid = _root.Q<VisualElement>("locked-grid");

        // Profile page refs
        _profileGreeting = _root.Q<Label>("profile-greeting");
        _profileAvatarText = _root.Q<Label>("profile-avatar-text");
        _profileName = _root.Q<Label>("profile-name");
        _profileMeta = _root.Q<Label>("profile-meta");
        _profileLevelCleared = _root.Q<Label>("profile-level-cleared");
        _profileAchievements = _root.Q<Label>("profile-achievements");
        _profilePlaytime = _root.Q<Label>("profile-playtime");
        _profileRank = _root.Q<Label>("profile-rank");
        _courseProgressList = _root.Q<VisualElement>("course-progress-list");
        _gameProgressList = _root.Q<VisualElement>("game-progress-list");

        // Wire sidebar button clicks
        if (_btnDashboard != null) _btnDashboard.clicked += () => ShowPage("dashboard");
        if (_btnClass != null) _btnClass.clicked += () => ShowPage("class");
        if (_btnProfile != null) _btnProfile.clicked += () => ShowPage("profile");
        if (_btnLeaderboard != null) _btnLeaderboard.clicked += () => ShowPage("leaderboard");
        if (_btnSettings != null) _btnSettings.clicked += () => ShowPage("settings");
        if (_btnHelp != null) _btnHelp.clicked += () => ShowPage("help");
        if (_btnAdmin != null) _btnAdmin.clicked += () => ShowPage("admin");

        // Admin sub-navigation
        // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan:
        //   if (_btnManageUsers != null) _btnManageUsers.clicked += OpenManageUsers;
        if (_btnManageClass != null) _btnManageClass.clicked += OpenManageClass;
        // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan:
        //   if (_btnAdminBackUsers != null) _btnAdminBackUsers.clicked += ShowAdminHome;
        if (_btnAdminBackClass != null) _btnAdminBackClass.clicked += ShowAdminHome;
        if (_btnManagePosters != null) _btnManagePosters.clicked += OpenManagePosters;
        if (_btnAdminBackPosters != null) _btnAdminBackPosters.clicked += ShowAdminHome;
        if (_btnPosterUpload != null) _btnPosterUpload.clicked += OnPosterUploadClicked;

        // Manage Class: isi ikon PNG pada kartu "Manage Class" (Resources/Icons/icon-manage-class.png).
        LoadManageClassIcon();

        // Role gating: hide admin button by default, apply cached role, then refresh from server.
        if (_btnAdmin != null) _btnAdmin.style.display = DisplayStyle.None;
        ApplyRole(PlayerPrefs.GetString(RolePrefKey, ""));
        RefreshRoleFromServer();

        // Pull the admin-set lock state so the Class page is correct on first open.
        RefreshLocksFromServer();

        if (_tabGlobal != null) _tabGlobal.clicked += () => SelectFilterTab(_tabGlobal);

        UpdateNavbarGreeting();

        // Default to dashboard
        ShowPage(_currentPage);
    }

    void OnDisable()
    {
        StopAutoSlide();
    }

    // ── Navbar ──────────────────────────────────────────────────────────────

    private void UpdateNavbarGreeting()
    {
        if (_navbarGreeting == null) return;
        string raw = FirebaseManager.Instance != null && !string.IsNullOrEmpty(FirebaseManager.Instance.Username)
            ? FirebaseManager.Instance.Username
            : "Pemain";
        string display = CapitalizeFirst(raw);
        _navbarGreeting.text = $"Hai, {display}";

        if (_navbarProfileText != null)
            _navbarProfileText.text = string.IsNullOrEmpty(display) ? "?" : display.Substring(0, 1).ToUpper();
    }

    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    // ── Page routing ────────────────────────────────────────────────────────

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
        HideElement(_pageAdmin);

        // 3. Hide all old uGUI canvases
        if (canvasDashboard != null) canvasDashboard.SetActive(false);
        if (canvasClass != null) canvasClass.SetActive(false);
        if (canvasProfile != null) canvasProfile.SetActive(false);
        if (canvasSettings != null) canvasSettings.SetActive(false);
        if (canvasGetHelp != null) canvasGetHelp.SetActive(false);

        // 4. Show the target page (UXML if migrated, else activate old canvas)
        switch (pageName)
        {
            case "dashboard":
                if (dashboardMigrated)
                {
                    ShowElement(_pageDashboard);
                    SetupDashboardIfNeeded();
                    // Resume auto-slide if not already running
                    if (_autoSlideCoroutine == null && SlideCount > 1)
                        _autoSlideCoroutine = StartCoroutine(AutoSlideCoroutine());
                }
                else if (canvasDashboard != null) canvasDashboard.SetActive(true);
                else ShowElement(_pageDashboard);
                break;

            case "class":
                StopAutoSlide();
                if (classMigrated)
                {
                    ShowElement(_pageClass);
                    // Paint from the cached state immediately, then correct it from the
                    // server so an admin's lock lands without a restart.
                    BuildClassCardsIfNeeded();
                    RefreshLocksFromServer();
                }
                else if (canvasClass != null) canvasClass.SetActive(true);
                else ShowElement(_pageClass);
                break;

            case "profile":
                StopAutoSlide();
                if (profileMigrated)
                {
                    ShowElement(_pageProfile);
                    PopulateProfileIfNeeded();
                }
                else if (canvasProfile != null) canvasProfile.SetActive(true);
                else ShowElement(_pageProfile);
                break;

            case "leaderboard":
                StopAutoSlide();
                ShowElement(_pageLeaderboard);
                RefreshLeaderboard();
                break;

            case "settings":
                StopAutoSlide();
                if (settingsMigrated) ShowElement(_pageSettings);
                else if (canvasSettings != null) canvasSettings.SetActive(true);
                else ShowElement(_pageSettings);
                break;

            case "help":
                StopAutoSlide();
                if (helpMigrated)
                {
                    ShowElement(_pageHelp);
                    SetupHelpIfNeeded();
                }
                else if (canvasGetHelp != null) canvasGetHelp.SetActive(true);
                else ShowElement(_pageHelp);
                break;

            case "admin":
                StopAutoSlide();
                // Guard: non-admins should never reach this page.
                if (!_isAdmin) { ShowPage("dashboard"); return; }
                ShowElement(_pageAdmin);
                ShowAdminHome(); // Default ke admin-home (kartu "Manage Class" saja — "Manage Users" dihilangkan).
                break;
        }
    }

    private void SetActiveButton(string pageName)
    {
        SetButtonActive(_btnDashboard, pageName == "dashboard");
        SetButtonActive(_btnClass, pageName == "class");
        SetButtonActive(_btnProfile, pageName == "profile");
        SetButtonActive(_btnLeaderboard, pageName == "leaderboard");
        SetButtonActive(_btnSettings, pageName == "settings");
        SetButtonActive(_btnHelp, pageName == "help");
        SetButtonActive(_btnAdmin, pageName == "admin");
    }

    private void SetButtonActive(Button btn, bool isActive)
    {
        if (btn == null) return;
        if (isActive) btn.AddToClassList("active");
        else btn.RemoveFromClassList("active");
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
        if (_tabGlobal != null) _tabGlobal.RemoveFromClassList("active");
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
            Label emptyText = new Label("Belum ada skor — jadi yang pertama!");
            emptyText.AddToClassList("empty-text");
            empty.Add(emptyText);
            _rowList.Add(empty);
            return;
        }

        string myUid = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : null;

        if (_topScoreValue != null) _topScoreValue.text = entries[0].score.ToString();
        if (_topScoreSubtitle != null) _topScoreSubtitle.text = $"oleh {entries[0].displayName}";

        GlobalLeaderboardEntryDTO mine = !string.IsNullOrEmpty(myUid)
            ? entries.FirstOrDefault(e => e.userId == myUid)
            : null;

        if (mine != null)
        {
            if (_yourRankValue != null) _yourRankValue.text = "#" + mine.rank;
        }
        else
        {
            // User belum masuk daftar top-N yang tampil — cari peringkat global-nya
            // dari daftar yang lebih besar (sama seperti halaman Profil).
            if (_yourRankValue != null) _yourRankValue.text = "…";
            if (!string.IsNullOrEmpty(myUid) && LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.FetchGlobal(100, OnMyGlobalRankLoaded);
        }

        foreach (GlobalLeaderboardEntryDTO e in entries)
            _rowList.Add(BuildRow(e, isYou: e.userId == myUid));
    }

    // Isi kartu PERINGKATMU dengan peringkat global user (dari top 100).
    // Dipakai kalau user tidak masuk daftar top-N yang tampil di tabel.
    private void OnMyGlobalRankLoaded(bool ok, GlobalLeaderboardEntryDTO[] entries)
    {
        if (!ok || entries == null) return;

        string myUid = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : null;
        if (string.IsNullOrEmpty(myUid)) return;

        GlobalLeaderboardEntryDTO mine = entries.FirstOrDefault(e => e.userId == myUid);
        if (mine != null)
        {
            if (_yourRankValue != null) _yourRankValue.text = "#" + mine.rank;
        }
        else
        {
            if (_yourRankValue != null) _yourRankValue.text = "—";
        }
    }

    // ── Help page ──────────────────────────────────────────────────────────

    private static readonly (string question, string answer)[] _faqs = new (string, string)[]
    {
        ("Bagaimana cara membuka modul berikutnya?",
         "Selesaikan modul saat ini dengan skor 70% atau lebih untuk membuka modul berikutnya secara berurutan."),
        ("Bolehkah mengulang kuis untuk memperbaiki skor?",
         "Bisa! Kamu dapat memutar ulang modul yang sudah selesai kapan saja. Skor tertinggimu yang dihitung di papan peringkat, jadi mengulang hanya menguntungkanmu."),
        ("Kenapa skor saya tidak muncul di papan peringkat?",
         "Skor disinkronkan ke server setiap beberapa detik. Jika masih belum muncul setelah satu menit, pastikan modul diselesaikan sampai tuntas dan coba muat ulang halaman."),
        ("Bagaimana cara kerja sistem streak?",
         "Mainkan minimal satu modul per hari untuk menjaga streak tetap hidup. Jika bolos satu hari, streak kembali ke nol. Streak memberi hadiah bonus setiap 7 hari."),
        ("Apakah progres saya tersimpan kalau keluar (logout)?",
         "Ya. Progres, skor, dan checkpoint terikat ke akunmu. Masuk lagi di perangkat mana pun dan lanjutkan dari tempat terakhirmu."),
    };

    private void SetupHelpIfNeeded()
    {
        if (_helpWired) return;
        if (_pageHelp == null) return;

        _faqList = _pageHelp.Q<VisualElement>("help-faq-list");
        _comingSoonModal = _pageHelp.Q<VisualElement>("coming-soon-modal");
        _modalDesc = _pageHelp.Q<Label>("modal-desc");

        Debug.Log($"[MainMenuController] Help refs: faqList={_faqList != null} modal={_comingSoonModal != null} modalDesc={_modalDesc != null}");

        // Wire 3 topic cards + contact button + search → show modal
        WireComingSoon(_pageHelp.Q<Button>("help-topic-1"), "Detail panduan 'How to play' belum tersedia.");
        WireComingSoon(_pageHelp.Q<Button>("help-topic-2"), "Detail 'Account & login' belum tersedia.");
        WireComingSoon(_pageHelp.Q<Button>("help-topic-3"), "Form 'Report a bug' belum tersedia.");
        WireComingSoon(_pageHelp.Q<Button>("help-contact-btn"), "Live chat dengan SecMind team belum tersedia.");
        WireComingSoon(_pageHelp.Q<Button>("help-search-btn"), "Fitur pencarian belum tersedia.");

        // Modal close button
        Button closeBtn = _pageHelp.Q<Button>("modal-close-btn");
        if (closeBtn != null)
        {
            closeBtn.clicked += HideComingSoonModal;
            closeBtn.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("[MainMenuController] modal-close-btn ClickEvent fired");
                HideComingSoonModal();
                evt.StopPropagation();
            });
        }
        else
        {
            Debug.LogWarning("[MainMenuController] modal-close-btn not found in page-help!");
        }

        // Stop click propagation on modal-card so clicks inside card don't bubble to backdrop
        VisualElement modalCard = _pageHelp.Q<VisualElement>(className: "modal-card");
        if (modalCard != null)
            modalCard.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        // Click on backdrop (outside card) closes modal
        if (_comingSoonModal != null)
        {
            _comingSoonModal.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("[MainMenuController] backdrop ClickEvent — closing modal");
                HideComingSoonModal();
            });
        }

        // Build FAQ list
        if (_faqList != null)
        {
            _faqList.Clear();
            foreach (var (question, answer) in _faqs)
                _faqList.Add(BuildFaqItem(question, answer));
        }

        _helpWired = true;
    }

    private void WireComingSoon(Button btn, string desc)
    {
        if (btn == null) return;
        btn.clicked += () => ShowComingSoonModal(desc);
    }

    private void ShowComingSoonModal(string desc)
    {
        if (_comingSoonModal == null) return;
        if (_modalDesc != null) _modalDesc.text = desc;
        _comingSoonModal.RemoveFromClassList("hidden");
    }

    private void HideComingSoonModal()
    {
        if (_comingSoonModal == null) return;
        _comingSoonModal.AddToClassList("hidden");
    }

    private VisualElement BuildFaqItem(string question, string answer)
    {
        VisualElement item = new VisualElement();
        item.AddToClassList("help-faq-item");

        VisualElement qRow = new VisualElement();
        qRow.AddToClassList("help-faq-question");
        Label qText = new Label(question);
        qText.AddToClassList("help-faq-q-text");
        Label chevron = new Label("▾");
        chevron.AddToClassList("help-faq-chevron");
        qRow.Add(qText);
        qRow.Add(chevron);
        item.Add(qRow);

        Label answerLabel = new Label(answer);
        answerLabel.AddToClassList("help-faq-answer");
        item.Add(answerLabel);

        item.RegisterCallback<ClickEvent>(evt =>
        {
            if (item.ClassListContains("expanded"))
                item.RemoveFromClassList("expanded");
            else
                item.AddToClassList("expanded");
        });

        return item;
    }

    // ── Dashboard slideshow ─────────────────────────────────────────────────

    // Slides come from the server once posters have loaded; until then, and whenever
    // the fetch fails, they come from the Inspector array.
    private int SlideCount => _usingServerPosters
        ? PosterManager.Count
        : (dashboardSlides != null ? dashboardSlides.Length : 0);

    private void SetupDashboardIfNeeded()
    {
        if (_dashboardWired) return;
        if (_slideshowImage == null) return;

        // Paint the shipped sprites first so the dashboard is never blank while the
        // request is in flight, then swap to the server set when it arrives.
        _usingServerPosters = false;
        BuildSlideshow();
        _dashboardWired = true;

        if (PosterManager.IsLoaded)
        {
            _usingServerPosters = true;
            BuildSlideshow();
            return;
        }

        PosterManager.Load(ok =>
        {
            // On failure the Inspector sprites stay up. A dashboard showing the old
            // banners beats one showing nothing.
            if (!ok) return;
            _usingServerPosters = true;
            BuildSlideshow();
        });
    }

    // Rebuilds the dots and resets to the first slide. Called again whenever the
    // source changes, so the dot count always matches what is actually on screen.
    private void BuildSlideshow()
    {
        int count = SlideCount;
        if (count == 0)
        {
            if (!_usingServerPosters)
                Debug.LogWarning("[MainMenuController] dashboardSlides is empty — assign banner sprites in Inspector");
            return;
        }

        if (_slideshowDots != null)
        {
            _slideshowDots.Clear();
            for (int i = 0; i < count; i++)
            {
                VisualElement dot = new VisualElement();
                dot.AddToClassList("slideshow-dot");
                _slideshowDots.Add(dot);
            }
        }

        _slideIndex = 0;
        UpdateSlide();

        StopAutoSlide();
        if (count > 1) _autoSlideCoroutine = StartCoroutine(AutoSlideCoroutine());
    }

    // Auto-slide coroutine — only advances the index, does NOT call RestartAutoSlide
    private IEnumerator AutoSlideCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(slideInterval);
            int count = SlideCount;
            if (count == 0) yield break;
            _slideIndex = (_slideIndex + 1) % count;
            UpdateSlide();
        }
    }

    // Called by manual button clicks — advances slide AND resets the timer
    private void OnNextClicked()
    {
        int count = SlideCount;
        if (count == 0) return;
        _slideIndex = (_slideIndex + 1) % count;
        UpdateSlide();
        RestartAutoSlide();
    }

    private void OnPrevClicked()
    {
        int count = SlideCount;
        if (count == 0) return;
        _slideIndex = (_slideIndex - 1 + count) % count;
        UpdateSlide();
        RestartAutoSlide();
    }

    private void RestartAutoSlide()
    {
        if (_autoSlideCoroutine != null) StopCoroutine(_autoSlideCoroutine);
        _autoSlideCoroutine = StartCoroutine(AutoSlideCoroutine());
    }

    private void StopAutoSlide()
    {
        if (_autoSlideCoroutine != null)
        {
            StopCoroutine(_autoSlideCoroutine);
            _autoSlideCoroutine = null;
        }
    }

    private void UpdateSlide()
    {
        if (_slideshowImage == null) return;

        int count = SlideCount;
        if (count == 0) return;
        // The source can change under us mid-cycle (server posters arriving, or an
        // admin deleting one), so clamp instead of trusting the old index.
        if (_slideIndex < 0 || _slideIndex >= count) _slideIndex = 0;

        if (_usingServerPosters)
        {
            Texture2D tex = PosterManager.GetTexture(_slideIndex);
            if (tex != null)
                _slideshowImage.style.backgroundImage = new StyleBackground(tex);
        }
        else
        {
            Sprite slide = dashboardSlides[_slideIndex];
            if (slide != null)
                _slideshowImage.style.backgroundImage = new StyleBackground(slide);
        }

        // Update dot active state
        if (_slideshowDots != null)
        {
            for (int i = 0; i < _slideshowDots.childCount; i++)
            {
                VisualElement dot = _slideshowDots[i];
                if (i == _slideIndex) dot.AddToClassList("active");
                else dot.RemoveFromClassList("active");
            }
        }
    }

    // ── Profile page ────────────────────────────────────────────────────────

    private void PopulateProfileIfNeeded()
    {
        // Always refresh (in case Username changed after login)
        string raw = FirebaseManager.Instance != null && !string.IsNullOrEmpty(FirebaseManager.Instance.Username)
            ? FirebaseManager.Instance.Username
            : "Pemain";
        string display = CapitalizeFirst(raw);

        if (_profileGreeting != null) _profileGreeting.text = $"Hai, {display}";
        if (_profileName != null) _profileName.text = display;
        if (_profileAvatarText != null) _profileAvatarText.text = string.IsNullOrEmpty(display) ? "?" : display.Substring(0, 1).ToUpper();

        // Loading placeholders until backend responds.
        if (_profileLevelCleared != null) _profileLevelCleared.text = "… / " + ProfileStageIds.Length;
        if (_profileRank != null) _profileRank.text = "—";
        if (_profileMeta != null) _profileMeta.text = "Bergabung 2026 · Peringkat — global";

        // Level Cleared (# completed) and per-course scores both come from stage_completions,
        // read per stage via checkpoint/load for the logged-in user.
        FetchStageData();

        // Global Rank from the global leaderboard.
        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.FetchGlobal(100, OnProfileLeaderboardLoaded);

        _profilePopulated = true;
    }

    // Fetches completion + best score for each of the 6 stages, then fills Level Cleared
    // (count of completed) and Course Progress (each course's score).
    private void FetchStageData()
    {
        _stageScores.Clear();
        _stageCompleted.Clear();

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[MainMenuController] APIClient missing — profile stage data unavailable");
            if (_profileLevelCleared != null) _profileLevelCleared.text = "— / " + ProfileStageIds.Length;
            BuildCourseProgress();
            return;
        }

        _stageResponsesPending = ProfileStageIds.Length;
        foreach (string sid in ProfileStageIds)
        {
            string stage = sid; // capture per-iteration
            APIClient.Instance.CheckpointLoad(stage, (ok, resp, raw) =>
            {
                if (ok && resp != null && resp.isCompleted)
                {
                    _stageCompleted.Add(stage);
                    _stageScores[stage] = resp.bestScore ?? 0;
                }
                _stageResponsesPending--;
                if (_stageResponsesPending <= 0)
                    OnAllStageDataLoaded();
            });
        }
    }

    private void OnAllStageDataLoaded()
    {
        if (_profileLevelCleared != null)
            _profileLevelCleared.text = _stageCompleted.Count + " / " + ProfileStageIds.Length;
        BuildCourseProgress();
    }

    // Course Progress: one row per unlocked lesson, showing that stage's score (not a percent).
    private void BuildCourseProgress()
    {
        if (_courseProgressList == null) return;
        _courseProgressList.Clear();

        LessonData[] lessons = Resources.LoadAll<LessonData>("Lessons");
        System.Array.Sort(lessons, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        foreach (LessonData l in lessons)
        {
            if (l == null || !l.IsUnlocked) continue;
            string stage = LessonLocks.StageIdForScene(l.sceneName);
            int score = (stage != null && _stageScores.TryGetValue(stage, out int s)) ? s : 0;
            _courseProgressList.Add(BuildCourseItem(l.title, score));
        }
    }

    // Like BuildProgressItem but the value label shows the raw score (e.g. "85"), not "85%".
    private VisualElement BuildCourseItem(string name, int score)
    {
        VisualElement item = new VisualElement();
        item.AddToClassList("progress-item");

        VisualElement header = new VisualElement();
        header.AddToClassList("progress-item-header");

        Label nameLabel = new Label(name);
        nameLabel.AddToClassList("progress-item-name");
        header.Add(nameLabel);

        Label val = new Label(score.ToString());
        val.AddToClassList("progress-item-percent");
        header.Add(val);

        item.Add(header);

        VisualElement bg = new VisualElement();
        bg.AddToClassList("progress-bar-bg");

        VisualElement fill = new VisualElement();
        fill.AddToClassList("progress-bar-fill");
        fill.style.width = new Length(Mathf.Clamp(score, 0, 100), LengthUnit.Percent);

        bg.Add(fill);
        item.Add(bg);

        return item;
    }

    private void OnProfileLeaderboardLoaded(bool ok, GlobalLeaderboardEntryDTO[] entries)
    {
        if (!ok || entries == null) return;

        string myUid = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : null;
        if (string.IsNullOrEmpty(myUid)) return;

        GlobalLeaderboardEntryDTO mine = entries.FirstOrDefault(e => e.userId == myUid);
        if (mine != null)
        {
            if (_profileRank != null) _profileRank.text = "#" + mine.rank;
            if (_profileMeta != null) _profileMeta.text = $"Bergabung 2026 · Peringkat #{mine.rank} global";
        }
        else
        {
            if (_profileRank != null) _profileRank.text = "Tanpa peringkat";
        }
    }

    private VisualElement BuildProgressItem(string name, int percent)
    {
        VisualElement item = new VisualElement();
        item.AddToClassList("progress-item");

        VisualElement header = new VisualElement();
        header.AddToClassList("progress-item-header");

        Label nameLabel = new Label(name);
        nameLabel.AddToClassList("progress-item-name");
        header.Add(nameLabel);

        Label pct = new Label(percent + "%");
        pct.AddToClassList("progress-item-percent");
        header.Add(pct);

        item.Add(header);

        VisualElement bg = new VisualElement();
        bg.AddToClassList("progress-bar-bg");

        VisualElement fill = new VisualElement();
        fill.AddToClassList("progress-bar-fill");
        fill.style.width = new Length(Mathf.Clamp(percent, 0, 100), LengthUnit.Percent);

        bg.Add(fill);
        item.Add(bg);

        return item;
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

        LessonData[] lessons = Resources.LoadAll<LessonData>("Lessons");
        if (lessons == null || lessons.Length == 0)
        {
            Debug.LogWarning("[MainMenuController] No LessonData found in Resources/Lessons");
            return;
        }

        System.Array.Sort(lessons, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        int unlockedIdx = 1, lockedIdx = 1;
        foreach (LessonData data in lessons)
        {
            if (data == null) continue;
            bool unlocked = data.IsUnlocked;
            int displayIdx = unlocked ? unlockedIdx++ : lockedIdx++;
            VisualElement card = BuildLessonCard(data, displayIdx);
            (unlocked ? _unlockedGrid : _lockedGrid).Add(card);
        }

        _classCardsBuilt = true;
        Debug.Log($"[MainMenuController] Built {lessons.Length} lesson cards");
    }

    // Drops the cached Class cards so they rebuild with the current lock state
    // (called after a server refresh or an admin toggle).
    private void RebuildClassCards()
    {
        _classCardsBuilt = false;
        if (_unlockedGrid != null) _unlockedGrid.Clear();
        if (_lockedGrid != null) _lockedGrid.Clear();
        if (_currentPage == "class") BuildClassCardsIfNeeded();
    }

    private void RebuildAdminGrid()
    {
        _adminGridBuilt = false;
        if (_adminLevelGrid != null) _adminLevelGrid.Clear();
        if (_currentPage == "admin") BuildAdminLevelGridIfNeeded();
    }

    // Fetches lock state for every stage, then redraws whatever grid is on screen.
    private void RefreshLocksFromServer()
    {
        LessonLocks.Refresh(ok =>
        {
            if (!ok) return;
            RebuildClassCards();
            RebuildAdminGrid();
        });
    }

    private VisualElement BuildLessonCard(LessonData data, int index)
    {
        bool unlocked = data.IsUnlocked;

        VisualElement card = new VisualElement();
        card.AddToClassList("lesson-card");
        if (!unlocked) card.AddToClassList("locked");

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

        if (!unlocked)
        {
            VisualElement lockBadge = new VisualElement();
            lockBadge.AddToClassList("lock-badge");
            Label lockText = new Label("L");
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
            card.style.cursor = new StyleCursor();
        }

        return card;
    }

    // ── Menu Admin (role-gated) ─────────────────────────────────────────────

    // Fetches the user's role from the server (UserSync returns it), caches it,
    // and reveals/hides the Menu Admin sidebar button accordingly.
    private void RefreshRoleFromServer()
    {
        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[MainMenuController] Not authenticated — using cached role only");
            return;
        }

        string displayName = FirebaseManager.Instance.Username;
        APIClient.Instance.UserSync(displayName, (ok, resp, raw) =>
        {
            if (!ok || resp == null || resp.user == null)
            {
                Debug.LogWarning("[MainMenuController] UserSync failed, keeping cached role: " + raw);
                return;
            }

            string role = string.IsNullOrEmpty(resp.user.role) ? "user" : resp.user.role;
            PlayerPrefs.SetString(RolePrefKey, role);
            PlayerPrefs.Save();
            ApplyRole(role);
            Debug.Log($"[MainMenuController] Role = {role} (isAdmin={_isAdmin})");
        });
    }

    private void ApplyRole(string role)
    {
#if UNITY_EDITOR
        // Editor debug: Menu Admin SELALU muncul di editor, walau belum login / role server masih "user".
        // Cuma untuk testing UI — tidak berlaku di build WebGL (compile ke #else).
        _isAdmin = true;
#else
        _isAdmin = !string.IsNullOrEmpty(role) && role == "admin";
#endif
        if (_btnAdmin != null)
            _btnAdmin.style.display = _isAdmin ? DisplayStyle.Flex : DisplayStyle.None;

        // If a non-admin somehow sits on the admin page, kick back to dashboard.
        if (!_isAdmin && _currentPage == "admin")
            ShowPage("dashboard");
    }

    // Kembali ke halaman admin-home (menampilkan kartu "Manage Class" — "Manage Users" dihilangkan).
    private void ShowAdminHome()
    {
        SetAdminSubviews(home: true, users: false, cls: false, posters: false);
    }

    // [Manage User] DI-COMMENT — menu "Manage Users" dihilangkan dari Menu Admin:
    //   private void OpenManageUsers()
    //   {
    //       SetAdminSubviews(home: false, users: true, cls: false);
    //   }

    private void OpenManageClass()
    {
        SetAdminSubviews(home: false, users: false, cls: true, posters: false);
        BuildAdminLevelGridIfNeeded();
        RefreshLocksFromServer();
    }

    // Manage User: show/hide antar admin-home, subview users (di-comment), subview Manage Class,
    // dan subview Kelola Poster.
    private void SetAdminSubviews(bool home, bool users, bool cls, bool posters)
    {
        ToggleHidden(_adminHome, !home);
        // [Manage User] DI-COMMENT — subview users dihapus:
        //   ToggleHidden(_adminManageUsers, !users);
        ToggleHidden(_adminManageClass, !cls);
        ToggleHidden(_adminManagePosters, !posters);
    }

    // Manage Class icon: memuat PNG dari folder Resources/Icons (nama file: icon-manage-class.png)
    // lalu dipasang sebagai background-image pada element #manage-class-icon di UXML.
    // Taruh PNG di Assets/Resources/Icons/icon-manage-class.png agar ikut ter-build WebGL.
    private void LoadManageClassIcon()
    {
        _manageClassIcon = _root.Q<VisualElement>("manage-class-icon");
        if (_manageClassIcon == null) return;

        Texture2D tex = Resources.Load<Texture2D>("Icons/icon-manage-class");
        if (tex != null)
            _manageClassIcon.style.backgroundImage = new StyleBackground(tex);
        else
            Debug.LogWarning("[MainMenuController] Icon Manage Class tidak ditemukan: Resources/Icons/icon-manage-class.png");
    }

    private void ToggleHidden(VisualElement el, bool hidden)
    {
        if (el == null) return;
        if (hidden) el.AddToClassList("hidden");
        else el.RemoveFromClassList("hidden");
    }

    // Builds the Manage Class level grid (gambar 1) from LessonData — reuses the
    // lesson-card look and adds an Unlocked/Locked status pill per level.
    private void BuildAdminLevelGridIfNeeded()
    {
        if (_adminGridBuilt) return;
        if (_adminLevelGrid == null)
        {
            Debug.LogWarning("[MainMenuController] admin-level-grid not found in UXML");
            return;
        }

        LessonData[] lessons = Resources.LoadAll<LessonData>("Lessons");
        if (lessons == null || lessons.Length == 0)
        {
            Debug.LogWarning("[MainMenuController] No LessonData found in Resources/Lessons");
            return;
        }

        System.Array.Sort(lessons, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        int idx = 1;
        foreach (LessonData data in lessons)
        {
            if (data == null) continue;
            _adminLevelGrid.Add(BuildAdminLevelCard(data, idx++));
        }

        _adminGridBuilt = true;
        Debug.Log($"[MainMenuController] Built {lessons.Length} admin level cards");
    }

    private VisualElement BuildAdminLevelCard(LessonData data, int index)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("lesson-card");
        card.AddToClassList("admin-level-card");

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

        // Status pill — click writes the new lock state to the server. The pill is
        // disabled until the server answers, and only then does the UI flip, so a
        // rejected call (e.g. role revoked) can't show a state that isn't real.
        Button status = new Button();
        status.AddToClassList("admin-status-btn");
        status.clicked += () =>
        {
            bool unlockedNow = data.IsUnlocked;
            status.SetEnabled(false);
            LessonLocks.SetLocked(data, unlockedNow, ok =>
            {
                status.SetEnabled(true);
                if (!ok)
                {
                    Debug.LogWarning($"[MainMenuController] (Admin) toggle for '{data.title}' failed — state unchanged");
                    return;
                }
                ApplyAdminStatus(card, status, data.IsUnlocked);
                RebuildClassCards();
                Debug.Log($"[MainMenuController] (Admin) '{data.title}' -> {(data.IsUnlocked ? "Unlocked" : "Locked")}");
            });
        };
        ApplyAdminStatus(card, status, data.IsUnlocked);
        card.Add(status);

        return card;
    }

    private static void ApplyAdminStatus(VisualElement card, Button status, bool unlocked)
    {
        card.EnableInClassList("locked", !unlocked);
        status.EnableInClassList("admin-status-unlocked", unlocked);
        status.EnableInClassList("admin-status-locked", !unlocked);
        status.text = unlocked ? "Terbuka" : "Terkunci";
    }

    // ── Menu Admin > Kelola Poster ──────────────────────────────────────────

    private void OpenManagePosters()
    {
        SetAdminSubviews(home: false, users: false, cls: false, posters: true);
        LoadPosterList();
    }

    // Always refetches. The admin opened this page to see the current state, and
    // a stale list would make the reorder payload wrong.
    private void LoadPosterList()
    {
        if (_posterList == null) return;

        SetPosterStatus("Memuat…", false);
        PosterManager.Invalidate();
        PosterManager.Load(_ =>
        {
            BuildPosterRows();
            SetPosterStatus(null, false);
        });
    }

    private void BuildPosterRows()
    {
        if (_posterList == null) return;
        _posterList.Clear();

        if (PosterManager.Count == 0)
        {
            Label empty = new Label("Belum ada poster. Klik \"Tambah Poster\" untuk mengunggah.");
            empty.AddToClassList("poster-empty");
            _posterList.Add(empty);
            return;
        }

        for (int i = 0; i < PosterManager.Count; i++)
            _posterList.Add(BuildPosterRow(i));
    }

    private VisualElement BuildPosterRow(int index)
    {
        PosterInfo info = PosterManager.GetPoster(index);
        if (info == null) return new VisualElement();

        VisualElement row = new VisualElement();
        row.AddToClassList("poster-row");

        Label order = new Label((index + 1).ToString("D2"));
        order.AddToClassList("poster-order");
        row.Add(order);

        VisualElement thumb = new VisualElement();
        thumb.AddToClassList("poster-thumb");
        Texture2D tex = PosterManager.GetTexture(index);
        if (tex != null) thumb.style.backgroundImage = new StyleBackground(tex);
        row.Add(thumb);

        Label title = new Label(string.IsNullOrEmpty(info.title) ? $"Poster #{info.id}" : info.title);
        title.AddToClassList("poster-title");
        row.Add(title);

        int captured = index;

        Button up = new Button(() => MovePoster(captured, -1)) { text = "↑" };
        up.AddToClassList("poster-move-btn");
        up.SetEnabled(index > 0);
        row.Add(up);

        Button down = new Button(() => MovePoster(captured, +1)) { text = "↓" };
        down.AddToClassList("poster-move-btn");
        down.SetEnabled(index < PosterManager.Count - 1);
        row.Add(down);

        // Two-step delete instead of a modal: the first click arms the button, the
        // second one actually deletes. A misclick costs nothing and there is no
        // extra UXML to keep in sync.
        Button del = new Button { text = "Hapus" };
        del.AddToClassList("poster-delete-btn");
        bool armed = false;
        int posterId = info.id;
        del.clicked += () =>
        {
            if (!armed)
            {
                armed = true;
                del.text = "Yakin?";
                del.AddToClassList("poster-delete-armed");
                return;
            }
            DeletePoster(posterId);
        };
        row.Add(del);

        return row;
    }

    private void OnPosterUploadClicked()
    {
        if (_posterBusy) return;

        WebGLFilePicker.Open((ok, mimeType, base64, error) =>
        {
            if (!ok)
            {
                // A cancelled dialog reports no error — staying silent is correct there.
                if (!string.IsNullOrEmpty(error)) SetPosterStatus(error, true);
                return;
            }
            SendPosterUpload(mimeType, base64);
        });
    }

    private void SendPosterUpload(string mimeType, string base64)
    {
        if (APIClient.Instance == null)
        {
            SetPosterStatus("APIClient tidak tersedia", true);
            return;
        }

        SetPosterBusy(true, "Mengunggah…");
        APIClient.Instance.UploadPoster(string.Empty, mimeType, base64, (ok, resp, raw) =>
        {
            SetPosterBusy(false, null);
            if (!ok || resp == null || !resp.success)
            {
                SetPosterStatus(DescribeApiError(raw, "Upload gagal"), true);
                return;
            }
            OnPosterSetChanged("Poster ditambahkan");
        });
    }

    private void DeletePoster(int id)
    {
        if (_posterBusy || APIClient.Instance == null) return;

        SetPosterBusy(true, "Menghapus…");
        APIClient.Instance.DeletePoster(id, (ok, resp, raw) =>
        {
            SetPosterBusy(false, null);
            if (!ok || resp == null || !resp.success)
            {
                SetPosterStatus(DescribeApiError(raw, "Gagal menghapus"), true);
                return;
            }
            OnPosterSetChanged("Poster dihapus");
        });
    }

    private void MovePoster(int index, int delta)
    {
        if (_posterBusy || APIClient.Instance == null) return;

        int target = index + delta;
        if (index < 0 || target < 0 || index >= PosterManager.Count || target >= PosterManager.Count) return;

        // The endpoint wants every active id in the new order, not a single move —
        // a partial list would leave the posters it omits on stale positions.
        int[] ids = PosterManager.GetOrderedIds();
        int swap = ids[index];
        ids[index] = ids[target];
        ids[target] = swap;

        SetPosterBusy(true, "Menyimpan urutan…");
        APIClient.Instance.ReorderPosters(ids, (ok, resp, raw) =>
        {
            SetPosterBusy(false, null);
            if (!ok || resp == null || !resp.success)
            {
                SetPosterStatus(DescribeApiError(raw, "Gagal mengubah urutan"), true);
                return;
            }
            OnPosterSetChanged(null);
        });
    }

    // Refetching after a reorder re-downloads every image, which sounds wasteful but
    // is not: the proxy marks responses immutable, so the browser serves them from
    // cache. Trading that for a locally-patched list would risk the UI and the server
    // disagreeing about order.
    private void OnPosterSetChanged(string message)
    {
        // Force SetupDashboardIfNeeded to rebuild, otherwise the dashboard keeps
        // showing the old set until the game restarts.
        _dashboardWired = false;

        LoadPosterList();
        if (!string.IsNullOrEmpty(message)) SetPosterStatus(message, false);
    }

    private void SetPosterBusy(bool busy, string message)
    {
        _posterBusy = busy;
        if (_btnPosterUpload != null) _btnPosterUpload.SetEnabled(!busy);
        if (_posterList != null) _posterList.SetEnabled(!busy);
        if (!string.IsNullOrEmpty(message)) SetPosterStatus(message, false);
    }

    private void SetPosterStatus(string message, bool isError)
    {
        if (_posterStatus == null) return;
        _posterStatus.text = message ?? string.Empty;
        _posterStatus.EnableInClassList("poster-status-error", isError && !string.IsNullOrEmpty(message));
    }

    // Surfaces the server's own message ("Maksimal 12 poster", "File terlalu besar")
    // instead of a generic failure, so the admin knows what to change.
    private static string DescribeApiError(string raw, string fallback)
    {
        if (string.IsNullOrEmpty(raw)) return fallback;
        try
        {
            APIErrorResponse parsed = JsonUtility.FromJson<APIErrorResponse>(raw);
            if (parsed != null && !string.IsNullOrEmpty(parsed.error)) return parsed.error;
        }
        catch (System.Exception)
        {
            // Not JSON — an HTML error page from the platform, most likely.
        }
        return fallback;
    }

    // ── Leaderboard row builder ─────────────────────────────────────────────

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
            Label youBadge = new Label("KAMU");
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