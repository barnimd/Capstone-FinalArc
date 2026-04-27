using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach ke ObjectivePanel.
/// Panel ini dikontrol oleh EmailRandomizer — tidak perlu aktif di awal.
/// </summary>
public class ObjectivePanel : MonoBehaviour
{
    [Header("=== UI ===")]
    public Button btnTutup;
    public TMP_Text txtObjective;

    [Header("=== Isi Teks Objective ===")]
    [TextArea(4, 10)]
    public string pesanObjective =
        "Telusuri email dan tentukan decision yang benar\n\n" +
        "1. Balas jika email aman\n" +
        "2. Hapus jika email tidak aman\n" +
        "3. Laporkan jika email mencurigakan";

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Wire tombol X saat Awake agar siap meski panel tidak aktif di awal
        if (btnTutup != null)
            btnTutup.onClick.AddListener(TutupPanel);

        if (txtObjective != null)
            txtObjective.text = pesanObjective;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Dipanggil oleh EmailRandomizer untuk tampilkan panel.</summary>
    public void TampilkanJikaPerlu()
    {
        // Gunakan static bool agar cukup sekali per session
        if (!ObjectiveState.SudahTampil)
        {
            ObjectiveState.SudahTampil = true;
            gameObject.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void TutupPanel()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    [ContextMenu("Reset Objective (untuk testing)")]
    public void ResetObjective()
    {
        ObjectiveState.SudahTampil = false;
        Debug.Log("[ObjectivePanel] Reset selesai. Panel akan muncul lagi saat Play.");
    }
}

/// <summary>
/// Static class untuk menyimpan state objective selama session berjalan.
/// </summary>
public static class ObjectiveState
{
    public static bool SudahTampil = false;
}
