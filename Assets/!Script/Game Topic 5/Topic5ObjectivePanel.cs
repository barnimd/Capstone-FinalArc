using TMPro;
using UnityEngine;

public class Topic5ObjectivePanel : MonoBehaviour, IObjectivePanel
{
    [SerializeField] private TMP_Text objectiveText;

    [Tooltip("If set, objectives are shown via the redesigned 2-panel UI instead of this panel.")]
    public ObjectiveRedesignUI redesign;

    public void ShowObjective(string text)
    {
        if (redesign != null) { redesign.ShowObjective(text); HideOwnGraphics(); return; }

        gameObject.SetActive(true);
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private void OnDisable()
    {
        if (redesign != null) redesign.HideAll();
    }

    private void HideOwnGraphics()
    {
        foreach (var g in GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) g.enabled = false;
    }
}
