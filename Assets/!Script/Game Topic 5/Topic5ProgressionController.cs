using System.Collections;
using UnityEngine;

public class Topic5ProgressionController : MonoBehaviour
{
    private enum Stage
    {
        Opening,
        Sari,
        Rina,
        PakHendra,
        Investigation,
        KaraokeNarration,
        MasAnto,
        Choice,
        Resolution,
        Laptop,
        Computer
    }

    [Header("VN")]
    [SerializeField] private VNDialogueManager dialogueManager;
    [SerializeField] private VNDialogueData preDesktopDialogue;
    [SerializeField] private VNDialogueData choiceADialogue;
    [SerializeField] private VNDialogueData choiceBDialogue;
    [SerializeField] private VNDialogueData choiceCDialogue;

    [Header("Progression Interactables")]
    [SerializeField] private VNNPCInteractable openingNarration;
    [SerializeField] private VNNPCInteractable sari;
    [SerializeField] private VNNPCInteractable rina;
    [SerializeField] private VNNPCInteractable pakHendra;
    [SerializeField] private VNNPCInteractable budi;
    [SerializeField] private VNNPCInteractable buDewi;
    [SerializeField] private VNNPCInteractable karaokeNarration;
    [SerializeField] private VNNPCInteractable masAnto;

    [Header("UI and Laptop")]
    [SerializeField] private Topic5ObjectivePanel objectivePanel;
    [SerializeField] private FollowArrow_Tp1 objectiveArrow;
    [SerializeField] private Topic5ThreeWayChoiceController choiceController;
    [SerializeField] private Topic5LaptopInteractable laptop;
    [SerializeField] private GameObject embeddedComputerRoot;
    [SerializeField] private TopDownPlayerMovement playerMovement;

    private Stage stage;
    private bool buDewiCompleted;
    private bool laptopOpening;

    public bool CanUseLaptop => stage == Stage.Laptop && !laptopOpening;

    private void Awake()
    {
        Subscribe(openingNarration);
        Subscribe(sari);
        Subscribe(rina);
        Subscribe(pakHendra);
        Subscribe(budi);
        Subscribe(buDewi);
        Subscribe(karaokeNarration);
        Subscribe(masAnto);
    }

    private void Start()
    {
        SetInteractable(sari, false);
        SetInteractable(rina, false);
        SetInteractable(pakHendra, false);
        SetInteractable(budi, false);
        SetInteractable(buDewi, false);
        SetInteractable(karaokeNarration, false);
        SetInteractable(masAnto, false);

        if (embeddedComputerRoot != null)
            embeddedComputerRoot.SetActive(false);

        stage = Stage.Opening;
        ShowObjective("Tanya nama Wi-Fi ke barista");
        objectiveArrow?.Hide();
    }

    private void OnDestroy()
    {
        Unsubscribe(openingNarration);
        Unsubscribe(sari);
        Unsubscribe(rina);
        Unsubscribe(pakHendra);
        Unsubscribe(budi);
        Unsubscribe(buDewi);
        Unsubscribe(karaokeNarration);
        Unsubscribe(masAnto);
    }

    public void BeginLaptopInteraction()
    {
        if (!CanUseLaptop)
            return;

        laptopOpening = true;
        StartCoroutine(OpenEmbeddedComputer());
    }

