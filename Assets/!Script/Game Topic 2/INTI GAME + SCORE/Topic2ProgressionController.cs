using UnityEngine;

/// <summary>
/// Mengunci urutan interaksi Topic 2 (Office_Environment).
///
/// Tanpa ini semua interaksi kebuka dari awal — pemain bisa langsung buka
/// desktop tanpa ngobrol sama resepsionis. Pola sama seperti
/// Topic1ProgressionController / Topic4ProgressionController.
///
/// Urutan: Resepsionis -> NPC MFA -> Komputer.
/// Teks objektif tetap diurus lewat 'Objective Text On End' di masing-masing
/// VNNPCInteractable, jadi controller ini murni gating.
/// </summary>
public class Topic2ProgressionController : MonoBehaviour
{
    private enum Stage
    {
        Receptionist,
        MfaNpc,
        Computer
    }

    [Header("Progression Interactables")]
    [Tooltip("NPC pertama — resepsionis di meja depan.")]
    public VNNPCInteractable receptionist;

    [Tooltip("NPC kedua — pekerja lain (MFA Talking 2).")]
    public VNNPCInteractable mfaNpc;

    [Tooltip("Komputer yang membuka DesktopCanvas.")]
    public InteractOpenDesktop computer;

    private Stage stage;

    private void Awake()
    {
        Subscribe(receptionist);
        Subscribe(mfaNpc);
    }

    private void Start()
    {
        stage = Stage.Receptionist;
        SetInteractable(receptionist, true);
        SetInteractable(mfaNpc, false);
        computer?.SetUnlocked(false);
    }

    private void OnDestroy()
    {
        Unsubscribe(receptionist);
        Unsubscribe(mfaNpc);
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == receptionist && stage == Stage.Receptionist)
        {
            stage = Stage.MfaNpc;
            SetInteractable(receptionist, false);
            SetInteractable(mfaNpc, true);
        }
        else if (source == mfaNpc && stage == Stage.MfaNpc)
        {
            stage = Stage.Computer;
            SetInteractable(mfaNpc, false);
            computer?.SetUnlocked(true);
        }
    }

    private static void SetInteractable(VNNPCInteractable interactable, bool enabled)
    {
        if (interactable == null)
            return;

        interactable.isInteractable = enabled;
        interactable.RefreshPrompt();
    }

    private void Subscribe(VNNPCInteractable interactable)
    {
        if (interactable == null)
            return;

        interactable.DialogueEnded += OnDialogueEnded;
    }

    private void Unsubscribe(VNNPCInteractable interactable)
    {
        if (interactable == null)
            return;

        interactable.DialogueEnded -= OnDialogueEnded;
    }
}
