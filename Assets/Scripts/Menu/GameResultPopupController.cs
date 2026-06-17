using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultPopupController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    [SerializeField] private string homeSceneName = MainMenuSceneName;

    private readonly List<TrainingHealth> enemyHealths = new();
    private TrainingHealth playerHealth;
    private TrainingArenaManager arenaManager;
    private GameObject popupRoot;
    private Text resultText;
    private bool resultShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreateController(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateController(scene);
    }

    private static void TryCreateController(Scene scene)
    {
        if (scene.name == MainMenuSceneName || scene.name == "LoadingScene")
        {
            return;
        }

        if (FindFirstObjectByType<GameResultPopupController>() != null)
        {
            return;
        }

        GameObject controllerObject = new("GameResultPopupController");
        controllerObject.AddComponent<GameResultPopupController>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        arenaManager = FindFirstObjectByType<TrainingArenaManager>();

        if (arenaManager != null && arenaManager.UsesTrainingEpisodeReset)
        {
            enabled = false;
            return;
        }

        BuildPopup();
        BindHealthEvents();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        foreach (TrainingHealth enemyHealth in enemyHealths)
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= OnEnemyDied;
            }
        }
    }

    private void BindHealthEvents()
    {
        TrainingHealth[] healths = FindObjectsByType<TrainingHealth>(FindObjectsSortMode.None);
        foreach (TrainingHealth health in healths)
        {
            if (health.GetComponent<PlayerController>() != null)
            {
                playerHealth = health;
                playerHealth.Died += OnPlayerDied;
            }
            else
            {
                enemyHealths.Add(health);
                health.Died += OnEnemyDied;
            }
        }
    }

    private void OnPlayerDied(TrainingHealth health)
    {
        ShowResult("Game Over");
    }

    private void OnEnemyDied(TrainingHealth health)
    {
        if (enemyHealths.Count == 0)
        {
            ShowResult("You Win");
            return;
        }

        foreach (TrainingHealth enemyHealth in enemyHealths)
        {
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                return;
            }
        }

        ShowResult("You Win");
    }

    private void ShowResult(string message)
    {
        if (resultShown)
        {
            return;
        }

        resultShown = true;
        Time.timeScale = 0f;
        resultText.text = message;
        popupRoot.SetActive(true);
    }

    private void PlayAgain()
    {
        StartCoroutine(ReloadCurrentSceneRoutine());
    }

    private void GoHome()
    {
        Time.timeScale = 1f;
        DestroyPersistentGameplayObjects();
        SceneManager.LoadScene(homeSceneName);
    }

    private IEnumerator ReloadCurrentSceneRoutine()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        resultShown = false;
        Time.timeScale = 1f;
        DestroyPersistentGameplayObjects();
        yield return null;
        SceneManager.LoadScene(currentSceneName);
    }

    public static void DestroyPersistentGameplayObjects()
    {
        if (PlayerController.Instance != null)
        {
            Destroy(PlayerController.Instance.gameObject);
        }

        if (ActiveInventory.Instance != null)
        {
            Destroy(ActiveInventory.Instance.gameObject);
        }
    }

    private void BuildPopup()
    {
        EnsureEventSystem();

        Canvas canvas = CreateCanvas();
        popupRoot = new GameObject("GameResultPopup");
        popupRoot.transform.SetParent(canvas.transform, false);

        Image dimmer = CreateImage("Dimmer", popupRoot.transform, new Color(0f, 0f, 0f, 0.45f));
        StretchToFullScreen(dimmer.rectTransform);

        Image panel = CreateImage("Panel", popupRoot.transform, new Color(0.42f, 0.44f, 0.42f, 0.78f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(650f, 430f);

        resultText = CreateText("ResultText", panel.transform, "You Win", 66, new Vector2(0f, 125f), new Vector2(560f, 100f));
        resultText.fontStyle = FontStyle.Bold;

        CreateButton(panel.transform, "Play Again", new Vector2(0f, 15f), PlayAgain);
        CreateButton(panel.transform, "Home", new Vector2(0f, -100f), GoHome);

        popupRoot.SetActive(false);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("GameResultCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = GetDefaultFont();
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        RectTransform rectTransform = textComponent.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        return textComponent;
    }

    private static void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new(label.Replace(" ", "") + "Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.34f, 0.34f, 0.34f, 0.88f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.85f);
        outline.effectDistance = new Vector2(4f, -4f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(240f, 70f);

        Text buttonText = CreateText("Text", buttonObject.transform, label, 34, Vector2.zero, new Vector2(240f, 70f));
        buttonText.fontStyle = FontStyle.Bold;
    }

    private static void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
