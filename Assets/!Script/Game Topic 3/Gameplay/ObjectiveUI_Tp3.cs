using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ObjectiveUI_Tp3 : MonoBehaviour, IObjectivePanel
{
    public TextMeshProUGUI objectiveText;

    public void SetObjective(string text)
    {
        objectiveText.text = text;
    }

    /// <summary>
    /// IObjectivePanel implementation. Forwards to <see cref="SetObjective"/>
    /// so the VN system can drive Tp3 panels through the shared interface.
    /// </summary>
    public void ShowObjective(string text) => SetObjective(text);
}
