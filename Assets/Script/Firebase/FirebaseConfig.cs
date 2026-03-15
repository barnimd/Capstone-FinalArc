using UnityEngine;

[CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Firebase/Config")]
public class FirebaseConfig : ScriptableObject
{
    public string projectId;
    public string apiKey;

    public string FirestoreBase =>
        "https://firestore.googleapis.com/v1/projects/" + projectId + "/databases/(default)/documents";

    public string AuthBase => "https://identitytoolkit.googleapis.com/v1";

    public string AuthDomain => projectId + ".firebaseapp.com";
}
