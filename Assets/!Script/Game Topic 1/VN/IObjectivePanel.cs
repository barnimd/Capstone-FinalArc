/// <summary>
/// Common contract for any objective panel UI used by the VN system.
/// Implement this on per-topic classes (ObjectiveUI_Tp1 / Tp3 / Tp4 / ...)
/// so VNNPCInteractable can drive them all through the same call.
/// </summary>
public interface IObjectivePanel
{
    /// <summary>
    /// Display the given objective text on the panel.
    /// Implementations may animate, fade, slide, etc. — the contract only
    /// guarantees that the supplied text becomes visible to the player.
    /// </summary>
    void ShowObjective(string text);
}
