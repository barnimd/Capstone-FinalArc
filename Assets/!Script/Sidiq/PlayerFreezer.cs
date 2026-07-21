using UnityEngine;

/// <summary>
/// Stops the player the moment the end-game summary panel appears, no matter which
/// movement script a topic uses. Different topics use different controllers:
///   • Top-down topics (e.g. Topic 1) → <see cref="TopDownPlayerMovement"/> (has a CanMove flag)
///   • Side-scroller topics (e.g. Topic 2) → <c>_PlayerMovement</c> + <c>_PlayerJump</c>
///     (read Input directly every Update, so they must be disabled)
/// FreezeAll() handles every known controller and is a safe no-op when none are present.
/// Call it from any summary trigger (SummaryManager, GameplayManager.FinishGame, etc).
/// </summary>
public static class PlayerFreezer
{
    public static void FreezeAll()
    {
        int frozen = 0;

        foreach (var m in Object.FindObjectsOfType<TopDownPlayerMovement>())
        {
            m.CanMove = false;
            frozen++;
        }

        foreach (var m in Object.FindObjectsOfType<_PlayerMovement>())
        {
            m.enabled = false;
            frozen++;
        }

        foreach (var j in Object.FindObjectsOfType<_PlayerJump>())
        {
            j.enabled = false;
            frozen++;
        }

        if (frozen == 0)
            Debug.LogWarning("[PlayerFreezer] No known player-movement component found to freeze.");
    }
}
