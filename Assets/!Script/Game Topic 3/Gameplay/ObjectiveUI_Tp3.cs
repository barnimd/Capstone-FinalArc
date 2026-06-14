using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ObjectiveUI_Tp3 : MonoBehaviour, IObjectivePanel
{
    public TextMeshProUGUI objectiveText;

    [Tooltip("If set, objectives are shown via the redesigned 2-panel UI instead of this panel.")]
    public ObjectiveRedesignUI redesign;

    /// <summary>
    /// Both entry points (SetObjective and the IObjectivePanel ShowObjective) forward
    /// to the redesign when assigned — GameManager_Tp3 drives objectives via SetObjective.
    /// </summary>
    public void SetObjective(string text)
    {
        if (redesign != null) { redesign.ShowObjective(text); HideOwnGraphics(); return; }
        if (objectiveText != null) objectiveText.text = text;
    }

    public void ShowObjective(string text) => SetObjective(text);

    private void Start()
    {
        if (redesign == null) redesign = FindObjectOfType<ObjectiveRedesignUI>(true);
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
