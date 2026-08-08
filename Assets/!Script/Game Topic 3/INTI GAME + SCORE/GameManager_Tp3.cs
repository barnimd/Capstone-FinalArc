using UnityEngine;

public class GameManager_Tp3 : MonoBehaviour
{
    public static GameManager_Tp3 instance;

    [Header("UI")]
    public GameObject canvasInstructor;
    public ObjectiveUI_Tp3 objectiveUI;
    public FollowArrow_Tp1 objectiveArrow;

    [Header("Progression Targets")]
    public VNNPCInteractable arunika;
    public Transform arunikaTarget;
    public ComputerInteraction officeComputer;
    public Transform computerTarget;

    private int currentStep;

    private void Awake()
    {
        instance = this;

        if (arunika != null)
        {
            arunika.DialogueStarted += OnVNDialogueStarted;
            arunika.DialogueEnded += OnVNDialogueEnded;
        }

        if (officeComputer != null)
            officeComputer.DesktopOpening += OnDesktopOpening;
    }

    private void Start()
    {
        if (objectiveArrow != null)
            objectiveArrow.displayMode = FollowArrow_Tp1.ArrowDisplayMode.ScreenEdge;

        StartGame();
    }

    private void OnDestroy()
    {
        if (arunika != null)
        {
            arunika.DialogueStarted -= OnVNDialogueStarted;
            arunika.DialogueEnded -= OnVNDialogueEnded;
        }

        if (officeComputer != null)
            officeComputer.DesktopOpening -= OnDesktopOpening;
    }

    private void StartGame()
    {
        currentStep = 0;
        ShowObjective("Sapa orang di Meja Kerja", arunikaTarget);
    }

    public void CloseInstructor()
    {
        canvasInstructor.SetActive(false);
    }

    public void OnPlayerApproachNPC()
    {
        if (currentStep != 0)
            return;

        currentStep = 1;
        ShowObjective("Bicara dengan Arunika...", arunikaTarget);
    }

    public void OnDialogFinished()
    {
        NextObjective();
    }

    public void SetObjectiveFromDialog(string objectiveText)
    {
        ShowObjective(objectiveText, computerTarget);
    }

    public void NextObjective()
    {
        currentStep++;

        switch (currentStep)
        {
            case 2:
                ShowObjective("Pergi ke meja operator dan periksa email kamu", computerTarget);
                break;
            case 3:
                ShowObjective("Gunakan komputer kantor", computerTarget);
                break;
            default:
                if (currentStep > 3)
                {
                    objectiveUI?.SetObjective("Selesai");
                    objectiveArrow?.Hide();
                }
                break;
        }
    }

    private void OnVNDialogueStarted(VNNPCInteractable source)
    {
        objectiveArrow?.Hide();
    }

    private void OnVNDialogueEnded(VNNPCInteractable source)
    {
        if (source != arunika)
            return;

        currentStep = 2;
        ShowObjective("Pergi ke meja operator dan periksa email kamu", computerTarget);
    }

    private void OnDesktopOpening()
    {
        currentStep = 3;
        objectiveArrow?.Hide();
    }

    private void ShowObjective(string text, Transform target)
    {
        objectiveUI?.SetObjective(text);

        if (target != null)
            objectiveArrow?.Show(target);
        else
            objectiveArrow?.Hide();
    }
}
