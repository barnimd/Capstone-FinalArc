using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ObjectiveUI_Tp3 : MonoBehaviour
{
    public TextMeshProUGUI objectiveText;

    public void SetObjective(string text)
    {
        objectiveText.text = text;
    }
}
