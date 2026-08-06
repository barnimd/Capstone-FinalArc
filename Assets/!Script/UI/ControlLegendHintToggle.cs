using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tombol HINT buat munculin kartu Control Legend sebentar, terus ilang sendiri.
///
/// Kenapa dipisah dari <see cref="ControlLegendUI"/>:
///   ControlLegendUI ngurus "boleh keliatan gak sih sekarang" (lagi pause? ada panel
///   nutupin?). Script ini ngurus "player lagi mau lihat gak". Dua urusan beda,
///   jadi dua objek beda juga:
///
///     ControlLegendCanvas   [ControlLegendUI + script ini]
///     └── Visible                 ← di-toggle ControlLegendUI (pause / blocker)
///         ├── HintButton          ← selalu nongol selama Visible aktif
///         └── Legend              ← di-toggle script ini (timer 4 detik)
///
/// Tombolnya HARUS di luar <see cref="legendPanel"/>. Kalau tombolnya ikut dimatiin
/// pas kartu ditutup, gak ada lagi yang bisa dipencet buat munculin balik.
/// </summary>
public class ControlLegendHintToggle : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kartu legend-nya. JANGAN diisi objek yang punya tombol HINT di dalamnya.")]
    [SerializeField] private GameObject legendPanel;
    [SerializeField] private Button hintButton;

    [Header("Perilaku")]
    [Tooltip("Detik sebelum kartu ilang sendiri. Isi 0 kalau mau nutup manual terus.")]
    [SerializeField] private float autoHideSeconds = 4f;
    [Tooltip("Kartu langsung keliatan pas scene mulai?")]
    [SerializeField] private bool visibleOnStart = false;
    [Tooltip("Opsional. Tombol keyboard buat toggle, selain klik tombol HINT.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.None;

    private float _timeLeft;

    public bool IsShown => legendPanel != null && legendPanel.activeSelf;

    private void Awake()
    {
        if (hintButton != null) hintButton.onClick.AddListener(Toggle);
    }

    private void Start()
    {
        Apply(visibleOnStart);
    }

    private void OnDestroy()
    {
        if (hintButton != null) hintButton.onClick.RemoveListener(Toggle);
    }

    private void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey)) Toggle();

        if (!IsShown || autoHideSeconds <= 0f) return;

        // unscaledDeltaTime: timer tetap jalan walau game lagi pause.
        _timeLeft -= Time.unscaledDeltaTime;
        if (_timeLeft <= 0f) Apply(false);
    }

    /// <summary>Pencet HINT: lagi kebuka = tutup, lagi ketutup = buka + reset timer.</summary>
    public void Toggle() => Apply(!IsShown);

    public void Show() => Apply(true);
    public void Hide() => Apply(false);

    private void Apply(bool show)
    {
        if (legendPanel == null)
        {
            Debug.LogError("[ControlLegendHintToggle] legendPanel belum di-assign.", this);
            return;
        }

        legendPanel.SetActive(show);
        _timeLeft = show ? autoHideSeconds : 0f;
    }
}
