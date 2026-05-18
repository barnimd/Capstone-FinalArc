using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameTopic2SceneSetup
{
    [MenuItem("Game/Setup Topic 2 - Office Environment")]
    public static void SetupScene()
    {
        if (Object.FindObjectOfType<ObjectiveStarter_Tp2>(true) != null) return;

        var objUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        if (objUI == null) return;

        var go = new GameObject("ObjectiveStarter");
        go.AddComponent<ObjectiveStarter_Tp2>().objectiveUI = objUI;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] ObjectiveStarter created.");
    }
}
