using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Topic5ThreeWayChoiceController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Button choiceAButton;
    [SerializeField] private Button choiceBButton;
    [SerializeField] private Button choiceCButton;

    private Action<int> onSelected;

    private void Awake()
    {
        choiceAButton.onClick.AddListener(() => Select(0));
        choiceBButton.onClick.AddListener(() => Select(1));
        choiceCButton.onClick.AddListener(() => Select(2));
        Hide();
    }

    public void Show(Action<int> callback)
    {
        onSelected = callback;
        root.SetActive(true);
        promptText.text = "Bagaimana Raka merespons Mas Anto?";
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void Select(int index)
    {
        Hide();
        Action<int> callback = onSelected;
        onSelected = null;
        callback?.Invoke(index);
    }
}
