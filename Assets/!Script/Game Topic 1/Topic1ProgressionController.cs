using UnityEngine;

public class Topic1ProgressionController : MonoBehaviour
{
    private enum Stage
    {
        Receptionist,
        Pantry,
        Lift,
        RizalChecklist,
        PakBudi,
        PakBudiChecklist,
        PakBudiInstaller,
        Computer,
        ComputerChecklist,
        Complete
    }

    [Header("Progression Interactables")]
    public VNNPCInteractable receptionist;
    public VNNPCInteractable pantry;
    public VNNPCInteractable liftTrigger;
    public Transform liftArrowTarget;
    public RoomTransitionTrigger pantryToCubicleTransition;
    public VNNPCInteractable pakBudi;
    public InteractOpenDesktop computer;
    public GameObject legacyAskingFile;

    [Header("UI and Flow")]
    public ObjectiveUI_Tp1 objectivePanel;
    public FollowArrow_Tp1 objectiveArrow;
    public Topic1EvidenceChecklistController evidenceChecklist;
    public GameFlowManager computerFlow;
    public InstallerFlow installerFlow;
    public GameplayManager gameplayManager;
    public CrashOverlayController_Tp1_Installer installerCrash;

    [Header("Evidence Cases")]
    public Topic1EvidenceCase rizalCase;
    public Topic1EvidenceCase pakBudiCase;
    public Topic1EvidenceCase computerCase;

    private Stage stage;

    private void Awake()
    {
        Subscribe(receptionist);
        Subscribe(pantry);
        Subscribe(liftTrigger);
        Subscribe(pakBudi);

        if (computer != null)
        {
            computer.DesktopOpened += OnDesktopOpened;
            computer.DesktopClosed += OnDesktopClosed;
        }

        if (computerFlow != null)
            computerFlow.DecisionCompleted += OnComputerDecision;

        if (installerFlow != null)
            installerFlow.InstallerCompleted += OnInstallerCompleted;
    }

    private void Start()
    {
        if (legacyAskingFile != null)
            legacyAskingFile.SetActive(false);

        if (pantryToCubicleTransition != null)
            pantryToCubicleTransition.enabled = false;

        if (objectiveArrow != null)
            objectiveArrow.displayMode = FollowArrow_Tp1.ArrowDisplayMode.ScreenEdge;

        SetStage(Stage.Receptionist);
    }

    private void OnDestroy()
    {
        Unsubscribe(receptionist);
        Unsubscribe(pantry);
        Unsubscribe(liftTrigger);
        Unsubscribe(pakBudi);

        if (computer != null)
        {
            computer.DesktopOpened -= OnDesktopOpened;
            computer.DesktopClosed -= OnDesktopClosed;
        }

        if (computerFlow != null)
            computerFlow.DecisionCompleted -= OnComputerDecision;

        if (installerFlow != null)
            installerFlow.InstallerCompleted -= OnInstallerCompleted;
    }

    private void OnDialogueStarted(VNNPCInteractable source)
    {
        objectiveArrow?.Hide();
        objectivePanel?.HideImmediate();
    }

    private void OnDialogueEnded(VNNPCInteractable source)
    {
        if (source == receptionist && stage == Stage.Receptionist)
            SetStage(Stage.Pantry);
        else if (source == pantry && stage == Stage.Pantry)
            SetStage(Stage.Lift);
        else if (source == liftTrigger && stage == Stage.Lift)
        {
            if (pantryToCubicleTransition != null)
                pantryToCubicleTransition.enabled = true;
            OpenDecisionChecklist(Stage.RizalChecklist, rizalCase, Stage.PakBudi);
        }
        else if (source == pakBudi && stage == Stage.PakBudi)
            OpenDecisionChecklist(Stage.PakBudiChecklist, pakBudiCase, Stage.Computer);
    }

