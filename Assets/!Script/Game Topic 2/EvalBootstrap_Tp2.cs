using UnityEngine;

public class EvalBootstrap_Tp2 : MonoBehaviour
{
    void Awake()
    {
        var animator = Object.FindObjectOfType<GameTopic2.PasswordOutcomeAnimator>(true);
        var evalMgr = Object.FindObjectOfType<EvaluationManager_Tp4>(true);
        if (animator == null || evalMgr == null) return;

        animator.onBeforeSummary = () =>
        {
            evalMgr.TriggerEvaluation(() =>
            {
                SummaryManager.instance.TriggerSummary();
            });
        };
    }
}
