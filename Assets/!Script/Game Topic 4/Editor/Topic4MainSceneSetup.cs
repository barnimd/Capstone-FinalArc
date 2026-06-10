using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class Topic4MainSceneSetup
{
    private const string MapPath = "Assets/!Scenes/Game/Game Topic 4/Map_Topic4.unity";
    private const string Topic5MapPath = "Assets/!Scenes/Game/Game Topic 5/Map_Topic_5.unity";
    private const string ComputerPath = "Assets/!Scenes/Game/Game Topic 4/Computer_Interaction.unity";

    [MenuItem("Game/Setup Topic 4 - Main Map Flow")]
    public static void Setup()
    {
        Scene mapScene = EditorSceneManager.GetActiveScene();
        if (mapScene.path != MapPath)
            mapScene = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Single);

        SetupMapColliders();

        DeleteGeneratedRoot("Topic4FlowSystem");
        DeleteGeneratedRoot("Topic4ObjectiveCanvas");
        DeleteGeneratedRoot("VN_Canvas");
        DeleteGeneratedRoot("EmbeddedComputerInteraction");

        VNDialogueManager dialogueManager = CopyTopic5VNCanvas(mapScene, out GameObject arrowTemplate);
        GameObject embeddedComputer = EmbedComputerScene(mapScene);
        ObjectiveUI_Tp4 objectiveUI = CreateObjectiveCanvas(arrowTemplate, out FollowArrow_Tp1 arrow);

        GameObject flowRoot = new GameObject("Topic4FlowSystem");
        SceneManager.MoveGameObjectToScene(flowRoot, mapScene);

        Sprite receptionistSprite = LoadFirstSprite("Assets/Asset game/Player/npc8_idle.png");
        Sprite dinaSprite = LoadFirstSprite("Assets/Asset game/Player/npc6_idle.png");
        Sprite rafiSprite = LoadFirstSprite("Assets/Asset game/Player/npc7_idle.png");

        VNDialogueData receptionistData = AssetDatabase.LoadAssetAtPath<VNDialogueData>(
            "Assets/!Script/Game Topic 4/VN/VNDialog_Topic4_Receptionist.asset");
        VNDialogueData vendingData = AssetDatabase.LoadAssetAtPath<VNDialogueData>(
            "Assets/!Script/Game Topic 4/VN/VNDialog_Topic4_VendingConversation.asset");

        VNNPCInteractable receptionist = CreateNpc(
            "NPC_Receptionist_Topic4",
            new Vector3(14.8f, 5.85f, 0f),
            receptionistSprite,
            receptionistData,
            dialogueManager,
            flowRoot.transform);
        BoxCollider2D receptionistCollider = receptionist.GetComponent<BoxCollider2D>();
        receptionistCollider.offset = new Vector2(0f, -1.1f);
        receptionistCollider.size = new Vector2(3f, 3.6f);

        VNNPCInteractable vendingConversation = CreateNpc(
            "NPC_Dina_Vending_Topic4",
            new Vector3(-3.35f, 10.65f, 0f),
            dinaSprite,
            vendingData,
            dialogueManager,
            flowRoot.transform);

        GameObject companion = CreateVisualNpc(
            "NPC_Rafi_Vending_Topic4",
            new Vector3(-2.1f, 10.65f, 0f),
            rafiSprite,
            flowRoot.transform);
        companion.transform.localScale = Vector3.one;

        GameObject vendingTarget = new GameObject("TARGET_VendingMachine_Topic4");
        vendingTarget.transform.SetParent(flowRoot.transform, false);
        vendingTarget.transform.position = new Vector3(-3.7f, 11.8f, 0f);

        GameObject deskObject = new GameObject("WorkDesk_Topic4");
        deskObject.transform.SetParent(flowRoot.transform, false);
        deskObject.transform.position = new Vector3(4.8f, -1.25f, 0f);
        BoxCollider2D deskCollider = deskObject.AddComponent<BoxCollider2D>();
        deskCollider.isTrigger = true;
        deskCollider.size = new Vector2(2.2f, 1.8f);
        GameObject deskPrompt = CreatePrompt(deskObject.transform);
        Topic4DeskInteractable desk = deskObject.AddComponent<Topic4DeskInteractable>();

        GameObject deskTarget = new GameObject("TARGET_WorkDesk_Topic4");
        deskTarget.transform.SetParent(deskObject.transform, false);

        Topic4ProgressionController progression = flowRoot.AddComponent<Topic4ProgressionController>();
        TopDownPlayerMovement movement = Object.FindObjectOfType<TopDownPlayerMovement>(true);
        PlayerInteraction legacyPlayerInteraction = Object.FindObjectOfType<PlayerInteraction>(true);
        if (legacyPlayerInteraction != null)
        {
            legacyPlayerInteraction.enabled = false;
            EditorUtility.SetDirty(legacyPlayerInteraction);
        }

        SetObjectReference(progression, "receptionist", receptionist);
        SetObjectReference(progression, "vendingConversation", vendingConversation);
        SetObjectReference(progression, "objectiveUI", objectiveUI);
        SetObjectReference(progression, "objectiveArrow", arrow);
        SetObjectReference(progression, "workDesk", desk);
        SetObjectReference(progression, "embeddedComputerRoot", embeddedComputer);
        SetObjectReference(progression, "playerMovement", movement);

        SetObjectReference(desk, "progression", progression);
        SetObjectReference(desk, "interactPrompt", deskPrompt);

        EditorUtility.SetDirty(flowRoot);
        EditorSceneManager.MarkSceneDirty(mapScene);
        EditorSceneManager.SaveScene(mapScene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = flowRoot;
        Debug.Log("[Topic4MainSceneSetup] Topic 4 map flow created and saved.");
    }

    [MenuItem("Game/Setup Topic 4 - Map Colliders")]
    public static void SetupMapColliders()
    {
        string[] solidInteriorTilemaps =
        {
            "desk",
            "separator desk",
            "bangku",
            "bangku layer 4",
            "tanaman",
            "taneman atas"
        };

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MapPath)
            scene = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Single);

        Rect[] automaticDoorCutouts =
        {
            new Rect(18.75f, 7.15f, 2.5f, 2.8f)
        };

        CreateCollisionProxy("wall", automaticDoorCutouts);
        CreateCollisionProxy("top wall", automaticDoorCutouts);

        foreach (string tilemapName in solidInteriorTilemaps)
        {
            Tilemap tilemap = Object.FindObjectsOfType<Tilemap>(true)
                .FirstOrDefault(candidate => candidate.name == tilemapName);
            if (tilemap == null)
            {
                Debug.LogWarning($"[Topic4MainSceneSetup] Tilemap '{tilemapName}' was not found.");
                continue;
            }

            ConfigureSolidTilemap(tilemap);
        }

        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Topic4MainSceneSetup] Wall and solid interior colliders configured.");
    }

    private static void CreateCollisionProxy(string sourceName, Rect[] worldCutouts)
    {
        Tilemap source = Object.FindObjectsOfType<Tilemap>(true)
            .FirstOrDefault(candidate => candidate.name == sourceName);
        if (source == null)
        {
            Debug.LogWarning($"[Topic4MainSceneSetup] Tilemap '{sourceName}' was not found.");
            return;
        }

        RemoveCollisionComponents(source.gameObject);

        string proxyName = sourceName + " Collision";
        Transform existing = source.transform.parent != null
            ? source.transform.parent.Find(proxyName)
            : null;
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject proxyObject = new GameObject(proxyName, typeof(Tilemap), typeof(TilemapRenderer));
        proxyObject.transform.SetParent(source.transform.parent, false);
        proxyObject.transform.localPosition = source.transform.localPosition;
        proxyObject.transform.localRotation = source.transform.localRotation;
        proxyObject.transform.localScale = source.transform.localScale;

        Tilemap proxy = proxyObject.GetComponent<Tilemap>();
        proxy.tileAnchor = source.tileAnchor;
        proxy.orientation = source.orientation;
        BoundsInt bounds = source.cellBounds;
        proxy.SetTilesBlock(bounds, source.GetTilesBlock(bounds));

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (!proxy.HasTile(position))
                continue;

            Vector3 worldCenter = source.GetCellCenterWorld(position);
            if (worldCutouts.Any(cutout => cutout.Contains(worldCenter)))
                proxy.SetTile(position, null);
        }

        proxyObject.GetComponent<TilemapRenderer>().enabled = false;
        proxy.RefreshAllTiles();
        ConfigureSolidTilemap(proxy);
    }

    private static void ConfigureSolidTilemap(Tilemap tilemap)
    {
        Rigidbody2D body = tilemap.GetComponent<Rigidbody2D>();
        if (body == null)
            body = tilemap.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

        TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
            tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.isTrigger = false;
        tilemapCollider.usedByComposite = true;
        tilemapCollider.extrusionFactor = 0.01f;

        EditorUtility.SetDirty(tilemap.gameObject);
    }

    private static void RemoveCollisionComponents(GameObject target)
    {
        TilemapCollider2D tilemapCollider = target.GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
            Object.DestroyImmediate(tilemapCollider);

        CompositeCollider2D composite = target.GetComponent<CompositeCollider2D>();
        if (composite != null)
            Object.DestroyImmediate(composite);

        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if (body != null)
            Object.DestroyImmediate(body);
    }

    private static VNDialogueManager CopyTopic5VNCanvas(Scene mapScene, out GameObject arrowTemplate)
    {
        Scene sourceScene = EditorSceneManager.OpenScene(Topic5MapPath, OpenSceneMode.Additive);
        GameObject sourceCanvas = sourceScene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "VN_Canvas");

        if (sourceCanvas == null)
            throw new System.InvalidOperationException("VN_Canvas was not found in Topic 5 map.");

        GameObject canvasClone = Object.Instantiate(sourceCanvas);
        canvasClone.name = "VN_Canvas";
        SceneManager.MoveGameObjectToScene(canvasClone, mapScene);

        foreach (Topic5VNMovementGuard guard in canvasClone.GetComponentsInChildren<Topic5VNMovementGuard>(true))
            Object.DestroyImmediate(guard);

        VNDialogueManager manager = canvasClone.GetComponentInChildren<VNDialogueManager>(true);
        if (manager == null)
            throw new System.InvalidOperationException("Copied VN_Canvas has no VNDialogueManager.");

        Topic4VNMovementGuard movementGuard = manager.gameObject.GetComponent<Topic4VNMovementGuard>();
        if (movementGuard == null)
            movementGuard = manager.gameObject.AddComponent<Topic4VNMovementGuard>();
        SetObjectReference(movementGuard, "dialogueManager", manager);

        FollowArrow_Tp1 sourceArrow = sourceScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FollowArrow_Tp1>(true))
            .FirstOrDefault();
        arrowTemplate = sourceArrow != null ? Object.Instantiate(sourceArrow.gameObject) : null;
        if (arrowTemplate != null)
            SceneManager.MoveGameObjectToScene(arrowTemplate, mapScene);

        EditorSceneManager.CloseScene(sourceScene, true);
        return manager;
    }

    private static GameObject EmbedComputerScene(Scene mapScene)
    {
        Scene sourceScene = EditorSceneManager.OpenScene(ComputerPath, OpenSceneMode.Additive);
        GameObject container = new GameObject("EmbeddedComputerInteraction");
        SceneManager.MoveGameObjectToScene(container, mapScene);

        EventSystem existingEventSystem = Object.FindObjectsOfType<EventSystem>(true)
            .FirstOrDefault(system => system.gameObject.scene == mapScene);

        foreach (GameObject root in sourceScene.GetRootGameObjects().ToArray())
        {
            if (root.name == "Main Camera" || root.name == "Directional Light")
                continue;

            SceneManager.MoveGameObjectToScene(root, mapScene);

            if (root.GetComponent<EventSystem>() != null)
            {
                if (existingEventSystem == null)
                {
                    root.name = "EventSystem";
                    existingEventSystem = root.GetComponent<EventSystem>();
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
                continue;
            }

            root.transform.SetParent(container.transform, true);
        }

        EditorSceneManager.CloseScene(sourceScene, true);
        container.SetActive(false);
        return container;
    }

    private static ObjectiveUI_Tp4 CreateObjectiveCanvas(GameObject arrowTemplate, out FollowArrow_Tp1 arrow)
    {
        GameObject canvasObject = new GameObject(
            "Topic4ObjectiveCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(650f, 88f);
        panel.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.08f, 0.9f);

        GameObject textObject = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 12f);
        textRect.offsetMax = new Vector2(-24f, -12f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "Objective";
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;

        ObjectiveUI_Tp4 objective = panel.AddComponent<ObjectiveUI_Tp4>();
        objective.objectiveText = text;
        objective.panelRoot = panel;
        objective.highlightDuration = 1.4f;
        objective.moveDuration = 0.35f;
        objective.centerPosition = new Vector2(0f, 350f);
        objective.cornerPosition = new Vector2(-24f, -24f);
        objective.centerSize = new Vector2(760f, 100f);
        objective.cornerSize = new Vector2(650f, 88f);

        GameObject arrowObject = arrowTemplate;
        if (arrowObject == null)
        {
            arrowObject = new GameObject("ObjectiveArrow", typeof(RectTransform), typeof(Image));
            Image fallbackImage = arrowObject.GetComponent<Image>();
            fallbackImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fallbackImage.color = new Color(0.15f, 0.95f, 0.45f, 1f);
        }

        arrowObject.name = "ObjectiveArrow";
        arrowObject.transform.SetParent(canvasObject.transform, false);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(64f, 64f);
        arrow = arrowObject.GetComponent<FollowArrow_Tp1>();
        if (arrow == null)
            arrow = arrowObject.AddComponent<FollowArrow_Tp1>();
        arrow.arrowImage = arrowObject.GetComponent<Image>();
        arrow.displayMode = FollowArrow_Tp1.ArrowDisplayMode.ScreenEdge;
        arrow.screenEdgeMargin = 68f;
        arrow.hideWhenTargetVisible = true;
        arrow.hideWithinWorldDistance = 1.2f;
        arrowObject.SetActive(false);

        return objective;
    }

    private static VNNPCInteractable CreateNpc(
        string name,
        Vector3 position,
        Sprite sprite,
        VNDialogueData dialogue,
        VNDialogueManager manager,
        Transform parent)
    {
        GameObject npc = CreateVisualNpc(name, position, sprite, parent);
        BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.offset = new Vector2(0f, 0.15f);
        collider.size = new Vector2(1.7f, 1.8f);

        GameObject prompt = CreatePrompt(npc.transform);
        VNNPCInteractable interactable = npc.AddComponent<VNNPCInteractable>();
        interactable.interactPrompt = prompt;
        interactable.vnDialogue = dialogue;
        interactable.manager = manager;
        interactable.autoTrigger = false;
        interactable.oneTimeOnly = true;
        interactable.isInteractable = true;
        return interactable;
    }

    private static GameObject CreateVisualNpc(string name, Vector3 position, Sprite sprite, Transform parent)
    {
        GameObject npc = new GameObject(name, typeof(SpriteRenderer));
        npc.transform.SetParent(parent, false);
        npc.transform.position = position;
        SpriteRenderer renderer = npc.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 15;
        return npc;
    }

    private static GameObject CreatePrompt(Transform parent)
    {
        GameObject prompt = new GameObject("Prompt", typeof(RectTransform), typeof(Canvas), typeof(Image));
        prompt.transform.SetParent(parent, false);
        RectTransform rect = prompt.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(270f, 48f);
        rect.localScale = Vector3.one * 0.008f;
        rect.localPosition = new Vector3(0f, 1.35f, 0f);

        Canvas canvas = prompt.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;
        prompt.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.055f, 0.92f);

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(prompt.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 4f);
        labelRect.offsetMax = new Vector2(-10f, -4f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "Tekan E untuk berinteraksi";
        text.fontSize = 22f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        prompt.SetActive(false);
        return prompt;
    }

    private static Sprite LoadFirstSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void DeleteGeneratedRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }
}
