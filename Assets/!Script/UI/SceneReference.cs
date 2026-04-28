using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneReference
{
#if UNITY_EDITOR
    public SceneAsset sceneAsset;
#endif
    [HideInInspector]
    public string sceneName;

    public string SceneName => sceneName;
}
