using UnityEngine;

public class Topic6ProgressionController : MonoBehaviour
{
    private enum Stage
    {
        MorningGreeting,
        BackupStory,
        WorkDesk
    }

    [Header("VN Interactables")]
    [SerializeField] private VNNPCInteractable morningGreetingNpc;
    [SerializeField] private VNNPCInteractable backupStoryNpc;

    [Header("Computer")]
    [SerializeField] private Topic6ComputerInteractable workComputer;
    [SerializeField] private InteractableTrigger legacyComputerTrigger;

    [Header("UI")]
    [SerializeField] private ObjectiveUI_Tp4 objectiveUI;

    private Stage stage;

    private void Awake()
    {
        Subscribe(morningGreetingNpc);
        Subscribe(backupStoryNpc);
    }

    private void Start()
    {
        stage = Stage.MorningGreeting;

        SetInteractable(morningGreetingNpc, true);
        SetInteractable(backupStoryNpc, false);

        if (legacyComputerTrigger != null)
            legacyComputerTrigger.enabled = false;

        if (workComputer != null)
            workComputer.SetUnlocked(false);

        ShowObjective("Sapa rekan kerja di pagi hari.");
    }

    private void OnDestroy()
    {
        Unsubscribe(morningGreetingNpc);
        Unsubscribe(backupStoryNpc);
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == morningGreetingNpc && stage == Stage.MorningGreeting)
        {
            stage = Stage.BackupStory;
            SetInteractable(morningGreetingNpc, false);
            SetInteractable(backupStoryNpc, true);
            ShowObjective("Dengarkan cerita rekan kerja tentang backup data.");
        }
        else if (source == backupStoryNpc && stage == Stage.BackupStory)
        {
            stage = Stage.WorkDesk;
            SetInteractable(backupStoryNpc, false);

            if (workComputer != null)
                workComputer.SetUnlocked(true);

            ShowObjective("Pergi ke meja kerja dan mulai mengelola file.");
        }
    }

    private void ShowObjective(string text)
    {
        if (objectiveUI != null)
            objectiveUI.ShowObjective(text);
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
        if (interactable != null)
            interactable.DialogueEnded += OnDialogueEnded;
    }

    private void Unsubscribe(VNNPCInteractable interactable)
    {
        if (interactable != null)
            interactable.DialogueEnded -= OnDialogueEnded;
    }
}
