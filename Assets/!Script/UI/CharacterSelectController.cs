using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Pilih Karakter Mu" — the one-time character pick, shown after auth and before
/// All_Menu.
///
/// Lives in its own scene rather than as a canvas inside the auth screens because
/// three entry points need it (guest callsign, email login, signup). As a canvas
/// it would have to be duplicated and kept in sync in all three.
///
/// The pick is PERMANENT. The server enforces that too — see the gender_chosen
/// handling in Docs/api/user/sync.js — because a client can post that endpoint
/// directly. The warning label here exists precisely because there is no undo.
///
/// Everything visual is assigned in the Inspector. Nothing is loaded by path, so
/// renaming or replacing art never breaks this script.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    [Header("Karakter — Male (BIMA)")]
    [SerializeField] private Image       malePortrait;
    [SerializeField] private Sprite      maleSprite;
    [SerializeField] private CanvasGroup maleGroup;      // dipakai buat redupin yang tidak dipilih
    [SerializeField] private GameObject  maleGlow;
    [SerializeField] private GameObject  maleBadge;      // kartu "BIMA / OPERATIVE 01"
    [SerializeField] private Button      maleClickArea;  // opsional: klik langsung di karakternya

    [Header("Karakter — Female (AYU)")]
    [SerializeField] private Image       femalePortrait;
    [SerializeField] private Sprite      femaleSprite;
    [SerializeField] private CanvasGroup femaleGroup;
    [SerializeField] private GameObject  femaleGlow;
    [SerializeField] private GameObject  femaleBadge;
    [SerializeField] private Button      femaleClickArea;

    [Header("Navigasi")]
    [SerializeField] private Button arrowLeft;
    [SerializeField] private Button arrowRight;

    [Header("Konfirmasi")]
    [SerializeField] private Button           confirmButton;
    [SerializeField] private Image            confirmBackground;
    [SerializeField] private TextMeshProUGUI  confirmLabel;
    [SerializeField] private TextMeshProUGUI  warningText;
    [SerializeField] private TextMeshProUGUI  errorText;
    [SerializeField] private GameObject       loadingSpinner;

    [Header("Warna aksen")]
    [SerializeField] private Color maleAccent   = new Color(0.95f, 0.55f, 0.15f); // oranye
    [SerializeField] private Color femaleAccent = new Color(0.93f, 0.29f, 0.60f); // pink

    [Header("Redup / terang")]
    [Range(0f, 1f)]
    [SerializeField] private float dimmedAlpha   = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float selectedAlpha = 1f;

    private PlayerGender _selected = PlayerGender.Male;
    private bool _busy;

    void Start()
    {
        // Sprites are optional so the scene can be laid out before the art exists.
        // The setup script leaves the Image transparent so an unassigned portrait
        // is invisible instead of a white slab — which means assigning a sprite has
        // to restore the alpha, or the art would never show up.
        ApplyPortrait(malePortrait,   maleSprite);
        ApplyPortrait(femalePortrait, femaleSprite);

        if (arrowLeft       != null) arrowLeft.onClick.AddListener(SelectPrevious);
        if (arrowRight      != null) arrowRight.onClick.AddListener(SelectNext);
        if (maleClickArea   != null) maleClickArea.onClick.AddListener(() => Select(PlayerGender.Male));
        if (femaleClickArea != null) femaleClickArea.onClick.AddListener(() => Select(PlayerGender.Female));
        if (confirmButton   != null) confirmButton.onClick.AddListener(OnConfirmClicked);

        if (warningText != null)
            warningText.text = "Pilihan ini tidak bisa diubah lagi nanti.";

        HideError();
        SetLoading(false);

        // Start on whatever the player already has. Someone who somehow lands here
        // twice sees their own character highlighted, not a reset to BIMA.
        Select(PlayerProfile.Gender);
    }

    private static void ApplyPortrait(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        target.sprite = sprite;

        Color c = target.color;
        target.color = new Color(c.r, c.g, c.b, 1f);
    }

    // ─── Selection ────────────────────────────────────────────────────────────

    private void SelectPrevious() => Select(PlayerGender.Male);
    private void SelectNext()     => Select(PlayerGender.Female);

    private void Select(PlayerGender gender)
    {
        if (_busy) return;

        _selected = gender;
        bool isMale = gender == PlayerGender.Male;

        ApplySide(maleGroup,   maleGlow,   maleBadge,   isMale);
        ApplySide(femaleGroup, femaleGlow, femaleBadge, !isMale);

        Color accent = isMale ? maleAccent : femaleAccent;
        if (confirmBackground != null) confirmBackground.color = accent;
        if (arrowLeft  != null && arrowLeft.image  != null) arrowLeft.image.color  = accent;
        if (arrowRight != null && arrowRight.image != null) arrowRight.image.color = accent;

        HideError();
    }

    private void ApplySide(CanvasGroup group, GameObject glow, GameObject badge, bool active)
    {
        if (group != null) group.alpha = active ? selectedAlpha : dimmedAlpha;
        if (glow  != null) glow.SetActive(active);
        if (badge != null) badge.SetActive(active);
    }

    // ─── Confirm ──────────────────────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        if (_busy) return;

        HideError();
        SetLoading(true);

        PlayerProfile.SetGender(_selected, ok =>
        {
            SetLoading(false);

            if (!ok)
            {
                // The local cache is already written, so the game would run with the
                // right character — but only on this device. Since the choice is
                // permanent, letting it through silently would strand the player with
                // a different character on their next browser. Make them retry.
                ShowError("Gagal menyimpan pilihan. Cek koneksi lalu coba lagi.");
                return;
            }

            if (AuthUIManager.Instance != null)
                AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GAME);
            else
                Debug.LogWarning("[CharacterSelect] AuthUIManager.Instance is null — cannot leave scene");
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetLoading(bool isLoading)
    {
        _busy = isLoading;

        if (confirmButton  != null) confirmButton.interactable = !isLoading;
        if (arrowLeft      != null) arrowLeft.interactable     = !isLoading;
        if (arrowRight     != null) arrowRight.interactable    = !isLoading;
        if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);

        if (confirmLabel != null)
            confirmLabel.text = isLoading ? "Menyimpan…" : "Pilih Karakter";
    }

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (errorText != null) errorText.gameObject.SetActive(false);
    }
}
