using UnityEngine;

public class HideObjectiveOnDesktop_Tp2 : MonoBehaviour
{
    void OnEnable()
    {
        ObjectiveStarter_Tp2.Instance?.HideObjective();
    }
}
