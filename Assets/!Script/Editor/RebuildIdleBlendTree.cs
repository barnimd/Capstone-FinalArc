#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// Temporary editor utility — safe to delete after running once.
/// Run via: Tools > Setup > Rebuild Idle Blend Tree
public static class RebuildIdleBlendTree
{
    private const string ControllerPath = "Assets/Animations/TopDown/PlayerTopDown.controller";

    [MenuItem("Tools/Setup/Rebuild Idle Blend Tree")]
    public static void Rebuild()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[RebuildIdleBlendTree] Controller not found at: " + ControllerPath);
            return;
        }

        var sm = controller.layers[0].stateMachine;

        // Load all 4 idle clips
        var clipDown  = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/TopDown/IdleDownNew.anim");
        var clipUp    = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/TopDown/IdleUp.anim");
        var clipLeft  = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/TopDown/IdleLeft.anim");
        var clipRight = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/TopDown/IdleRight.anim");

        if (clipDown == null || clipUp == null || clipLeft == null || clipRight == null)
        {
            Debug.LogError("[RebuildIdleBlendTree] One or more idle clips not found. Expected: IdleDownNew, IdleUp, IdleLeft, IdleRight in Assets/Animations/TopDown/");
            return;
        }

        // Remove old IdleDown state
        AnimatorState oldIdle = null;
        foreach (var cs in sm.states)
            if (cs.state.name == "IdleDown") { oldIdle = cs.state; break; }
        if (oldIdle != null)
            sm.RemoveState(oldIdle);

        // Create new Idle blend tree state
        var idleState = sm.AddState("Idle", new Vector3(-100f, 0f, 0f));
        var blendTree = new BlendTree();
        blendTree.name = "Idle";
        blendTree.blendType       = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter  = "moveX";
        blendTree.blendParameterY = "moveY";
        blendTree.AddChild(clipDown,  new Vector2( 0f, -1f));
        blendTree.AddChild(clipUp,    new Vector2( 0f,  1f));
        blendTree.AddChild(clipLeft,  new Vector2(-1f,  0f));
        blendTree.AddChild(clipRight, new Vector2( 1f,  0f));

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        idleState.motion = blendTree;
        sm.defaultState  = idleState;

        // Find Walk state
        AnimatorState walkState = null;
        foreach (var cs in sm.states)
            if (cs.state.name == "Walk") { walkState = cs.state; break; }

        if (walkState != null)
        {
            // Idle → Walk
            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration    = 0.05f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Remove old Walk → IdleDown transitions, add Walk → Idle
            foreach (var t in walkState.transitions)
                walkState.RemoveTransition(t);

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration    = 0.05f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RebuildIdleBlendTree] Done! Idle is now a 4-direction Blend Tree.");
    }
}
#endif
