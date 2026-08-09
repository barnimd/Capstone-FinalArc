using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cutscene "kena serangan siber": hacker mancing ke PC player lalu menarik file-file
/// keluar. Dipanggil saat player membalas (Balas) email phishing di Topic 3.
///
/// Semua UI dibangun sendiri saat runtime (overlay canvas sendiri), jadi cukup taruh
/// komponen ini di 1 GameObject kosong di scene lalu assign 4 sprite di Inspector.
/// Urutan animasi mengikuti storyboard:
///   1. layar gelap + merah  → 2. PC & Hacker masuk  → 3. mail terbang PC → hacker
///   4. tali kail nyangkut   → 5. folder ditarik satu per satu ke hacker
///   6. peringatan merah lalu fade out.
/// </summary>
public class PhishingHackAnimation : MonoBehaviour
{
    public static PhishingHackAnimation Instance { get; private set; }

    // ─── Sprite ───────────────────────────────────────────────────────────────
    [Header("=== Sprite (drag di Inspector) ===")]
    public Sprite spritePC;
    public Sprite spriteHacker;
    public Sprite spriteMail;
    public Sprite spriteFolder;

    [Header("=== Ukuran Sprite (px, referensi 1920x1080) ===")]
    public Vector2 sizePC     = new Vector2(340f, 380f);
    public Vector2 sizeHacker = new Vector2(400f, 320f);
    public Vector2 sizeMail   = new Vector2(130f,  95f);
    public Vector2 sizeFolder = new Vector2(120f, 110f);

    // ─── Layout ───────────────────────────────────────────────────────────────
    [Header("=== Posisi & Lintasan ===")]
    public Vector2 posPC      = new Vector2(-520f, -60f);
    public Vector2 posHacker  = new Vector2( 520f, -20f);
    [Tooltip("Tinggi lengkungan tali kail (garis putus-putus).")]
    public float   tinggiBusur = 260f;
    [Tooltip("Jumlah strip garis putus-putus.")]
    public int     jumlahStrip = 22;

    // ─── Timing ───────────────────────────────────────────────────────────────
    [Header("=== Timing (detik) ===")]
    public float durasiGelap    = 0.45f;
    public float durasiMasuk    = 0.55f;
    public float durasiLempar   = 0.90f;
    public float durasiNyangkut = 0.50f;
    public float durasiPerFile  = 0.80f;
    public float jedaAntarFile  = 0.40f;
    public int   jumlahFile     = 3;
    public float durasiOutro    = 1.10f;
    public float durasiFadeOut  = 0.35f;

    // ─── Teks ─────────────────────────────────────────────────────────────────
    [Header("=== Teks ===")]
    public string teksPeringatan    = "DATA KAMU DICURI!";
    [TextArea] public string teksSubPeringatan = "Membalas email phishing = memberi akses ke penyerang.";
    public string formatCounter     = "FILE DICURI  {0}/{1}";
    public bool   bisaDiSkip        = true;

    // ─── Warna ────────────────────────────────────────────────────────────────
    [Header("=== Warna ===")]
    public Color warnaGelap  = new Color(0.02f, 0.03f, 0.05f, 1f);
    public Color warnaMerah  = new Color(1f, 0.17f, 0.23f, 1f);
    public Color warnaGaris  = new Color(1f, 0.28f, 0.32f, 1f);
    [Range(0f, 1f)] public float alphaGelapMaks = 0.94f;

    [Header("=== Debug ===")]
    public bool mainkanSaatStartUntukTes = false;

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private Canvas       _canvas;
    private CanvasGroup  _group;
    private RectTransform _root;
    private RectTransform _stage;
    private RectTransform _dashRoot;
    private Image        _backdrop;
    private Image        _redWash;
    private Image        _pc;
    private Image        _hacker;
    private readonly List<Image> _dashes = new List<Image>();
    private readonly List<Image> _frame  = new List<Image>();
    private TextMeshProUGUI _txtCounter;
    private TextMeshProUGUI _txtWarning;
    private TextMeshProUGUI _txtWarningSub;

    private Vector3 _pcHome;
    private Vector3 _hackerHome;
    private bool    _sedangMain;
    private bool    _mintaSkip;
    private int     _fileSampai;
    private Coroutine _shakeRoutine;
    private static Sprite _spriteKotak;

