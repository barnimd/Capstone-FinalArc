using UnityEngine;

/// <summary>
/// Jembatan antara sistem prompt lama (SetActive pada GameObject anak NPC)
/// dan HUD terpusat <see cref="InteractionPromptUI"/> di bawah-tengah layar.
///
/// Pasang di GameObject prompt yang sudah ada (InteractPrompt / Prompt / InteractText),
/// tanpa mengubah nama atau posisinya di hierarchy.
/// Objeknya tidak lagi punya visual apa pun. Begitu di-SetActive(true) oleh script
/// interactable, dia menyalakan HUD; saat SetActive(false) dia mematikannya.
/// Dengan begitu semua serialized reference di NPC/desk tetap utuh.
/// </summary>
public class HudPromptProxy : MonoBehaviour
{
    [Header("Pesan Prompt")]
    [Tooltip("Teks yang tampil di HUD saat prompt ini aktif.")]
    [SerializeField] private string message = "Tekan E untuk berinteraksi";

    private static HudPromptProxy current;
    private static InteractionPromptUI ui;

    public string Message
    {
        get => message;
        set => message = value;
    }

    private void OnEnable()
    {
        if (ui == null)
            ui = FindObjectOfType<InteractionPromptUI>(true);

        if (ui == null)
        {
            Debug.LogWarning($"[HudPromptProxy] InteractionPromptUI tidak ditemukan di scene. Prompt '{message}' tidak tampil.", this);
            return;
        }

        current = this;
        ui.ShowPrompt(message);
    }

    private void OnDisable()
    {
        // Prompt lain sudah mengambil alih HUD -> jangan ikut mematikan.
        if (current != this) return;

        current = null;
        if (ui != null)
            ui.HidePrompt();
    }
}
