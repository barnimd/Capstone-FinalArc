using UnityEngine;

/// <summary>
/// Legend kontrol di bagian bawah layar (WASD / Shift / Q / E / Ctrl).
///
/// Visual-nya dibangun sekali lewat menu "Tools > SecMind > Rebuild Control Legend",
/// terus disimpan jadi prefab. Script ini cuma ngurus kapan legend-nya kelihatan.
///
/// Cara pakai di topic lain:
///   1. Drag prefab ControlLegend ke scene.
///   2. Isi <see cref="hideWhenAnyActive"/> dengan panel yang nutupin area bawah layar
///      (VN_Canvas, panel pause, canvas desktop, dll).
/// </summary>
public class ControlLegendUI : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Objek yang di-show/hide. HARUS anak dari GameObject ini, jangan objek ini sendiri — " +
             "kalau objeknya sendiri yang dimatiin, script ini ikut mati dan legend gak bisa balik. " +
             "Kalau kosong, otomatis pakai anak pertama.")]
    [SerializeField] private GameObject legendRoot;

    [Header("Auto Hide")]
    [Tooltip("Legend disembunyiin kalau salah satu objek ini lagi aktif. " +
             "Contoh: VN_Canvas, panel pause, canvas email.")]
    [SerializeField] private GameObject[] hideWhenAnyActive;

    [Tooltip("Sembunyiin juga pas game ke-pause (Time.timeScale = 0).")]
    [SerializeField] private bool hideWhenPaused = true;

    /// <summary>Matiin legend dari script lain, mis. pas cutscene.</summary>
    public bool ForceHidden { get; set; }

    private void Awake()
    {
        if (legendRoot == null && transform.childCount > 0)
            legendRoot = transform.GetChild(0).gameObject;

        // Kalau root-nya objek ini sendiri, matiin dia = matiin script ini juga.
        // Legend bakal nyangkut ilang selamanya, jadi lebih baik nonaktifin fiturnya.
        if (legendRoot == gameObject)
        {
            Debug.LogError("[ControlLegendUI] legendRoot tidak boleh GameObject ini sendiri. " +
                           "Pakai child (kartu legend-nya).", this);
            legendRoot = null;
        }
    }

    private void LateUpdate()
    {
        if (legendRoot == null) return;

        bool visible = !ForceHidden && !AnyBlockerActive();
        if (hideWhenPaused && Time.timeScale == 0f) visible = false;

        if (legendRoot.activeSelf != visible)
            legendRoot.SetActive(visible);
    }

    private bool AnyBlockerActive()
    {
        if (hideWhenAnyActive == null) return false;

        for (int i = 0; i < hideWhenAnyActive.Length; i++)
        {
            var go = hideWhenAnyActive[i];
            if (go != null && go.activeInHierarchy) return true;
        }
        return false;
    }
}
