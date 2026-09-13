// One-shot Editor tool: builds the interact-key dialogue prototype
// (Player + DialogueRunner + a Cube wired as an NPCDialogue target)
// inside the currently open scene. Run once from Tools menu, then
// save the scene (Ctrl+S).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public static class PrototypeDialogueSetup
{
    private const string PlayerPrefabPath = "Assets/[02]Prefeb/Player/TzPlayer/Player.prefab";
    private const string YarnProjectPath = "Assets/[06]Dialogue/Bruno_Dialogue/NPC_Project.yarnproject";
    private const string YarnStartNode = "Bruno_Upgrade";

    [MenuItem("Tools/Prototype Dialogue/Setup Bruno Cube In Scene")]
    private static void Setup()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Player prefab not found at {PlayerPrefabPath}");
                return;
            }
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(0f, 1f, -3f);
            Undo.RegisterCreatedObjectUndo(player, "Spawn Player");
        }

        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>();
        if (runner == null)
        {
            GameObject runnerObj = new GameObject("DialogueRunner");
            runner = runnerObj.AddComponent<DialogueRunner>();
            Undo.RegisterCreatedObjectUndo(runnerObj, "Create DialogueRunner");

            YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(YarnProjectPath);
            if (project == null)
            {
                Debug.LogError($"YarnProject not found at {YarnProjectPath}");
            }
            else
            {
                SerializedObject runnerSO = new SerializedObject(runner);
                runnerSO.FindProperty("yarnProject").objectReferenceValue = project;
                runnerSO.ApplyModifiedProperties();
            }
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Bruno_DialogueCube";
        cube.transform.position = new Vector3(0f, 0.5f, 0f);
        Undo.RegisterCreatedObjectUndo(cube, "Create Dialogue Cube");

        // Wider than the default 1x1x1 cube so the interact trigger reaches farther than the visible mesh.
        BoxCollider box = cube.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 3f, 3f);

        GameObject talkPoint = new GameObject("TalkPoint");
        talkPoint.transform.SetParent(cube.transform);
        talkPoint.transform.localPosition = new Vector3(0f, 0f, -1.5f);
        talkPoint.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        NPCDialogue dialogue = cube.AddComponent<NPCDialogue>();
        SerializedObject dialogueSO = new SerializedObject(dialogue);
        dialogueSO.FindProperty("yarnStartNode").stringValue = YarnStartNode;
        dialogueSO.FindProperty("dialogueRunner").objectReferenceValue = runner;
        dialogueSO.FindProperty("talkPoint").objectReferenceValue = talkPoint.transform;
        dialogueSO.ApplyModifiedProperties();

        Selection.activeGameObject = cube;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("Prototype dialogue cube ready: Player, DialogueRunner, and Bruno_DialogueCube wired. Save the scene (Ctrl+S).");
    }
}
