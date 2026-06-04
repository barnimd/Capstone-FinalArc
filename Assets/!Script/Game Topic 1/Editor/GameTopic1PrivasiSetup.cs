using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameTopic1PrivasiSetup
{
    [MenuItem("Game/Setup Topic 1 - Privasi Keamanan")]
    public static void SetupScene()
    {
        Debug.Log("=== Setup Topic 1 Privasi ===");

        FixCanvases();
        SetupInstallerCrash();
        WireGameFlowCrash();
        SetupFocusedProgression();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("=== Setup Topic 1 Privasi SELESAI ===");
    }

    static void FixCanvases()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.transform.localScale == Vector3.zero)
                c.transform.localScale = Vector3.one;
    }

    static void SetupInstallerCrash()
    {
        // Check if already wired
        var flow = Object.FindObjectOfType<InstallerFlow>(true);
        if (flow != null && flow.crash != null)
        {
            Debug.Log("[Setup] InstallerFlow.crash already wired. Skip.");
            return;
        }

        // Delete old if exists
        var old = GameObject.Find("CrashOverlayCanvas_Installer");
        if (old != null) Object.DestroyImmediate(old);

        var go = new GameObject("CrashOverlayCanvas_Installer",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = go.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        cv.overrideSorting = true;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(800, 600);
        go.SetActive(false);

        var bg = new GameObject("CrashOverlay", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.sizeDelta = brt.anchoredPosition = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.95f);

        var txt = new GameObject("CrashText", typeof(RectTransform));
        txt.transform.SetParent(bg.transform, false);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0, 60);
        trt.sizeDelta = new Vector2(500, 300);
        var ttmp = txt.AddComponent<TextMeshProUGUI>();
        ttmp.fontSize = 15;
        ttmp.alignment = TextAlignmentOptions.Left;

        var ctrl = go.AddComponent<CrashOverlayController_Tp1_Installer>();
        ctrl.crashCanvas = go;
        ctrl.mainBg = bg.GetComponent<Image>();
        ctrl.mainText = ttmp;

        if (flow != null)
        {
            flow.crash = ctrl;
            Debug.Log("[Setup] InstallerFlow.crash wired to CrashOverlayController_Tp1_Installer.");
        }
    }

    static void WireGameFlowCrash()
    {
        var gf = Object.FindObjectOfType<GameFlowManager>(true);
        if (gf == null || gf.crash != null) return;

        var existingCrash = Object.FindObjectOfType<CrashOverlayController_Tp1>(true);
        if (existingCrash != null)
        {
            gf.crash = existingCrash;
            Debug.Log("[Setup] GameFlowManager.crash wired to existing CrashOverlayController_Tp1.");
        }
    }

    static void SetupFocusedProgression()
    {
        VNNPCInteractable receptionist = FindVN("NPC Receptionist");
        VNNPCInteractable pantry = FindVN("NPC Talking 1");
        VNNPCInteractable lift = FindVN("RightWallTriggerKitchenToVNLift");
        VNNPCInteractable pakBudi = FindVN("NPC Asking Install Data");
        InteractOpenDesktop computer = FindComponent<InteractOpenDesktop>("ComputerInteraction_2");
        GameFlowManager computerFlow = Object.FindObjectOfType<GameFlowManager>(true);
        InstallerFlow installerFlow = Object.FindObjectOfType<InstallerFlow>(true);
        GameplayManager gameplayManager = Object.FindObjectOfType<GameplayManager>(true);
        ObjectiveUI_Tp1 objective = Object.FindObjectOfType<ObjectiveUI_Tp1>(true);
        FollowArrow_Tp1 arrow = Object.FindObjectOfType<FollowArrow_Tp1>(true);

        GameObject flowRoot = GameObject.Find("Topic1FlowSystem");
        if (flowRoot == null)
            flowRoot = new GameObject("Topic1FlowSystem");

        Topic1EvidenceChecklistController checklist = flowRoot.GetComponent<Topic1EvidenceChecklistController>();
        if (checklist == null)
            checklist = flowRoot.AddComponent<Topic1EvidenceChecklistController>();

        Topic1ProgressionController progression = flowRoot.GetComponent<Topic1ProgressionController>();
        if (progression == null)
            progression = flowRoot.AddComponent<Topic1ProgressionController>();

        progression.receptionist = receptionist;
        progression.pantry = pantry;
        progression.liftTrigger = lift;
        progression.liftArrowTarget = CreateLiftArrowTarget(lift);
        progression.pantryToCubicleTransition = ConfigureLiftReturnTransition(lift);
        progression.pakBudi = pakBudi;
        progression.computer = computer;
        progression.legacyAskingFile = GameObject.Find("NPC Asking File");
        progression.objectivePanel = objective;
        progression.objectiveArrow = arrow;
        progression.evidenceChecklist = checklist;
        progression.computerFlow = computerFlow;
        progression.installerFlow = installerFlow;
        progression.gameplayManager = gameplayManager;
        progression.installerCrash = Object.FindObjectOfType<CrashOverlayController_Tp1_Installer>(true);
        progression.rizalCase = CreateOrUpdateRizalCase();
        progression.pakBudiCase = CreateOrUpdatePakBudiCase();
        progression.computerCase = CreateOrUpdateComputerCase();

        if (installerFlow != null)
        {
            installerFlow.gameObject.SetActive(true);
            if (installerFlow.desktopCanvas != null)
                installerFlow.desktopCanvas.SetActive(false);
            EditorUtility.SetDirty(installerFlow.gameObject);
        }

        CleanupVNInteractable(receptionist, false);
        CleanupVNInteractable(pantry, false);
        CleanupVNInteractable(lift, true);
        CleanupVNInteractable(pakBudi, false);

        RemoveLegacyInteraction(receptionist);
        RemoveLegacyInteraction(pantry);
        RemoveLegacyInteraction(pakBudi);

        if (progression.legacyAskingFile != null)
            progression.legacyAskingFile.SetActive(false);

        if (computer != null)
            computer.requiresNPCUnlock = true;

        if (arrow != null)
        {
            arrow.displayMode = FollowArrow_Tp1.ArrowDisplayMode.ScreenEdge;
            arrow.screenEdgeMargin = 32f;
            arrow.hideWhenTargetVisible = true;
            arrow.hideWithinWorldDistance = 1.5f;
            arrow.rotationOffset = 0f;
            PlayerMovement movement = Object.FindObjectOfType<PlayerMovement>(true);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            arrow.player = movement != null ? movement.transform : player != null ? player.transform : null;

            Image arrowImage = arrow.GetComponent<Image>();
            Sprite topic5Arrow = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Assets/UI Elements/White/2x/up arrow.png");
            if (arrowImage != null)
            {
                arrow.arrowImage = arrowImage;
                arrowImage.sprite = topic5Arrow;
                arrowImage.color = new Color(1f, 0.78f, 0.08f, 1f);
                arrowImage.preserveAspect = true;
                arrowImage.raycastTarget = false;
            }

            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            if (arrowRect != null)
                arrowRect.sizeDelta = new Vector2(46f, 46f);

            EditorUtility.SetDirty(arrow);
            if (arrowImage != null)
                EditorUtility.SetDirty(arrowImage);
        }

        ScoreManager scoreManager = Object.FindObjectOfType<ScoreManager>(true);
        if (scoreManager != null && scoreManager.scoreText != null)
        {
            scoreManager.scoreText.gameObject.SetActive(false);
            scoreManager.scoreText = null;
            EditorUtility.SetDirty(scoreManager);
        }

        Canvas objectiveCanvas = objective != null ? objective.GetComponentInParent<Canvas>(true) : null;
        if (objective != null)
        {
            objective.panelRoot = objective.gameObject;
            objective.centerSize = new Vector2(820f, 150f);
            objective.cornerPosition = new Vector2(18f, -18f);
            objective.cornerSize = new Vector2(560f, 100f);

            if (objective.objectiveText != null)
            {
                RectTransform textRect = objective.objectiveText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = new Vector2(24f, 14f);
                textRect.offsetMax = new Vector2(-24f, -14f);
                objective.objectiveText.fontSize = 24f;
                objective.objectiveText.alignment = TextAlignmentOptions.MidlineLeft;
                EditorUtility.SetDirty(objective.objectiveText);
            }

            EditorUtility.SetDirty(objective);
        }

        if (objectiveCanvas != null)
        {
            CanvasScaler scaler = objectiveCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        VNDialogueManager vnManager = Object.FindObjectOfType<VNDialogueManager>(true);
        if (vnManager != null)
        {
            vnManager.acceptScore = 0;
            vnManager.rejectScore = 0;
        }

        DisableVNChoice("Assets/!Script/Game Topic 1/VN/VNDialog_Lift_SuspiciousMan.asset");
        DisableVNChoice("Assets/!Script/Game Topic 1/VN/VNDialog_InstallData.asset");

        EditorUtility.SetDirty(flowRoot);
        EditorUtility.SetDirty(progression);
        Debug.Log("[Setup] Topic 1 focused progression, checklist, and screen-edge arrow wired.");
    }

    static VNNPCInteractable FindVN(string gameObjectName)
    {
        return FindComponent<VNNPCInteractable>(gameObjectName);
    }

    static T FindComponent<T>(string gameObjectName) where T : Component
    {
        GameObject go = GameObject.Find(gameObjectName);
        return go != null ? go.GetComponent<T>() : null;
    }

    static void CleanupVNInteractable(VNNPCInteractable interactable, bool autoTrigger)
    {
        if (interactable == null)
            return;

        interactable.nextNPCToActivate = null;
        interactable.npcToDisableAfterDialogue = null;
        interactable.activateAfterDialogue = null;
        interactable.activateOnAccept = null;
        interactable.objectiveUI = null;
        interactable.objectiveTextOnEnd = "";
        interactable.followArrow = null;
        interactable.followArrowTarget = null;
        interactable.autoTrigger = autoTrigger;
        interactable.isInteractable = false;
        interactable.oneTimeOnly = true;
        EditorUtility.SetDirty(interactable);
    }

    static RoomTransitionTrigger ConfigureLiftReturnTransition(VNNPCInteractable lift)
    {
        if (lift == null)
            return null;

        RoomTransitionTrigger transition = lift.GetComponent<RoomTransitionTrigger>();
        if (transition == null)
            transition = lift.gameObject.AddComponent<RoomTransitionTrigger>();

        transition.spawnPoint = lift.roomSpawnPoint;
        transition.cameraFollow = lift.roomCameraFollow;
        transition.cameraSnapPoint = lift.roomCameraSnapPoint;
        transition.newMinBounds = lift.roomMinBounds;
        transition.newMaxBounds = lift.roomMaxBounds;
        transition.retriggerDelay = 0.35f;
        transition.enabled = false;
        EditorUtility.SetDirty(transition);
        return transition;
    }

    static Transform CreateLiftArrowTarget(VNNPCInteractable lift)
    {
        if (lift == null)
            return null;

        const string targetName = "Topic1ArrowTarget_LiftDoor";
        GameObject target = GameObject.Find(targetName);
        if (target == null)
            target = new GameObject(targetName);

        Vector3 triggerPosition = lift.transform.position;
        target.transform.position = new Vector3(triggerPosition.x, 0f, triggerPosition.z);
        EditorUtility.SetDirty(target);
        return target.transform;
    }

    static void RemoveLegacyInteraction(VNNPCInteractable interactable)
    {
        if (interactable == null)
            return;

        NPCBubbleInteractable legacy = interactable.GetComponent<NPCBubbleInteractable>();
        if (legacy != null)
            Object.DestroyImmediate(legacy);
    }

    static void DisableVNChoice(string path)
    {
        VNDialogueData data = AssetDatabase.LoadAssetAtPath<VNDialogueData>(path);
        if (data == null)
            return;

        data.hasChoice = false;
        data.dialogueIfAccepted = null;
        data.dialogueIfRejected = null;
        EditorUtility.SetDirty(data);
    }

    static Topic1EvidenceCase CreateOrUpdateRizalCase()
    {
        return CreateOrUpdateCase(
            "Topic1Evidence_Rizal",
            "Verifikasi Permintaan Rizal",
            "Rizal meminta file Finance Q3 saat bertemu di lift. Tandai informasi yang merupakan red flag.",
            new[]
            {
                "Identitas Rizal belum pernah diverifikasi.",
                "Permintaan menyangkut file keuangan sensitif.",
                "Ia menekan agar permintaan diselesaikan secepatnya.",
                "Ia mengatakan berasal dari divisi Finance."
            },
            new[] { 0, 1, 2 },
            "Tolak dan verifikasi melalui supervisor",
            "Kirim file Finance Q3",
            "Keputusan aman. Permintaan akses data sensitif harus diverifikasi melalui jalur resmi, terlepas dari seberapa mendesaknya permintaan tersebut.",
            "Keputusan berisiko. Penyerang sering menggunakan identitas yang belum terverifikasi, data sensitif, dan tekanan waktu untuk mendorong korban melewati prosedur.");
    }

    static Topic1EvidenceCase CreateOrUpdatePakBudiCase()
    {
        return CreateOrUpdateCase(
            "Topic1Evidence_PakBudi",
            "Verifikasi Permintaan Instalasi",
            "Pak Budi meminta software akuntansi dari email segera dipasang. Tandai informasi yang merupakan red flag.",
            new[]
            {
                "Software dikirim melalui email, bukan sistem IT resmi.",
                "Sumber file hanya disebut sebagai teman di Finance.",
                "Ada tekanan waktu sebelum meeting.",
                "Pak Budi memang bekerja di kantor ini."
            },
            new[] { 0, 1, 2 },
            "Tolak dan verifikasi file",
            "Instal software tersebut",
            "Keputusan aman. Software harus berasal dari sumber resmi dan diverifikasi sebelum memperoleh izin pada perangkat kantor.",
            "Keputusan berisiko. Attachment tidak terverifikasi dapat memasang malware, mengambil data, atau membuka akses ke sistem perusahaan.");
    }

    static Topic1EvidenceCase CreateOrUpdateComputerCase()
    {
        return CreateOrUpdateCase(
            "Topic1Evidence_Computer",
            "Tinjau Permintaan File",
            "Tinjau kembali permintaan pengiriman file yang muncul pada komputer. Tandai red flag yang seharusnya diperiksa.",
            new[]
            {
                "File yang diminta berisi informasi finansial sensitif.",
                "Identitas dan kewenangan peminta belum diverifikasi.",
                "Permintaan mendorong pengiriman langsung tanpa jalur resmi.",
                "Permintaan muncul melalui aplikasi chat kantor."
            },
            new[] { 0, 1, 2 },
            "Tolak pengiriman",
            "Kirim file",
            "Keputusan aman. Menolak sementara memberi waktu untuk memverifikasi identitas, kewenangan, dan jalur pengiriman yang benar.",
            "Keputusan berisiko. Mengirim data sebelum verifikasi dapat menyebabkan kebocoran informasi sensitif.");
    }

    static Topic1EvidenceCase CreateOrUpdateCase(
        string assetName,
        string title,
        string scenario,
        string[] options,
        int[] redFlags,
        string safeAction,
        string unsafeAction,
        string safeFeedback,
        string unsafeFeedback)
    {
        const string folder = "Assets/!Script/Game Topic 1/Evidence";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/!Script/Game Topic 1", "Evidence");

        string path = folder + "/" + assetName + ".asset";
        Topic1EvidenceCase evidenceCase = AssetDatabase.LoadAssetAtPath<Topic1EvidenceCase>(path);
        if (evidenceCase == null)
        {
            evidenceCase = ScriptableObject.CreateInstance<Topic1EvidenceCase>();
            AssetDatabase.CreateAsset(evidenceCase, path);
        }

        evidenceCase.title = title;
        evidenceCase.scenario = scenario;
        evidenceCase.evidenceOptions = options;
        evidenceCase.redFlagIndices = redFlags;
        evidenceCase.safeActionText = safeAction;
        evidenceCase.unsafeActionText = unsafeAction;
        evidenceCase.safeFeedback = safeFeedback;
        evidenceCase.unsafeFeedback = unsafeFeedback;
        evidenceCase.unsafePenalty = -25;
        EditorUtility.SetDirty(evidenceCase);
        return evidenceCase;
    }
}
