using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wires CanvasLeaderboard to LeaderboardManager and dynamically instantiates rows.
///
/// Two modes (toggle via `mode` field):
///   • PerStage — show top-N for a single stage. Topic column is left blank.
///   • Global   — show best-topic-per-user across all stages. Topic column = stage name.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    public enum LeaderboardMode { PerStage, Global }

    [Header("Config")]
    public LeaderboardMode mode          = LeaderboardMode.Global;
    [Tooltip("Only used in PerStage mode.")]
    public string          stageId       = "phishing";
    public int             limit         = 10;
    public bool            fetchOnEnable = true;

    [Header("Row template & container")]
    [Tooltip("A GameObject with LeaderboardRowEntry attached. Will be hidden + cloned at runtime.")]
    public LeaderboardRowEntry rowTemplate;
    [Tooltip("Parent Transform for spawned rows. Defaults to rowTemplate.parent if null.")]
    public Transform           rowContainer;

    [Header("Optional state panels")]
    public GameObject emptyStatePanel;
    public GameObject loadingStatePanel;

    private readonly List<LeaderboardRowEntry> spawnedRows = new List<LeaderboardRowEntry>();

    void OnEnable()
    {
        if (fetchOnEnable) Refresh();
    }

    /// <summary>Manual refresh (e.g. for a refresh button onClick).</summary>
    public void Refresh()
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUI] LeaderboardManager.Instance missing — ensure SecMindManagers exists in this scene");
            return;
        }
        if (rowTemplate == null)
        {
            Debug.LogWarning("[LeaderboardUI] rowTemplate not assigned");
            return;
        }

        if (loadingStatePanel != null) loadingStatePanel.SetActive(true);
        if (emptyStatePanel != null)   emptyStatePanel.SetActive(false);

        if (mode == LeaderboardMode.Global)
            LeaderboardManager.Instance.FetchGlobal(limit, OnGlobalLoaded);
        else
            LeaderboardManager.Instance.Fetch(stageId, limit, OnPerStageLoaded);
    }

    /// <summary>Switch the stage shown (PerStage mode). Forces mode to PerStage.</summary>
    public void SetStage(string newStageId)
    {
        mode    = LeaderboardMode.PerStage;
        stageId = newStageId;
        Refresh();
    }

    /// <summary>Switch to Global mode and refresh.</summary>
    public void SetGlobalMode()
    {
        mode = LeaderboardMode.Global;
        Refresh();
    }

    private void OnPerStageLoaded(bool ok, LeaderboardEntryDTO[] entries)
    {
        if (loadingStatePanel != null) loadingStatePanel.SetActive(false);
        ClearSpawned();

        if (!ok || entries == null)
        {
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
            return;
        }

        if (emptyStatePanel != null) emptyStatePanel.SetActive(entries.Length == 0);
        Debug.Log($"[LeaderboardUI] Fetched {entries.Length} per-stage entries (stage={stageId})");

        PrepareTemplate(out Transform parent, out int templateSiblingIndex);
        if (parent == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            LeaderboardEntryDTO e = entries[i];
            LeaderboardRowEntry clone = SpawnRow(parent, templateSiblingIndex, i, e.rank);
            clone.SetData(e.rank, e.displayName, e.score, topic: null);
        }
    }

    private void OnGlobalLoaded(bool ok, GlobalLeaderboardEntryDTO[] entries)
    {
        if (loadingStatePanel != null) loadingStatePanel.SetActive(false);
        ClearSpawned();

        if (!ok || entries == null)
        {
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
            return;
        }

        if (emptyStatePanel != null) emptyStatePanel.SetActive(entries.Length == 0);
        Debug.Log($"[LeaderboardUI] Fetched {entries.Length} global entries");

        PrepareTemplate(out Transform parent, out int templateSiblingIndex);
        if (parent == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            GlobalLeaderboardEntryDTO e = entries[i];
            LeaderboardRowEntry clone = SpawnRow(parent, templateSiblingIndex, i, e.rank);
            clone.SetData(e.rank, e.displayName, e.score, topic: e.bestStageName);
        }
    }

    private void PrepareTemplate(out Transform parent, out int templateSiblingIndex)
    {
        rowTemplate.gameObject.SetActive(false);
        parent = rowContainer != null ? rowContainer : rowTemplate.transform.parent;
        templateSiblingIndex = rowTemplate.transform.GetSiblingIndex();
        if (parent == null)
            Debug.LogWarning("[LeaderboardUI] no rowContainer and rowTemplate has no parent");
    }

    private LeaderboardRowEntry SpawnRow(Transform parent, int templateSiblingIndex, int index, int rank)
    {
        LeaderboardRowEntry clone = Instantiate(rowTemplate, parent);
        clone.gameObject.SetActive(true);
        clone.name = $"Row_{rank}";
        clone.transform.SetSiblingIndex(templateSiblingIndex + 1 + index);
        spawnedRows.Add(clone);
        return clone;
    }

    private void ClearSpawned()
    {
        foreach (LeaderboardRowEntry row in spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        spawnedRows.Clear();
    }
}
