using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LessonCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Button button;

    public void Setup(LessonData data, int index)
    {
        if (numberText != null)
            numberText.text = index.ToString("D2");

        if (titleText != null)
            titleText.text = data.title;

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        bool unlocked = data.IsUnlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        if (button != null)
        {
            button.interactable = unlocked;

            if (unlocked && !string.IsNullOrEmpty(data.sceneName))
            {
                string scene = data.sceneName;
                button.onClick.AddListener(() => SceneManager.LoadScene(scene));
            }
        }
    }
}
