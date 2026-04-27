using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogManager_Tp3 : MonoBehaviour
{
    public static DialogManager_Tp3 instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI NPCnameText;
    public TextMeshProUGUI DialogueText;

    [Header("Speaker Colors")]
    public Color playerNameColor = new Color(0.4f, 0.8f, 1f);
    public Color npcNameColor = new Color(1f, 0.75f, 0.4f);

    private DialogData_Tp3 currentDialog;
    private int currentLine = 0;
    private bool isDialogActive = false;

    void Awake()
    {
        instance = this;
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (isDialogActive && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)))
            NextLine();
    }

    public void StartDialog(DialogData_Tp3 data)
    {
        if (isDialogActive) return;

        currentDialog = data;
        currentLine = 0;
        isDialogActive = true;

        dialoguePanel.SetActive(true);
        ShowLine();
    }

    void ShowLine()
    {
        DialogLine_Tp3 line = currentDialog.lines[currentLine];
        NPCnameText.text = line.speakerName;
        DialogueText.text = line.text;
        NPCnameText.color = line.speakerName == "Joji" ? playerNameColor : npcNameColor;
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine >= currentDialog.lines.Length)
            EndDialog();
        else
            ShowLine();
    }

    void EndDialog()
    {
        isDialogActive = false;
        dialoguePanel.SetActive(false);

        if (!string.IsNullOrEmpty(currentDialog.objectiveAfterDialog))
            GameManager_Tp3.instance.SetObjectiveFromDialog(currentDialog.objectiveAfterDialog);

        GameManager_Tp3.instance.OnDialogFinished();
    }

    public bool IsActive() => isDialogActive;
}