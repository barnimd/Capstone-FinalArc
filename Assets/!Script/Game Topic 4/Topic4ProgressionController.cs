using UnityEngine;

public class Topic4ProgressionController : MonoBehaviour
{
    private enum Stage
    {
        Receptionist,
        VendingConversation,
        WorkDesk,
        Computer
    }

    [Header("VN")]
    [SerializeField] private VNNPCInteractable receptionist;
    [SerializeField] private VNNPCInteractable vendingConversation;

    [Header("Guidance")]
    [SerializeField] private ObjectiveUI_Tp4 objectiveUI;
    [SerializeField] private FollowArrow_Tp1 objectiveArrow;

    [Header("Computer")]
    [SerializeField] private Topic4DeskInteractable workDesk;
    [SerializeField] private GameObject embeddedComputerRoot;
    [SerializeField] private TopDownPlayerMovement playerMovement;

    private Stage stage;

    public bool CanUseDesk => stage == Stage.WorkDesk;

    private void Awake()
    {
        Subscribe(receptionist);
        Subscribe(vendingConversation);
    }

    private void Start()
    {
        stage = Stage.Receptionist;
        SetInteractable(receptionist, true);
        SetInteractable(vendingConversation, false);

        if (embeddedComputerRoot != null)
            embeddedComputerRoot.SetActive(false);

        workDesk?.SetUnlocked(false);
        ShowObjective("Sapa resepsionis dan tanyakan cara browsing yang aman.", receptionist);
    }

    private void OnDestroy()
    {
        Unsubscribe(receptionist);
        Unsubscribe(vendingConversation);
    }

    public void BeginComputerInteraction()
    {
        if (!CanUseDesk)
            return;

        stage = Stage.Computer;
        objectiveArrow?.Hide();

        if (objectiveUI != null)
            objectiveUI.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.CanMove = false;

        if (embeddedComputerRoot != null)
            embeddedComputerRoot.SetActive(true);

        workDesk?.RefreshPrompt();
    }

    private void OnDialogueStarted(VNNPCInteractable source)
    {
        objectiveArrow?.Hide();

        if (objectiveUI != null)
            objectiveUI.gameObject.SetActive(false);
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == receptionist && stage == Stage.Receptionist)
        {
            stage = Stage.VendingConversation;
            SetInteractable(receptionist, false);
            SetInteractable(vendingConversation, true);
            ShowObjective("Pergi ke vending machine dan dengarkan percakapan dua rekan kerja.", vendingConversation);
        }
        else if (source == vendingConversation && stage == Stage.VendingConversation)
        {
            stage = Stage.WorkDesk;
            SetInteractable(vendingConversation, false);
            workDesk?.SetUnlocked(true);
            ShowObjective("Kembali ke meja dan mulai browsing dengan aman.", workDesk);
        }
    }

    private void ShowObjective(string text, Component target)
    {
        objectiveUI?.ShowObjective(text);

        if (objectiveArrow == null)
            return;

        if (target != null)
            objectiveArrow.Show(target.transform);
        else
            objectiveArrow.Hide();
    }

    private static void SetInteractable(VNNPCInteractable interactable, bool value)
    {
        if (interactable == null)
            return;

        interactable.isInteractable = value;
        interactable.RefreshPrompt();
    }

    private void Subscribe(VNNPCInteractable interactable)
    {
        if (interactable == null)
            return;

        interactable.DialogueStarted += OnDialogueStarted;
        interactable.DialogueEnded += OnDialogueEnded;
    }

    private void Unsubscribe(VNNPCInteractable interactable)
    {
        if (interactable == null)
            return;

        interactable.DialogueStarted -= OnDialogueStarted;
        interactable.DialogueEnded -= OnDialogueEnded;
    }
}
