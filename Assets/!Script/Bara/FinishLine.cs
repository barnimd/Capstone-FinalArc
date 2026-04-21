using UnityEngine;

public class FinishLine : MonoBehaviour
{
    public GameplayManager gameManager;

    // Fungsi ini otomatis terpanggil saat ada objek menyentuh collider ini
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Pastikan objek yang menyentuh memiliki Tag "Player"
        if (collision.CompareTag("Player"))
        {
            gameManager.FinishGame();
        }
    }
}