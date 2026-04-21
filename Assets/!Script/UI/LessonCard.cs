using UnityEngine;
using UnityEngine.UI;
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

        if (lockOverlay != null)
            lockOverlay.SetActive(!data.isUnlocked);

        if (button != null)
            button.interactable = data.isUnlocked;
    }
}