    private void OpenDecisionChecklist(Stage checklistStage, Topic1EvidenceCase evidenceCase, Stage nextStage)
    {
        stage = checklistStage;
        SetAllInteractables(false);
        objectiveArrow?.Hide();
        objectivePanel?.HideImmediate();

        if (evidenceChecklist == null || evidenceCase == null)
        {
            SetStage(nextStage);
            return;
        }

        evidenceChecklist.ShowDecision(evidenceCase, safe =>
        {
            if (!safe && evidenceCase != pakBudiCase && ScoreManager.instance != null)
                ScoreManager.instance.AddScore(evidenceCase.unsafePenalty);

            if (!safe && evidenceCase == pakBudiCase && installerFlow != null)
            {
                stage = Stage.PakBudiInstaller;
                objectiveArrow?.Hide();
                objectivePanel?.HideImmediate();
                installerFlow.BeginInstaller();
                return;
            }

            SetStage(nextStage);
        });
    }

    private void OnInstallerCompleted(bool installed)
    {
        if (stage != Stage.PakBudiInstaller)
            return;

        SetStage(Stage.Computer);
    }

    private void OnDesktopOpened()
    {
        if (stage != Stage.Computer)
            return;

        objectiveArrow?.Hide();
        objectivePanel?.HideImmediate();
    }

    private void OnDesktopClosed()
    {
        if (stage == Stage.Computer && (computerFlow == null || !computerFlow.HasCompletedDecision))
            ShowObjective("Periksa permintaan file pada komputer", computer.transform);
    }

    private void OnComputerDecision(bool safe)
    {
        if (stage != Stage.Computer)
            return;

        stage = Stage.ComputerChecklist;
        computer?.CloseDesktop();
        computer?.SetUnlocked(false);
        objectiveArrow?.Hide();
        objectivePanel?.HideImmediate();

        if (evidenceChecklist != null && computerCase != null)
            evidenceChecklist.ShowReview(computerCase, safe, _ => CompleteLevel());
        else
            CompleteLevel();
    }

    private void CompleteLevel()
    {
        stage = Stage.Complete;
        SetAllInteractables(false);
        computer?.SetUnlocked(false);
        objectiveArrow?.Hide();
        objectivePanel?.HideImmediate();
        gameplayManager?.FinishGame();
    }

    private void SetStage(Stage nextStage)
    {
        stage = nextStage;
        SetAllInteractables(false);
        computer?.SetUnlocked(false);

        switch (stage)
        {
            case Stage.Receptionist:
                EnableAndPoint(receptionist, "Bicara dengan resepsionis");
                break;
            case Stage.Pantry:
                EnableAndPoint(pantry, "Temui Rio dan Bagas di pantry");
                break;
            case Stage.Lift:
                SetInteractable(liftTrigger, true);
                ShowObjective(
                    "Pergi ke lift dan periksa permintaan mencurigakan",
                    liftArrowTarget != null ? liftArrowTarget : liftTrigger.transform);
                break;
            case Stage.PakBudi:
                EnableAndPoint(pakBudi, "Temui Pak Budi di ruang cubicle");
                break;
            case Stage.Computer:
                computer?.SetUnlocked(true);
                ShowObjective("Periksa permintaan file pada komputer", computer != null ? computer.transform : null);
                break;
        }
    }

    private void EnableAndPoint(VNNPCInteractable interactable, string objective)
    {
        SetInteractable(interactable, true);
        ShowObjective(objective, interactable != null ? interactable.transform : null);
    }

    private void ShowObjective(string text, Transform target)
    {
        objectivePanel?.ShowObjective(text);
        if (target != null)
            objectiveArrow?.Show(target);
        else
            objectiveArrow?.Hide();
    }

    private void SetAllInteractables(bool enabled)
    {
        SetInteractable(receptionist, enabled);
        SetInteractable(pantry, enabled);
        SetInteractable(liftTrigger, enabled);
        SetInteractable(pakBudi, enabled);
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
