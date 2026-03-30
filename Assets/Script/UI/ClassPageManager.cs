using UnityEngine;

public class ClassPageManager : MonoBehaviour
{
    [Header("Lesson Data")]
    [SerializeField] private LessonData[] lessons;

    [Header("Grid References")]
    [SerializeField] private Transform unlockedGrid;
    [SerializeField] private Transform lockedGrid;

    [Header("Prefab")]
    [SerializeField] private GameObject lessonCardPrefab;

    private void Start()
    {
        PopulateCards();
    }

    private void PopulateCards()
    {
        if (lessonCardPrefab == null)
        {
            Debug.LogError("ClassPageManager: lessonCardPrefab is not assigned.");
            return;
        }

        int unlockedIndex = 1;
        int lockedIndex = 1;

        foreach (LessonData data in lessons)
        {
            if (data == null) continue;

            Transform targetGrid = data.isUnlocked ? unlockedGrid : lockedGrid;
            int cardIndex = data.isUnlocked ? unlockedIndex++ : lockedIndex++;

            if (targetGrid == null) continue;

            GameObject cardGO = Instantiate(lessonCardPrefab, targetGrid);
            LessonCard card = cardGO.GetComponent<LessonCard>();
            if (card != null)
                card.Setup(data, cardIndex);
        }
    }
}
