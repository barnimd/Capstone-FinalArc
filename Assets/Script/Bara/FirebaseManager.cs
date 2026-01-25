using UnityEngine;
using FirebaseWebGL.Scripts.FirebaseBridge; // Namespace dari plugin


public class FirebaseManager : MonoBehaviour
{
    // Fungsi untuk Login Google
    public void SignWithGoogle()
    {
        // "gameObject.name" adalah nama objek tempat script ini menempel
        // "OnLoginSuccess" adalah nama fungsi yang dipanggil jika berhasil
        FirebaseAuth.SignInWithGoogle(gameObject.name, "OnLoginSuccess", "OnLoginError");
    }

    void OnLoginSuccess(string data)
    {
        Debug.Log("Login Berhasil: " + data);
        // Contoh: Ambil nama dari data JSON user
        // Setelah login, kamu bisa panggil fungsi Firestore di sini
    }

    void OnLoginError(string error)
    {
        Debug.LogError("Login Gagal: " + error);
    }

    // Fungsi untuk Simpan Data ke Firestore
    public void SaveScore(int score)
    {
        string jsonData = "{\"score\":" + score + "}";
        // Simpan ke collection "UserScores", dengan ID document "Player1"
        FirebaseFirestore.SetDocument("UserScores", "Player1", jsonData, gameObject.name, "OnSaveSuccess", "OnSaveError");
    }

    void OnSaveSuccess(string data) => Debug.Log("Data Tersimpan!");
    void OnSaveError(string error) => Debug.LogError("Gagal Simpan: " + error);
}