    private IEnumerator OpenEmbeddedComputer()
    {
        if (preDesktopDialogue != null && dialogueManager != null)
        {
            dialogueManager.SetActiveInteractable(null);
            dialogueManager.StartDialogue(preDesktopDialogue);

            while (dialogueManager.vnRoot != null && !dialogueManager.vnRoot.activeInHierarchy)
                yield return null;
            while (dialogueManager.vnRoot != null && dialogueManager.vnRoot.activeInHierarchy)
                yield return null;
        }

        stage = Stage.Computer;
        objectiveArrow?.Hide();
        if (playerMovement != null)
            playerMovement.CanMove = false;
        if (embeddedComputerRoot != null)
            embeddedComputerRoot.SetActive(true);
        if (objectivePanel != null)
            objectivePanel.gameObject.SetActive(false);
        laptop?.RefreshPrompt();
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == openingNarration && stage == Stage.Opening)
        {
            stage = Stage.Sari;
            SetInteractable(sari, true);
            ShowObjective("Tanya nama Wi-Fi ke barista", sari);
        }
        else if (source == sari && stage == Stage.Sari)
        {
            stage = Stage.Rina;
            SetInteractable(sari, false);
            SetInteractable(rina, true);
            ShowObjective("Cari tempat duduk di area seating", rina);
        }
        else if (source == rina && stage == Stage.Rina)
        {
            stage = Stage.PakHendra;
            SetInteractable(rina, false);
            SetInteractable(pakHendra, true);
            ShowObjective("Tanya staff soal Wi-Fi mencurigakan", pakHendra);
        }
        else if (source == pakHendra && stage == Stage.PakHendra)
        {
            stage = Stage.Investigation;
            SetInteractable(pakHendra, false);
            SetInteractable(budi, true);
            SetInteractable(buDewi, true);
            ShowObjective("Selidiki setiap ruangan dan cari sumber Wi-Fi palsu", budi);
        }
        else if (source == buDewi && !buDewiCompleted
                 && stage >= Stage.Investigation && stage < Stage.Laptop)
        {
            buDewiCompleted = true;
            SetInteractable(buDewi, false);
            if (ScoreManager.instance != null)
                ScoreManager.instance.AddScore(5);
        }
        else if (source == budi && stage == Stage.Investigation)
        {
            stage = Stage.KaraokeNarration;
            SetInteractable(budi, false);
            SetInteractable(karaokeNarration, true);
            ShowObjective("Periksa ruang karaoke", karaokeNarration);
        }
        else if (source == karaokeNarration && stage == Stage.KaraokeNarration)
        {
            stage = Stage.MasAnto;
            SetInteractable(karaokeNarration, false);
            SetInteractable(masAnto, true);
            ShowObjective("Hadapi sumber Wi-Fi palsu", masAnto);
        }
        else if (source == masAnto && stage == Stage.MasAnto)
        {
            stage = Stage.Choice;
            SetInteractable(masAnto, false);
            objectiveArrow?.Hide();
            if (playerMovement != null)
                playerMovement.CanMove = false;
            choiceController.Show(ResolveMasAntoChoice);
        }
        else if (source == masAnto && stage == Stage.Resolution)
        {
            stage = Stage.Laptop;
            SetInteractable(buDewi, false);
            if (playerMovement != null)
                playerMovement.CanMove = true;
            ShowObjective("Kembali ke meja dan sambungkan laptop dengan aman", laptop);
            laptop?.RefreshPrompt();
        }
    }

    private void ResolveMasAntoChoice(int choice)
    {
        stage = Stage.Resolution;

        VNDialogueData resolution = choice == 0
            ? choiceADialogue
            : choice == 1 ? choiceBDialogue : choiceCDialogue;

        if (ScoreManager.instance != null)
        {
            if (choice == 0)
                ScoreManager.instance.AddScore(10);
            else if (choice == 1)
                ScoreManager.instance.AddScore(5);
        }

        dialogueManager.SetActiveInteractable(masAnto);
        dialogueManager.StartDialogue(resolution);
    }

    private void ShowObjective(string text)
    {
        objectivePanel?.ShowObjective(text);
    }

    private void ShowObjective(string text, Component target)
    {
        ShowObjective(text);

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
            interactable.DialogueEnded += OnDialogueEnded;
    }

    private void Unsubscribe(VNNPCInteractable interactable)
    {
        if (interactable != null)
            interactable.DialogueEnded -= OnDialogueEnded;
    }
}
