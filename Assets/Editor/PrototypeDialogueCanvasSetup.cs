// One-shot Editor tool: builds a working Yarn Spinner dialogue Canvas
// (LinePresenter + OptionsPresenter + OptionItem prefab) in the
// currently open scene and wires it to the scene's DialogueRunner.
// Run once from Tools menu, then save the scene (Ctrl+S).
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public static class PrototypeDialogueCanvasSetup
{
    private const string YarnProjectPath = "Assets/[06]Dialogue/Bruno_Dialogue/NPC_Project.yarnproject";
    private const string OptionItemPrefabDir = "Assets/[06]Dialogue/UI";
    private const string OptionItemPrefabPath = OptionItemPrefabDir + "/OptionItem.prefab";

    [MenuItem("Tools/Prototype Dialogue/Setup Dialogue Canvas")]
    private static void Setup()
    {
        if (GameObject.Find("DialogueCanvas") != null)
        {
            Debug.LogWarning("A 'DialogueCanvas' already exists in this scene. Delete it first if you want to rebuild.");
            return;
        }

        EnsureEventSystem();
        DialogueRunner runner = EnsureDialogueRunner();

        // ---- Canvas root ----
        GameObject canvasGO = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Dialogue Canvas");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ---- Dialogue (line) panel ----
        RectTransform linePanel = CreateUI("DialoguePanel", canvasGO.transform);
        linePanel.anchorMin = new Vector2(0f, 0f);
        linePanel.anchorMax = new Vector2(1f, 0f);
        linePanel.pivot = new Vector2(0.5f, 0f);
        linePanel.sizeDelta = new Vector2(0f, 220f);
        linePanel.anchoredPosition = new Vector2(0f, 40f);
        Image linePanelBg = linePanel.gameObject.AddComponent<Image>();
        linePanelBg.color = new Color(0f, 0f, 0f, 0.75f);
        CanvasGroup lineCanvasGroup = linePanel.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI nameText = CreateText("CharacterNameText", linePanel, "Name", 28, FontStyles.Bold);
        RectTransform nameRT = nameText.rectTransform;
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(0f, 1f);
        nameRT.pivot = new Vector2(0f, 1f);
        nameRT.anchoredPosition = new Vector2(24f, -12f);
        nameRT.sizeDelta = new Vector2(400f, 40f);

        TextMeshProUGUI lineText = CreateText("LineText", linePanel, "", 24, FontStyles.Normal);
        RectTransform lineRT = lineText.rectTransform;
        lineRT.anchorMin = Vector2.zero;
        lineRT.anchorMax = Vector2.one;
        lineRT.offsetMin = new Vector2(24f, 50f);
        lineRT.offsetMax = new Vector2(-24f, -50f);

        GameObject continueButtonGO = CreateUI("ContinueButton", linePanel).gameObject;
        RectTransform continueRT = continueButtonGO.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(1f, 0f);
        continueRT.anchorMax = new Vector2(1f, 0f);
        continueRT.pivot = new Vector2(1f, 0f);
        continueRT.anchoredPosition = new Vector2(-24f, 16f);
        continueRT.sizeDelta = new Vector2(160f, 44f);
        Image continueBg = continueButtonGO.AddComponent<Image>();
        continueBg.color = new Color(1f, 1f, 1f, 0.15f);
        Button continueButton = continueButtonGO.AddComponent<Button>();
        continueButton.targetGraphic = continueBg;
        TextMeshProUGUI continueLabel = CreateText("Label", continueButtonGO.transform, "Continue ▶", 20, FontStyles.Normal);
        continueLabel.alignment = TextAlignmentOptions.Center;
        RectTransform continueLabelRT = continueLabel.rectTransform;
        continueLabelRT.anchorMin = Vector2.zero;
        continueLabelRT.anchorMax = Vector2.one;
        continueLabelRT.offsetMin = Vector2.zero;
        continueLabelRT.offsetMax = Vector2.zero;

        LinePresenter linePresenter = linePanel.gameObject.AddComponent<LinePresenter>();
        linePresenter.canvasGroup = lineCanvasGroup;
        linePresenter.lineText = lineText;
        linePresenter.characterNameText = nameText;
        linePresenter.characterNameContainer = nameText.gameObject;

        LinePresenterButtonHandler buttonHandler = continueButtonGO.AddComponent<LinePresenterButtonHandler>();
        SerializedObject buttonHandlerSO = new SerializedObject(buttonHandler);
        buttonHandlerSO.FindProperty("continueButton").objectReferenceValue = continueButton;
        buttonHandlerSO.FindProperty("dialogueRunner").objectReferenceValue = runner;
        buttonHandlerSO.ApplyModifiedProperties();

        SerializedObject linePresenterSO = new SerializedObject(linePresenter);
        SerializedProperty eventHandlers = linePresenterSO.FindProperty("eventHandlers");
        eventHandlers.arraySize = 1;
        eventHandlers.GetArrayElementAtIndex(0).objectReferenceValue = buttonHandler;
        linePresenterSO.ApplyModifiedProperties();

        // ---- Options panel ----
        RectTransform optionsPanel = CreateUI("OptionsPanel", canvasGO.transform);
        optionsPanel.anchorMin = new Vector2(0.5f, 0f);
        optionsPanel.anchorMax = new Vector2(0.5f, 0f);
        optionsPanel.pivot = new Vector2(0.5f, 0f);
        optionsPanel.anchoredPosition = new Vector2(0f, 260f);
        optionsPanel.sizeDelta = new Vector2(640f, 0f);
        CanvasGroup optionsCanvasGroup = optionsPanel.gameObject.AddComponent<CanvasGroup>();
        VerticalLayoutGroup layout = optionsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = optionsPanel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        OptionItem optionItemPrefab = EnsureOptionItemPrefab();

        OptionsPresenter optionsPresenter = optionsPanel.gameObject.AddComponent<OptionsPresenter>();
        SerializedObject optionsPresenterSO = new SerializedObject(optionsPresenter);
        optionsPresenterSO.FindProperty("canvasGroup").objectReferenceValue = optionsCanvasGroup;
        optionsPresenterSO.FindProperty("optionViewPrefab").objectReferenceValue = optionItemPrefab;
        optionsPresenterSO.ApplyModifiedProperties();

        runner.DialoguePresenters = new List<DialoguePresenterBase> { linePresenter, optionsPresenter };

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Dialogue canvas ready: LinePresenter + OptionsPresenter wired to DialogueRunner. Save the scene (Ctrl+S).");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static DialogueRunner EnsureDialogueRunner()
    {
        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>();
        if (runner != null) return runner;

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
        return runner;
    }

    private static OptionItem EnsureOptionItemPrefab()
    {
        OptionItem existing = AssetDatabase.LoadAssetAtPath<OptionItem>(OptionItemPrefabPath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(OptionItemPrefabDir))
        {
            AssetDatabase.CreateFolder("Assets/[06]Dialogue", "UI");
        }

        GameObject temp = new GameObject("OptionItem", typeof(RectTransform));
        RectTransform rt = temp.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 50f);

        Image bg = temp.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        TextMeshProUGUI text = CreateText("Text", temp.transform, "Option", 22, FontStyles.Normal);
        RectTransform textRT = text.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 4f);
        textRT.offsetMax = new Vector2(-16f, -4f);

        OptionItem optionItem = temp.AddComponent<OptionItem>();
        optionItem.targetGraphic = bg;
        SerializedObject optionItemSO = new SerializedObject(optionItem);
        optionItemSO.FindProperty("text").objectReferenceValue = text;
        optionItemSO.FindProperty("selectionImage").objectReferenceValue = bg;
        optionItemSO.ApplyModifiedProperties();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(temp, OptionItemPrefabPath);
        Object.DestroyImmediate(temp);

        return prefabAsset.GetComponent<OptionItem>();
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, FontStyles style)
    {
        RectTransform rt = CreateUI(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }
}
