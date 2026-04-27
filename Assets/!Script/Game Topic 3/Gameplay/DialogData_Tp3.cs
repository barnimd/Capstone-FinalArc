using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogLine_Tp3
{
    public string speakerName;
    [TextArea(2, 4)]
    public string text;
}

[CreateAssetMenu(fileName = "DialogData_Tp3", menuName = "Game/Dialog Data Tp3")]
public class DialogData_Tp3 : ScriptableObject
{
    public DialogLine_Tp3[] lines;
    public string objectiveAfterDialog;
}
