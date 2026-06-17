using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneGenerator
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
    private const string GeneratedFolder = "Assets/Generated/Menu";

    [MenuItem("Tools/Menu/Create Main Menu And Loading Scenes")]
    public static void CreateMenuAndLoadingScenes()
    {
        Directory.CreateDirectory(ScenesFolder);
        Directory.CreateDirectory(GeneratedFolder);

        Sprite menuBackground = CreateBackgroundSprite(
            "MenuBackground",
            new Color(0.95f, 0.55f, 0.2f),
            new Color(0.23f, 0.58f, 0.43f),
            true
        );

        Sprite loadingBackground = CreateBackgroundSprite(
            "LoadingBackground",
            new Color(0.98f, 0.62f, 0.23f),
            new Color(0.15f, 0.46f, 0.36f),
            false
        );

        CreateMainMenuScene(menuBackground);
        CreateLoadingScene(loadingBackground);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Menu scenes created",
            "Created MainMenu and LoadingScene. Open MainMenu and press Play to test.",
            "OK"
        );
    }

    private static void CreateMainMenuScene(Sprite background)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        CreateBackground(canvas.transform, background);
        CreateTitle(canvas.transform);

        GameObject controllerObject = new("MainMenuController");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("loadingSceneName").stringValue = "LoadingScene";
        serializedController.FindProperty("gameSceneName").stringValue = "TrainingArena";
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        CreatePlayButton(canvas.transform, controller);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void CreateLoadingScene(Sprite background)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        CreateBackground(canvas.transform, background);
        CreateTitle(canvas.transform);

        Text loadingText = CreateText(
            "LoadingText",
            canvas.transform,
            "Loading...",
            26,
            TextAnchor.MiddleCenter,
            new Vector2(0f, -150f),
            new Vector2(420f, 60f)
        );

        Slider slider = CreateLoadingSlider(canvas.transform);

        GameObject controllerObject = new("LoadingScreenController");
        LoadingScreenController controller = controllerObject.AddComponent<LoadingScreenController>();
        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("fallbackSceneName").stringValue = "TrainingArena";
        serializedController.FindProperty("progressSlider").objectReferenceValue = slider;
        serializedController.FindProperty("progressText").objectReferenceValue = loadingText;
        serializedController.FindProperty("minimumLoadingTime").floatValue = 1.2f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, LoadingScenePath);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.tag = "MainCamera";
        return camera;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void CreateBackground(Transform parent, Sprite sprite)
    {
        GameObject backgroundObject = new("Background");
        backgroundObject.transform.SetParent(parent, false);
        Image image = backgroundObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void CreateTitle(Transform parent)
    {
        Text title = CreateText(
            "Title",
            parent,
            "TheD.Game",
            88,
            TextAnchor.MiddleCenter,
            new Vector2(250f, 205f),
            new Vector2(700f, 130f)
        );
        title.color = Color.white;
        title.fontStyle = FontStyle.Bold;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
    }

    private static void CreatePlayButton(Transform parent, MainMenuController controller)
    {
        GameObject buttonObject = new("PlayButton");
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.32f, 0.33f, 0.82f);

        Button button = buttonObject.AddComponent<Button>();

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -110f);
        rectTransform.sizeDelta = new Vector2(300f, 90f);

        Text text = CreateText(
            "PlayText",
            buttonObject.transform,
            "Play",
            48,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            new Vector2(300f, 90f)
        );
        text.fontStyle = FontStyle.Bold;

        SerializedObject serializedButton = new(button);
        SerializedProperty calls = serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.arraySize = 1;
        SerializedProperty call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = controller;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(MainMenuController).AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = nameof(MainMenuController.PlayGame);
        call.FindPropertyRelative("m_Mode").enumValueIndex = 1;
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Slider CreateLoadingSlider(Transform parent)
    {
        GameObject root = new("LoadingSlider");
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -205f);
        rootRect.sizeDelta = new Vector2(360f, 28f);

        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        Image background = CreateSliderImage("Background", root.transform, new Color(0.1f, 0.19f, 0.12f, 0.95f));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        GameObject fillArea = new("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        Image fill = CreateSliderImage("Fill", fillArea.transform, new Color(0.7f, 0.92f, 0.35f, 1f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        slider.targetGraphic = fill;
        slider.fillRect = fill.rectTransform;
        return slider;
    }

    private static Image CreateSliderImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = GetDefaultFont();
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;

        RectTransform rectTransform = textComponent.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        return textComponent;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static Sprite CreateBackgroundSprite(string assetName, Color topColor, Color bottomColor, bool addSword)
    {
        const int width = 640;
        const int height = 360;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color rowColor = Color.Lerp(bottomColor, topColor, t);

            for (int x = 0; x < width; x++)
            {
                Color pixelColor = rowColor;

                if (y < 110)
                {
                    pixelColor = Color.Lerp(new Color(0.25f, 0.65f, 0.2f), new Color(0.1f, 0.35f, 0.23f), x / (float)width);
                }

                if (IsHillPixel(x, y, 230, 80, 140, 130) || IsHillPixel(x, y, 450, 95, 210, 100))
                {
                    pixelColor = new Color(0.22f, 0.55f, 0.28f);
                }

                if (IsHillPixel(x, y, 330, 120, 90, 150))
                {
                    pixelColor = new Color(0.16f, 0.44f, 0.38f);
                }

                if (IsCastlePixel(x, y))
                {
                    pixelColor = new Color(0.33f, 0.22f, 0.22f);
                }

                if (addSword && IsSwordPixel(x, y))
                {
                    pixelColor = new Color(0.7f, 0.86f, 0.9f);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        string texturePath = $"{GeneratedFolder}/{assetName}.png";
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(texturePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static bool IsHillPixel(int x, int y, int centerX, int baseY, int width, int height)
    {
        float normalized = Mathf.Abs(x - centerX) / (float)width;
        if (normalized > 1f)
        {
            return false;
        }

        float hillTop = baseY + Mathf.Cos(normalized * Mathf.PI * 0.5f) * height;
        return y < hillTop && y > baseY - 18;
    }

    private static bool IsCastlePixel(int x, int y)
    {
        bool mainTower = x > 292 && x < 350 && y > 150 && y < 230;
        bool sideTower = x > 365 && x < 395 && y > 130 && y < 190;
        bool flag = x > 350 && x < 382 && y > 225 && y < 235;
        return mainTower || sideTower || flag;
    }

    private static bool IsSwordPixel(int x, int y)
    {
        bool blade = x > 120 && x < 160 && y > 60 && y < 300;
        bool handle = x > 100 && x < 180 && y > 108 && y < 135;
        bool grip = x > 134 && x < 146 && y > 20 && y < 110;
        return blade || handle || grip;
    }

    private static void UpdateBuildSettings()
    {
        string[] desiredScenes =
        {
            MenuScenePath,
            LoadingScenePath,
            "Assets/Scenes/TrainingArena.unity",
            "Assets/Scenes/Scene1.unity",
            "Assets/Scenes/Scene2.unity"
        };

        List<EditorBuildSettingsScene> scenes = new();
        foreach (string scenePath in desiredScenes)
        {
            if (File.Exists(scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
