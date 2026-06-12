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
    [SerializeField] private FollowArrow_Tp1 objectiveArrow;

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

        ShowObjective("Sapa rekan kerja di pagi hari.", morningGreetingNpc);
    }

    private void OnDestroy()
    {
        Unsubscribe(morningGreetingNpc);
        Unsubscribe(backupStoryNpc);
    }

    public void BeginComputerInteraction()
    {
        objectiveArrow?.Hide();

        if (objectiveUI != null)
            objectiveUI.gameObject.SetActive(false);
    }

    private void OnDialogueStarted(VNNPCInteractable source)
    {
        objectiveArrow?.Hide();
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == morningGreetingNpc && stage == Stage.MorningGreeting)
        {
            stage = Stage.BackupStory;
            SetInteractable(morningGreetingNpc, false);
            SetInteractable(backupStoryNpc, true);
            ShowObjective("Dengarkan cerita rekan kerja tentang backup data.", backupStoryNpc);
        }
        else if (source == backupStoryNpc && stage == Stage.BackupStory)
        {
            stage = Stage.WorkDesk;
            SetInteractable(backupStoryNpc, false);

            if (workComputer != null)
                workComputer.SetUnlocked(true);

            ShowObjective("Pergi ke meja kerja dan mulai mengelola file.", workComputer);
        }
    }

    private void ShowObjective(string text, Component target)
    {
        if (objectiveUI != null)
        {
            objectiveUI.gameObject.SetActive(true);
            objectiveUI.ShowObjective(text);
        }

        if (objectiveArrow == null)
            return;

        if (target != null)
            objectiveArrow.Show(target.transform);
        else
            objectiveArrow.Hide();
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
        {
            interactable.DialogueStarted += OnDialogueStarted;
            interactable.DialogueEnded += OnDialogueEnded;
        }
    }

    private void Unsubscribe(VNNPCInteractable interactable)
    {
        if (interactable != null)
        {
            interactable.DialogueStarted -= OnDialogueStarted;
            interactable.DialogueEnded -= OnDialogueEnded;
        }
    }
}
