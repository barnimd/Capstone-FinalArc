using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI")]
    public GameObject canvasInstructor;
    public ObjectiveUI objectiveUI;

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
        objectiveUI.SetObjective("Cari orang di meja kerja");
    }

    public void CloseInstructor()
    {
        canvasInstructor.SetActive(false);
    }

    public void NextObjective()
    {
        currentStep++;

        switch (currentStep)
        {
            case 1:
                objectiveUI.SetObjective("Ajak ngobrol orang di meja");
                break;

            case 2:
                objectiveUI.SetObjective("Gunakan komputer kantor");
                break;

            default:
                objectiveUI.SetObjective("Selesai");
                break;
        }
    }
}
