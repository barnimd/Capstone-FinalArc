using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager_Tp3 : MonoBehaviour
{
    public static GameManager_Tp3 instance;

    [Header("UI")]
    public GameObject canvasInstructor;
    public ObjectiveUI_Tp3 objectiveUI;

    private int currentStep = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        currentStep = 0;
        objectiveUI.SetObjective("Sapa orang di Meja Kerja");
    }

    public void CloseInstructor()
    {
        canvasInstructor.SetActive(false);
    }

    public void OnPlayerApproachNPC()
    {
        if (currentStep == 0)
        {
            currentStep = 1;
            objectiveUI.SetObjective("Bicara dengan Arunika...");
        }
    }

    public void OnDialogFinished()
    {
        NextObjective();
    }

    public void SetObjectiveFromDialog(string objectiveText)
    {
        objectiveUI.SetObjective(objectiveText);
    }

    public void NextObjective()
    {
        currentStep++;

        switch (currentStep)
        {
            case 2:
                objectiveUI.SetObjective("Pergi ke meja operator dan periksa email kamu");
                break;
            case 3:
                objectiveUI.SetObjective("Gunakan komputer kantor");
                break;
            default:
                if (currentStep > 3)
                    objectiveUI.SetObjective("Selesai");
                break;
        }
    }
}