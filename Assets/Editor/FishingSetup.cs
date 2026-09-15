// One-shot Editor tool: wires the fishing minigame into the currently
// open scene (Player already has fishing components attached; a
// FishingGameManager + QTE/CameraRig/DebugHud are created and cross-
// referenced automatically). Run once from Tools menu, then save
// the scene (Ctrl+S).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public static class FishingSetup
{
    private const string LeftShoulderPrefabPath = "Assets/[02]Prefeb/Cinemachine/CM_LeftShoulder.prefab";
    private const string RightShoulderPrefabPath = "Assets/[02]Prefeb/Cinemachine/CM_RightShoulder.prefab";
    private const string OverHeadPrefabPath = "Assets/[02]Prefeb/Cinemachine/CM_OverHead.prefab";

    [MenuItem("Tools/Fishing/Setup Fishing Manager In Scene")]
    private static void Setup()
    {
        if (GameObject.Find("FishingGameManager") != null)
        {
            Debug.LogWarning("A 'FishingGameManager' already exists in this scene. Delete it first if you want to rebuild.");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("No GameObject tagged 'Player' found. Add the Player to the scene first.");
            return;
        }

        CinemachineCamera mainCam = Object.FindFirstObjectByType<CinemachineCamera>();

        CinemachineCamera left = InstantiateCam(LeftShoulderPrefabPath);
        CinemachineCamera right = InstantiateCam(RightShoulderPrefabPath);
        CinemachineCamera overHead = InstantiateCam(OverHeadPrefabPath);

        GameObject managerObj = new GameObject("FishingGameManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create FishingGameManager");

        var qte = managerObj.AddComponent<FishingQTEManager>();
        var rig = managerObj.AddComponent<FishingCameraRig>();
        var manager = managerObj.AddComponent<FishingGameManager>();
        var hud = managerObj.AddComponent<FishingDebugHud>();

        var reel = playerObj.GetComponent<PlayerReelController>();
        if (reel == null) reel = playerObj.AddComponent<PlayerReelController>();

        var playerFishing = playerObj.GetComponent<PlayerNormal.Project_wide.PlayerFishing>();
        if (playerFishing == null) playerFishing = playerObj.AddComponent<PlayerNormal.Project_wide.PlayerFishing>();

        FishDatabase database = Object.FindFirstObjectByType<FishDatabase>();
        if (database == null)
        {
            GameObject dbObj = new GameObject("FishDatabase");
            Undo.RegisterCreatedObjectUndo(dbObj, "Create FishDatabase");
            database = dbObj.AddComponent<FishDatabase>();
        }

        // Wire the FishDatabase into any FishZone in the scene that doesn't have one yet
        // (e.g. "FishZone_Com" already placed in Scene2 before this manager existed).
        FishZone zone = Object.FindFirstObjectByType<FishZone>();
        if (zone != null)
        {
            SerializedObject zoneSO = new SerializedObject(zone);
            SerializedProperty dbProp = zoneSO.FindProperty("fishDatabase");
            if (dbProp.objectReferenceValue == null)
            {
                dbProp.objectReferenceValue = database;
                zoneSO.ApplyModifiedProperties();
            }
        }

        SerializedObject rigSO = new SerializedObject(rig);
        rigSO.FindProperty("leftShoulderCamera").objectReferenceValue = left;
        rigSO.FindProperty("rightShoulderCamera").objectReferenceValue = right;
        rigSO.FindProperty("overHeadCamera").objectReferenceValue = overHead;
        rigSO.FindProperty("mainPlayerCamera").objectReferenceValue = mainCam;
        rigSO.FindProperty("player").objectReferenceValue = playerObj.transform;
        rigSO.ApplyModifiedProperties();

        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("player").objectReferenceValue = playerObj.transform;
        managerSO.FindProperty("reelController").objectReferenceValue = reel;
        managerSO.FindProperty("qteManager").objectReferenceValue = qte;
        managerSO.FindProperty("cameraRig").objectReferenceValue = rig;
        managerSO.ApplyModifiedProperties();

        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("gameManager").objectReferenceValue = manager;
        hudSO.FindProperty("reelController").objectReferenceValue = reel;
        hudSO.ApplyModifiedProperties();

        SerializedObject pfSO = new SerializedObject(playerFishing);
        pfSO.FindProperty("fishingGameManager").objectReferenceValue = manager;
        pfSO.ApplyModifiedProperties();

        Selection.activeGameObject = managerObj;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Fishing setup ready: FishingGameManager (+QTE/CameraRig/DebugHud) created; PlayerReelController + PlayerFishing added to Player; FishDatabase wired to any FishZone found. Save the scene (Ctrl+S).");
    }

    private static CinemachineCamera InstantiateCam(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath}");
            return null;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, $"Create {prefab.name}");
        return instance.GetComponent<CinemachineCamera>();
    }
}
