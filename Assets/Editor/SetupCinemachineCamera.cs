using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetupCinemachineCamera
{
    [MenuItem("Tools/Setup Cinemachine Follow Camera")]
    public static void Setup()
    {
        // 1. Get Floor tilemap bounds
        var gridObj = GameObject.Find("Grid");
        if (gridObj == null) { Debug.LogError("[CamSetup] Grid not found."); return; }

        Tilemap floorTilemap = null;
        foreach (Transform child in gridObj.transform)
        {
            if (child.name == "Floor")
            {
                floorTilemap = child.GetComponent<Tilemap>();
                break;
            }
        }
        if (floorTilemap == null) { Debug.LogError("[CamSetup] Floor tilemap not found."); return; }

        floorTilemap.CompressBounds();
        BoundsInt cellBounds = floorTilemap.cellBounds;
        Vector3 worldMin = floorTilemap.CellToWorld(cellBounds.min);
        Vector3 worldMax = floorTilemap.CellToWorld(cellBounds.max);

        float minX = worldMin.x;
        float minY = worldMin.y;
        float maxX = worldMax.x;
        float maxY = worldMax.y;
        Debug.Log($"[CamSetup] Map bounds: ({minX:F2},{minY:F2}) to ({maxX:F2},{maxY:F2})");

        // 2. Create CameraConfiner with PolygonCollider2D
        var existing = GameObject.Find("CameraConfiner");
        if (existing != null) Object.DestroyImmediate(existing);

        var confinerObj = new GameObject("CameraConfiner");
        var poly = confinerObj.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.SetPath(0, new Vector2[]
        {
            new Vector2(minX, minY),
            new Vector2(maxX, minY),
            new Vector2(maxX, maxY),
            new Vector2(minX, maxY)
        });

        // 3. Add CinemachineBrain to Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null) { Debug.LogError("[CamSetup] Main Camera not found."); return; }
        mainCam.orthographic = true;
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();

        // 4. Create CinemachineVirtualCamera
        var existingVcam = GameObject.Find("CM_FollowPlayer");
        if (existingVcam != null) Object.DestroyImmediate(existingVcam);

        var vcamObj = new GameObject("CM_FollowPlayer");
        vcamObj.transform.position = new Vector3(0f, 0f, -10f);
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();

        var player = GameObject.Find("Player");
        if (player != null)
            vcam.Follow = player.transform;
        else
            Debug.LogWarning("[CamSetup] Player not found – set Follow manually.");

        float orthoSize = (mainCam.orthographicSize > 0) ? mainCam.orthographicSize : 5f;
        vcam.m_Lens.Orthographic = true;
        vcam.m_Lens.OrthographicSize = orthoSize;
        vcam.m_Lens.NearClipPlane = -100f;
        vcam.m_Lens.FarClipPlane = 100f;

        // 5. Body: Framing Transposer (2D follow)
        var transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_CameraDistance = 10f;
        transposer.m_DeadZoneWidth = 0.05f;
        transposer.m_DeadZoneHeight = 0.05f;
        transposer.m_SoftZoneWidth = 0.8f;
        transposer.m_SoftZoneHeight = 0.8f;
        transposer.m_XDamping = 0.5f;
        transposer.m_YDamping = 0.5f;

        // 6. Add Confiner extension
        var confiner = vcamObj.AddComponent<CinemachineConfiner>();
        confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        confiner.m_BoundingShape2D = poly;
        confiner.m_ConfineScreenEdges = true;
        confiner.InvalidatePathCache();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = vcamObj;
        Debug.Log("[CamSetup] Done! CM_FollowPlayer created and configured.");
    }
}
