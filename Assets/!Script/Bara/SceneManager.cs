using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor; // Library khusus Editor, biar bisa drag-drop scene
#endif

public class SceneLoaderSmart : MonoBehaviour
{
    // Bagian ini cuma muncul di Unity Editor buat tempat kamu naruh Scene
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [HideInInspector]
    public string nameScene;
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (sceneAsset != null)
        {
            nameScene = sceneAsset.name;
        }
#endif
    }
    public void GantiScene()
    { 
        if (!string.IsNullOrEmpty(nameScene))
        {
            SceneManager.LoadScene(nameScene);
        }
        else
        {
            Debug.LogError("Scene belum dimasukin di Inspector!");
        }
    }
}