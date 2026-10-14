#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Membangun seluruh main menu Fish Hunt otomatis:
/// Canvas, background langit+laut, judul, 4 tombol teks, panel Settings, fade, EventSystem,
/// dan menyambungkan semuanya ke MainMenuController.
///
/// Letakkan file ini di folder "Assets/Editor".
/// Menu: Tools > Fish Hunt > ...
/// </summary>
public static class FishHuntMainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";

    private static readonly Color SkyColor = new Color(0.04f, 0.05f, 0.18f, 1f);
    private static readonly Color WaterColor = new Color(0.08f, 0.55f, 0.75f, 1f);
    private static readonly Color TextNormal = new Color(0.78f, 0.88f, 1f, 0.85f);

    // ---------- Menu Editor ----------

    [MenuItem("Tools/Fish Hunt/Create Main Menu (Current Scene)")]
    private static void CreateInCurrentScene()
    {
        Build();
    }

    [MenuItem("Tools/Fish Hunt/Create Main Menu (New Scene)")]
    private static void CreateInNewScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Kamera 3D template: jadikan warna solid supaya tidak muncul skybox di belakang UI.
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
        }

        if (!Build()) return;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        string path = AssetDatabase.GenerateUniqueAssetPath(ScenePath);
        EditorSceneManager.SaveScene(scene, path);
        AddSceneToBuildSettings(path);

        Debug.Log("[Fish Hunt] Scene main menu disimpan di: " + path);
    }

    // ---------- Builder ----------

    private static bool Build()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog(
                "TextMeshPro belum siap",
                "Import dulu TMP Essential Resources lewat:\nWindow > TextMeshPro > Import TMP Essential Resources\n\nLalu jalankan menu ini lagi.",
                "OK");
            return false;
        }

        if (Object.FindFirstObjectByType<MainMenuController>() != null)
        {
            EditorUtility.DisplayDialog("Sudah ada",
                "Scene ini sudah punya MainMenuController. Hapus dulu kalau mau membuat ulang.", "OK");
            return false;
        }

        // --- Canvas ---
        var canvasGO = new GameObject("MainMenuCanvas", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Main Menu");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Transform root = canvasGO.transform;

        // --- Background: langit malam + laut ---
        var sky = CreateImage("Background_Sky", root, SkyColor);
        Stretch(sky.rectTransform);
        sky.raycastTarget = false;

        var water = CreateImage("Background_Water", root, WaterColor);
        SetAnchors(water.rectTransform, new Vector2(0, 0), new Vector2(1, 0.45f));
        water.rectTransform.offsetMin = Vector2.zero;
        water.rectTransform.offsetMax = Vector2.zero;
        water.raycastTarget = false;

        // --- Judul (placeholder: ganti dengan gambar logo Fish Hunt) ---
        var title = CreateText("Title", root, "FISH HUNT", 150, Color.white);
        title.fontStyle = FontStyles.Bold;
        title.raycastTarget = false;
        PlaceCentered(title.rectTransform, new Vector2(0.74f, 0.76f), new Vector2(1000, 250));

        // --- Tombol menu ---
        string[] names = { "NEW GAME", "CONTINUE", "SETTINGS", "EXIT" };
        float[] ys = { 0.42f, 0.34f, 0.26f, 0.18f };
        var labels = new TMP_Text[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var t = CreateText("Button_" + names[i].Replace(" ", ""), root, names[i], 56, TextNormal);
            PlaceCentered(t.rectTransform, new Vector2(0.77f, ys[i]), new Vector2(700, 80));
            labels[i] = t;
        }

        // --- Panel Settings ---
        var panelImg = CreateImage("SettingsPanel", root, new Color(0f, 0f, 0f, 0.85f));
        Stretch(panelImg.rectTransform);
        Transform panel = panelImg.transform;

        var sTitle = CreateText("SettingsTitle", panel, "SETTINGS", 90, Color.white);
        sTitle.raycastTarget = false;
        PlaceCentered(sTitle.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(900, 140));

        var vLabel = CreateText("VolumeLabel", panel, "VOLUME", 48, TextNormal);
        vLabel.raycastTarget = false;
        PlaceCentered(vLabel.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(600, 70));

        Slider slider = CreateSlider(panel);
        PlaceCentered((RectTransform)slider.transform, new Vector2(0.5f, 0.48f), new Vector2(700, 40));

        var back = CreateText("BackButton", panel, "BACK", 56, Color.white);
        PlaceCentered(back.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(500, 80));
        var backBtn = back.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = back;
        var colors = backBtn.colors;
        colors.highlightedColor = new Color(0.8f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.75f, 0.95f, 1f);
        backBtn.colors = colors;

        panelImg.gameObject.SetActive(false);

        // --- Fade overlay (paling atas) ---
        var fadeImg = CreateImage("FadeOverlay", root, Color.black);
        Stretch(fadeImg.rectTransform);
        fadeImg.raycastTarget = false;
        var fade = fadeImg.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        fade.interactable = false;

        // --- EventSystem ---
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
#if ENABLE_INPUT_SYSTEM
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // --- Controller + sambungan referensi ---
        var controllerGO = new GameObject("MainMenu");
        Undo.RegisterCreatedObjectUndo(controllerGO, "Create Main Menu");
        var controller = controllerGO.AddComponent<MainMenuController>();

        var so = new SerializedObject(controller);
        SerializedProperty itemsProp = so.FindProperty("items");
        for (int i = 0; i < labels.Length && i < itemsProp.arraySize; i++)
        {
            itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("label").objectReferenceValue = labels[i];
        }
        so.FindProperty("settingsPanel").objectReferenceValue = panelImg.gameObject;
        so.FindProperty("volumeSlider").objectReferenceValue = slider;
        so.FindProperty("settingsBackButton").objectReferenceValue = backBtn;
        so.FindProperty("fadeGroup").objectReferenceValue = fade;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = controllerGO;

        Debug.Log("[Fish Hunt] Main menu dibuat. Jangan lupa isi 'Game Scene Name' di MainMenuController.");
        return true;
    }

    // ---------- Helper UI ----------

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = true;
        return t;
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "VolumeSlider";
        go.transform.SetParent(parent, false);

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        Image bg = go.transform.Find("Background").GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.25f);
        Image fill = go.transform.Find("Fill Area/Fill").GetComponent<Image>();
        fill.color = new Color(0.5f, 0.8f, 1f, 1f);
        Image handle = go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
        handle.color = Color.white;
        handle.rectTransform.sizeDelta = new Vector2(30f, 0f);

        return slider;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
    }

    private static void PlaceCentered(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == path) return;

        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        list[0] = new EditorBuildSettingsScene(path, true); // main menu jadi scene pertama
        for (int i = 0; i < scenes.Length; i++) list[i + 1] = scenes[i];
        EditorBuildSettings.scenes = list;
    }
}
#endif
