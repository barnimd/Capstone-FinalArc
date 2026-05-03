using UnityEngine;

[System.Serializable]
public class URLEntry
{
    [Tooltip("URL lengkap yang ditampilkan di address bar, contoh: https://go0gle.com/login")]
    public string url;

    [Tooltip("Nama website, contoh: Google, Microsoft")]
    public string siteName;

    [Tooltip("True jika ini URL phishing, false jika legitimate")]
    public bool isPhishing;

    [TextArea(2, 4)]
    [Tooltip("Penjelasan kenapa URL ini phishing atau legit")]
    public string explanation;
}

[CreateAssetMenu(fileName = "URLData_Tp4", menuName = "Game/URL Data Tp4")]
public class URLData_Tp4 : ScriptableObject
{
    [Tooltip("Pool URL phishing (min 5 direkomendasikan)")]
    public URLEntry[] phishingURLs;

    [Tooltip("Pool URL legitimate (min 3 direkomendasikan)")]
    public URLEntry[] legitimateURLs;
}
