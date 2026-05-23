using UnityEngine;

public class ObjectiveStarter_Tp2 : MonoBehaviour
{
    public static ObjectiveStarter_Tp2 Instance;
    public ObjectiveUI_Tp4 objectiveUI;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (objectiveUI != null)
            objectiveUI.ShowObjective("Sapa Resepsionis di meja");
    }

    public void HideObjective()
    {
        if (objectiveUI != null)
            objectiveUI.gameObject.SetActive(false);
    }
}