    public bool SedangMain => _sedangMain;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject != gameObject)
            Debug.LogWarning("[PhishingHackAnimation] Ada lebih dari satu instance di scene.");
        Instance = this;
    }

    private void Start()
    {
        if (mainkanSaatStartUntukTes)
            Mainkan(null);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!_sedangMain || !bisaDiSkip || _mintaSkip) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            _mintaSkip = true;
    }

    /// <summary>Cari instance di scene walau GameObject-nya nonaktif.</summary>
    public static PhishingHackAnimation Cari()
    {
        if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<PhishingHackAnimation>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<PhishingHackAnimation>(true);
#endif
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>Mainkan cutscene. <paramref name="selesai"/> dipanggil setelah fade out.</summary>
    public void Mainkan(System.Action selesai)
    {
        if (_sedangMain)
        {
            Debug.LogWarning("[PhishingHackAnimation] Cutscene sudah jalan, request diabaikan.");
            return;
        }

        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        StartCoroutine(RutinCutscene(selesai));
    }

    // =========================================================================
    // CUTSCENE
    // =========================================================================

    private IEnumerator RutinCutscene(System.Action selesai)
    {
        _sedangMain = true;
        _mintaSkip  = false;
        _fileSampai = 0;

        BangunUI();

        // ── 1. Layar gelap + kilat merah ────────────────────────────────────
        yield return Tween(durasiGelap, t =>
        {
            _backdrop.color = WithAlpha(warnaGelap, Mathf.Lerp(0f, alphaGelapMaks, t));
            _redWash.color  = WithAlpha(warnaMerah, Mathf.Sin(t * Mathf.PI) * 0.35f);
            SetAlphaFrame(t * 0.85f);
        });
        _redWash.color = WithAlpha(warnaMerah, 0f);

        // ── 2. PC & hacker masuk dari kiri / kanan ──────────────────────────
        Vector3 pcMasuk     = _pcHome     + new Vector3(-1200f, 0f, 0f);
        Vector3 hackerMasuk = _hackerHome + new Vector3( 1200f, 0f, 0f);
        _pc.rectTransform.anchoredPosition3D     = pcMasuk;
        _hacker.rectTransform.anchoredPosition3D = hackerMasuk;
        _pc.color     = Color.white;
        _hacker.color = Color.white;

        yield return Tween(durasiMasuk, t =>
        {
            float e = EaseOutCubic(t);
            _pc.rectTransform.anchoredPosition3D     = Vector3.LerpUnclamped(pcMasuk,     _pcHome,     e);
            _hacker.rectTransform.anchoredPosition3D = Vector3.LerpUnclamped(hackerMasuk, _hackerHome, e);
        });
        _pc.rectTransform.anchoredPosition3D     = _pcHome;
        _hacker.rectTransform.anchoredPosition3D = _hackerHome;

        // ── 3. Mail terbang PC → hacker, garis kail tergambar di belakangnya ─
        Image mail = BuatTraveler(spriteMail, sizeMail, Color.white);
        yield return Tween(durasiLempar, t =>
        {
            float e = EaseInOutSine(t);
            mail.rectTransform.anchoredPosition3D = TitikBusur(e);
            mail.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e);
            mail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 12f);
            UngkapStrip(e);
        });
        UngkapStrip(1f);

        // ── 4. Kail nyangkut: mail ditelan hacker, garis nyala, layar getar ──
        yield return Tween(durasiNyangkut, t =>
        {
            mail.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0f, EaseInCubic(t));
            mail.color = WithAlpha(Color.white, 1f - t);
            float kilat = Mathf.Sin(t * Mathf.PI);
            SetWarnaStrip(Color.Lerp(warnaGaris, Color.white, kilat));
            _redWash.color = WithAlpha(warnaMerah, kilat * 0.4f);
            _hacker.rectTransform.localScale = Vector3.one * (1f + kilat * 0.12f);
        });
        if (mail != null) Destroy(mail.gameObject);
        _hacker.rectTransform.localScale = Vector3.one;
        _redWash.color = WithAlpha(warnaMerah, 0f);
        SetWarnaStrip(warnaGaris);
        Getar(0.28f, 26f);

        // ── 5. File ditarik satu per satu dari PC ke hacker ─────────────────
        _txtCounter.gameObject.SetActive(true);
        _txtCounter.text  = string.Format(formatCounter, 0, jumlahFile);
        _txtCounter.alpha = 1f;

        int total = Mathf.Max(1, jumlahFile);
        for (int i = 0; i < total; i++)
        {
            StartCoroutine(RutinCuriFile(total));
            if (i < total - 1)
                yield return TungguDetik(jedaAntarFile);
        }

        float batasTunggu = durasiPerFile + 1.5f;
        float nunggu = 0f;
        while (_fileSampai < total && nunggu < batasTunggu && !_mintaSkip)
        {
            nunggu += Time.unscaledDeltaTime;
            yield return null;
        }
        _fileSampai = total;
        _txtCounter.text = string.Format(formatCounter, total, total);

        // ── 6. Peringatan merah, lalu fade out ──────────────────────────────
        _txtWarning.gameObject.SetActive(true);
        _txtWarningSub.gameObject.SetActive(true);
        RectTransform warnRect = _txtWarning.rectTransform;

        yield return Tween(durasiOutro * 0.45f, t =>
        {
            warnRect.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, EaseOutCubic(t));
            _txtWarning.alpha    = t;
            _txtWarningSub.alpha = Mathf.Clamp01((t - 0.4f) / 0.6f);
            _redWash.color = WithAlpha(warnaMerah, Mathf.Sin(t * Mathf.PI) * 0.5f);
            SetAlphaFrame(0.85f + Mathf.Sin(t * Mathf.PI * 4f) * 0.15f);
        });
        warnRect.localScale = Vector3.one;
        _txtWarning.alpha = 1f;
        _txtWarningSub.alpha = 1f;
        _redWash.color = WithAlpha(warnaMerah, 0.12f);
        Getar(0.3f, 18f);

        yield return TungguDetik(durasiOutro * 0.55f);

        yield return Tween(durasiFadeOut, t => _group.alpha = 1f - t);

        BersihkanUI();
        _sedangMain = false;
        selesai?.Invoke();
    }

    private IEnumerator RutinCuriFile(int total)
    {
        Image folder = BuatTraveler(spriteFolder, sizeFolder, Color.white);
        float goyang = Random.Range(-1f, 1f);

        yield return Tween(durasiPerFile, t =>
        {
            if (folder == null) return;
            float e = EaseInCubic(t) * 0.35f + t * 0.65f;   // makin cepat ditarik
            Vector3 p = TitikBusur(e);
            p.y += Mathf.Sin(t * Mathf.PI * 2f) * 18f * goyang;
            folder.rectTransform.anchoredPosition3D = p;
            folder.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, e);
            folder.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 18f * goyang);
            folder.color = Color.Lerp(Color.white, warnaMerah, e * 0.6f);
        });

        if (folder != null) Destroy(folder.gameObject);

        _fileSampai++;
        if (_txtCounter != null)
            _txtCounter.text = string.Format(formatCounter, Mathf.Min(_fileSampai, total), total);

        Getar(0.18f, 14f);
        StartCoroutine(RutinKilatMerah(0.22f, 0.3f));
        StartCoroutine(RutinKedipPC(0.25f));
    }

    private IEnumerator RutinKilatMerah(float durasi, float puncak)
    {
        yield return Tween(durasi, t =>
        {
            if (_redWash != null)
                _redWash.color = WithAlpha(warnaMerah, Mathf.Sin(t * Mathf.PI) * puncak);
        });
        if (_redWash != null) _redWash.color = WithAlpha(warnaMerah, 0f);
    }

    private IEnumerator RutinKedipPC(float durasi)
    {
        yield return Tween(durasi, t =>
        {
            if (_pc == null) return;
            float k = Mathf.Sin(t * Mathf.PI * 3f);
            _pc.color = Color.Lerp(Color.white, warnaMerah, Mathf.Abs(k));
        });
        if (_pc != null) _pc.color = Color.white;
    }

    private void Getar(float durasi, float kekuatan)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(RutinGetar(durasi, kekuatan));
    }

    private IEnumerator RutinGetar(float durasi, float kekuatan)
    {
        yield return Tween(durasi, t =>
        {
            if (_stage == null) return;
            float redam = 1f - t;
            _stage.anchoredPosition = new Vector2(
                Random.Range(-kekuatan, kekuatan) * redam,
                Random.Range(-kekuatan, kekuatan) * redam);
        });
        if (_stage != null) _stage.anchoredPosition = Vector2.zero;
        _shakeRoutine = null;
    }

    // =========================================================================
    // LINTASAN (Bezier kuadratik PC → Hacker)
    // =========================================================================

    private Vector3 TitikBusur(float t)
    {
        Vector2 a = posPC     + new Vector2(sizePC.x * 0.45f, sizePC.y * 0.25f);
        Vector2 c = posHacker - new Vector2(sizeHacker.x * 0.35f, -sizeHacker.y * 0.2f);
        Vector2 b = (a + c) * 0.5f + Vector2.up * tinggiBusur;
        float u = 1f - t;
        Vector2 p = u * u * a + 2f * u * t * b + t * t * c;
        return new Vector3(p.x, p.y, 0f);
    }

    private void UngkapStrip(float progres)
    {
        for (int i = 0; i < _dashes.Count; i++)
        {
            float ambang = (i + 1f) / _dashes.Count;
            bool tampil = progres >= ambang - 0.02f;
            if (_dashes[i].enabled != tampil) _dashes[i].enabled = tampil;
        }
    }

    private void SetWarnaStrip(Color c)
    {
        for (int i = 0; i < _dashes.Count; i++) _dashes[i].color = c;
    }

    private void SetAlphaFrame(float a)
    {
        for (int i = 0; i < _frame.Count; i++)
            _frame[i].color = WithAlpha(warnaMerah, Mathf.Clamp01(a) * 0.75f);
    }

    // =========================================================================
    // BUILD UI
    // =========================================================================

    private void BangunUI()
    {
        BersihkanUI();

        GameObject go = new GameObject("PhishingHackOverlay",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        go.layer = LayerMask.NameToLayer("UI");

        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        _group = go.GetComponent<CanvasGroup>();
        _group.alpha          = 1f;
        _group.blocksRaycasts = true;   // kunci klik ke UI email di belakang
        _group.interactable   = false;

        _root = go.GetComponent<RectTransform>();

        _backdrop = BuatGambar("Backdrop", _root, WithAlpha(warnaGelap, 0f));
        Regangkan(_backdrop.rectTransform);

        _redWash = BuatGambar("RedWash", _root, WithAlpha(warnaMerah, 0f));
        Regangkan(_redWash.rectTransform);
        _redWash.raycastTarget = false;

        BuatBingkai();

        _stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
        _stage.SetParent(_root, false);
        Regangkan(_stage);

        _dashRoot = new GameObject("Garis", typeof(RectTransform)).GetComponent<RectTransform>();
        _dashRoot.SetParent(_stage, false);
        Regangkan(_dashRoot);
        BuatStrip();

        _pc = BuatGambar("PC", _stage, Color.white);
        Pasang(_pc.rectTransform, posPC, sizePC);
        _pc.sprite = spritePC != null ? spritePC : SpriteKotak();
        _pc.color  = spritePC != null ? Color.white : new Color(0.75f, 0.8f, 0.9f, 1f);

        _hacker = BuatGambar("Hacker", _stage, Color.white);
        Pasang(_hacker.rectTransform, posHacker, sizeHacker);
        _hacker.sprite = spriteHacker != null ? spriteHacker : SpriteKotak();
        _hacker.color  = spriteHacker != null ? Color.white : new Color(0.85f, 0.25f, 0.3f, 1f);

        _txtCounter = BuatTeks("Counter", _stage, 46f, warnaMerah,
            new Vector2(0f, 330f), new Vector2(900f, 70f), FontStyles.Bold);
        _txtCounter.gameObject.SetActive(false);

        _txtWarning = BuatTeks("Peringatan", _stage, 92f, warnaMerah,
            new Vector2(0f, -300f), new Vector2(1500f, 130f), FontStyles.Bold);
        _txtWarning.text = teksPeringatan;
        _txtWarning.gameObject.SetActive(false);

        _txtWarningSub = BuatTeks("PeringatanSub", _stage, 34f, new Color(0.9f, 0.9f, 0.92f, 1f),
            new Vector2(0f, -390f), new Vector2(1400f, 90f), FontStyles.Normal);
        _txtWarningSub.text = teksSubPeringatan;
        _txtWarningSub.gameObject.SetActive(false);

        _pcHome     = new Vector3(posPC.x, posPC.y, 0f);
        _hackerHome = new Vector3(posHacker.x, posHacker.y, 0f);
    }

    private void BuatBingkai()
    {
        _frame.Clear();
        // atas, bawah, kiri, kanan
        Vector2[] anchorMin = { new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
        Vector2[] anchorMax = { new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        Vector2[] ukuran    = { new Vector2(0f, 10f), new Vector2(0f, 10f), new Vector2(10f, 0f), new Vector2(10f, 0f) };

        for (int i = 0; i < 4; i++)
        {
            Image bar = BuatGambar("FrameBar" + i, _root, WithAlpha(warnaMerah, 0f));
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = anchorMin[i];
            rt.anchorMax = anchorMax[i];
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = ukuran[i];
            rt.anchoredPosition = Vector2.zero;
            bar.raycastTarget = false;
            _frame.Add(bar);
        }
    }

    private void BuatStrip()
    {
        _dashes.Clear();
        int n = Mathf.Max(4, jumlahStrip);

        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            Vector3 titik = TitikBusur(t);
            Vector3 arah  = TitikBusur(Mathf.Min(1f, t + 0.02f)) - TitikBusur(Mathf.Max(0f, t - 0.02f));

            Image dash = BuatGambar("Dash" + i, _dashRoot, warnaGaris);
            RectTransform rt = dash.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(30f, 6f);
            rt.anchoredPosition3D = titik;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(arah.y, arah.x) * Mathf.Rad2Deg);
            dash.raycastTarget = false;
            dash.enabled = false;
            _dashes.Add(dash);
        }
    }

    private Image BuatTraveler(Sprite sprite, Vector2 ukuran, Color warna)
    {
        Image img = BuatGambar("Traveler", _stage, warna);
        Pasang(img.rectTransform, Vector2.zero, ukuran);
        img.rectTransform.anchoredPosition3D = TitikBusur(0f);
        img.sprite = sprite != null ? sprite : SpriteKotak();
        if (sprite == null) img.color = new Color(0.95f, 0.85f, 0.4f, 1f);
        img.raycastTarget = false;
        return img;
    }

    private void BersihkanUI()
    {
        _dashes.Clear();
        _frame.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform anak = transform.GetChild(i);
            if (anak != null && anak.name == "PhishingHackOverlay")
            {
                if (Application.isPlaying) Destroy(anak.gameObject);
                else DestroyImmediate(anak.gameObject);
            }
        }
        _canvas = null; _group = null; _root = null; _stage = null; _dashRoot = null;
        _backdrop = null; _redWash = null; _pc = null; _hacker = null;
        _txtCounter = null; _txtWarning = null; _txtWarningSub = null;
    }

    // =========================================================================
    // HELPER UI
    // =========================================================================

    private static Image BuatGambar(string nama, Transform induk, Color warna)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(induk, false);
        Image img = go.GetComponent<Image>();
        img.color  = warna;
        img.sprite = SpriteKotak();
        return img;
    }

    private static TextMeshProUGUI BuatTeks(string nama, Transform induk, float ukuranFont,
                                            Color warna, Vector2 posisi, Vector2 ukuran, FontStyles gaya)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(induk, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize      = ukuranFont;
        tmp.color         = warna;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontStyle     = gaya;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;

        Pasang(tmp.rectTransform, posisi, ukuran);
        return tmp;
    }

    private static void Pasang(RectTransform rt, Vector2 posisi, Vector2 ukuran)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = ukuran;
        rt.anchoredPosition3D = new Vector3(posisi.x, posisi.y, 0f);
        rt.localScale = Vector3.one;
    }

    private static void Regangkan(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>Sprite kotak putih 1x1, dipakai sebagai fallback kalau sprite belum di-assign.</summary>
    private static Sprite SpriteKotak()
    {
        if (_spriteKotak == null)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            _spriteKotak = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            _spriteKotak.hideFlags = HideFlags.HideAndDontSave;
        }
        return _spriteKotak;
    }

    // =========================================================================
    // HELPER TWEEN
    // =========================================================================

    private IEnumerator Tween(float durasi, System.Action<float> langkah)
    {
        if (durasi <= 0f || _mintaSkip) { langkah(1f); yield break; }

        float t = 0f;
        while (t < 1f)
        {
            if (_mintaSkip) break;
            t += Time.unscaledDeltaTime / durasi;
            langkah(Mathf.Clamp01(t));
            yield return null;
        }
        langkah(1f);
    }

    private IEnumerator TungguDetik(float durasi)
    {
        float t = 0f;
        while (t < durasi && !_mintaSkip)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private static float EaseOutCubic(float t)  => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t)   => t * t * t;
    private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
}
