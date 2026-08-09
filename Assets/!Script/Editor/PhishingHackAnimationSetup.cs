using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Helper editor: bikin GameObject PhishingHackAnimation di scene Email_Layout
/// lalu sambungkan otomatis ke semua EmailDetailButtons yang ada.
/// Menu: Tools > Topic 3 > Setup Phishing Hack Animation
/// </summary>
public static class PhishingHackAnimationSetup
{
    private const string NamaObjek = "PhishingHackAnimation";

    [MenuItem("Tools/Topic 3/Setup Phishing Hack Animation")]
    public static void Setup()
    {
        PhishingHackAnimation anim = Object.FindObjectOfType<PhishingHackAnimation>(true);

        if (anim == null)
        {
            GameObject go = new GameObject(NamaObjek);
            anim = go.AddComponent<PhishingHackAnimation>();
            Undo.RegisterCreatedObjectUndo(go, "Setup Phishing Hack Animation");
            Debug.Log($"[Setup] GameObject '{NamaObjek}' dibuat di scene.");
        }
        else
        {
            Debug.Log($"[Setup] PhishingHackAnimation sudah ada di '{anim.gameObject.name}', dipakai ulang.");
        }

        EmailDetailButtons[] tombol = Object.FindObjectsOfType<EmailDetailButtons>(true);
        int tersambung = 0;
        foreach (EmailDetailButtons t in tombol)
        {
            if (t.animasiKenaHack == anim) continue;
            Undo.RecordObject(t, "Sambung Phishing Hack Animation");
            t.animasiKenaHack = anim;
            EditorUtility.SetDirty(t);
            tersambung++;
        }

        Selection.activeGameObject = anim.gameObject;
        EditorSceneManager.MarkSceneDirty(anim.gameObject.scene);

        Debug.Log($"[Setup] Selesai. {tersambung} EmailDetailButtons disambungkan. " +
                  "Assign sprite PC / Hacker / Mail / Folder di Inspector, lalu Save scene.");
    }
}
