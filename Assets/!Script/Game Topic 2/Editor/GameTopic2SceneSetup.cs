using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameTopic2SceneSetup
{
    [MenuItem("Game/Setup Topic 2 - Office Environment")]
    public static void SetupScene()
    {
        Debug.Log("=== Setup Topic 2 ===");

        SetupObjectiveStarter();
        SetupEvaluation();
        SetupEvalHook();
        SetupDesktopCanvasHook();
        EnsureScoreManager();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("=== Setup Topic 2 SELESAI ===");
    }

    static void SetupObjectiveStarter()
    {
        var objUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        if (objUI == null) return;

        var existing = Object.FindObjectOfType<ObjectiveStarter_Tp2>(true);
        if (existing != null)
        {
            existing.objectiveUI = objUI;
            Debug.Log("[Setup] ObjectiveStarter rewired.");
            return;
        }

        var go = new GameObject("ObjectiveStarter");
        go.AddComponent<ObjectiveStarter_Tp2>().objectiveUI = objUI;

        Debug.Log("[Setup] ObjectiveStarter created.");
    }

    static void SetupEvaluation()
    {
        var go = GameObject.Find("EvaluationManager_Tp2");
        if (go == null) { Debug.LogWarning("[Setup] EvaluationManager_Tp2 GameObject not found."); return; }

        foreach (var c in go.GetComponents<Component>())
            if (c == null) Object.DestroyImmediate(c);

        var em = go.GetComponent<EvaluationManager_Tp4>();
        if (em == null) em = go.AddComponent<EvaluationManager_Tp4>();

        em.evaluationCanvas = FindCanvas("evaluation")?.gameObject;
        em.evaluationPanel = Object.FindObjectOfType<EvaluationPanel_Tp4>(true);
        em.scorePerCorrect = 0;

        if (go.GetComponent<EvalBootstrap_Tp2>() == null)
            go.AddComponent<EvalBootstrap_Tp2>();

        Debug.Log("[Setup] EvaluationManager_Tp4 + EvalBootstrap attached to EvaluationManager_Tp2.");
    }

    static void SetupEvalHook()
    {
        var animator = Object.FindObjectOfType<GameTopic2.PasswordOutcomeAnimator>(true);
        var evalMgr = Object.FindObjectOfType<EvaluationManager_Tp4>(true);

        if (animator == null) { Debug.LogWarning("[Setup] PasswordOutcomeAnimator not found."); return; }
        if (evalMgr == null) { Debug.LogWarning("[Setup] EvaluationManager_Tp4 not found."); return; }

        animator.onBeforeSummary = () =>
        {
            evalMgr.TriggerEvaluation(() =>
            {
                SummaryManager.instance.TriggerSummary();
            });
        };

        Debug.Log("[Setup] EvalHook wired — evaluation before summary.");
    }

    static void SetupDesktopCanvasHook()
    {
        var dc = FindCanvas("desktop")?.gameObject;
        if (dc == null) return;

        if (dc.GetComponent<HideObjectiveOnDesktop_Tp2>() == null)
            dc.AddComponent<HideObjectiveOnDesktop_Tp2>();

        Debug.Log("[Setup] DesktopCanvas hook attached.");
    }

    static Canvas FindCanvas(string p)
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.name.ToLower().Contains(p.ToLower())) return c;
        return null;
    }

    static void EnsureScoreManager()
    {
        if (GameObject.Find("ScoreManager") == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
    }
}
