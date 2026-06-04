using TMPro;
using UnityEngine;

public class Topic5ObjectivePanel : MonoBehaviour, IObjectivePanel
{
    [SerializeField] private TMP_Text objectiveText;

    public void ShowObjective(string text)
    {
        gameObject.SetActive(true);
        if (objectiveText != null)
            objectiveText.text = text;
    }
}
