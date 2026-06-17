using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    private const string TargetSceneKey = "TargetSceneToLoad";

    [SerializeField] private string fallbackSceneName = "TrainingArena";
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private float minimumLoadingTime = 1.2f;

    private void Start()
    {
        string targetScene = PlayerPrefs.GetString(TargetSceneKey, fallbackSceneName);
        StartCoroutine(LoadSceneRoutine(targetScene));
    }

    private IEnumerator LoadSceneRoutine(string targetScene)
    {
        float elapsedTime = 0f;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetScene);
        loadOperation.allowSceneActivation = false;

        while (!loadOperation.isDone)
        {
            elapsedTime += Time.deltaTime;
            float sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
            float progress = Mathf.Min(sceneProgress, timeProgress);

            UpdateProgress(progress);

            if (loadOperation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                UpdateProgress(1f);
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateProgress(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
        }
    }
}
