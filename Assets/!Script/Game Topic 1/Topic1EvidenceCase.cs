using UnityEngine;

[CreateAssetMenu(fileName = "Topic1EvidenceCase", menuName = "Game Topic 1/Evidence Case")]
public class Topic1EvidenceCase : ScriptableObject
{
    [Tooltip("Stable ID used by the post-game AI context catalog.")]
    public string contextId;
    public string title;
    [TextArea(2, 5)] public string scenario;
    public string[] evidenceOptions;
    [Tooltip("Stable ID for each evidenceOptions entry. Keep the same order.")]
    public string[] evidenceIds;
    public int[] redFlagIndices;
    public string safeActionText = "Tolak dan verifikasi";
    public string unsafeActionText = "Penuhi permintaan";
    [TextArea(3, 6)] public string safeFeedback;
    [TextArea(3, 6)] public string unsafeFeedback;
    public int unsafePenalty = -25;

    public bool IsRedFlag(int index)
    {
        if (redFlagIndices == null)
            return false;

        foreach (int redFlagIndex in redFlagIndices)
            if (redFlagIndex == index)
                return true;

        return false;
    }

    public string GetEvidenceId(int index)
    {
        if (evidenceIds != null && index >= 0 && index < evidenceIds.Length &&
            !string.IsNullOrWhiteSpace(evidenceIds[index]))
            return evidenceIds[index];

        return "evidence_" + (index + 1);
    }
}
