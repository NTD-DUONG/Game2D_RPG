using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private const string TargetSceneKey = "TargetSceneToLoad";

    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string gameSceneName = "TrainingArena";

    public void PlayGame()
    {
        GameResultPopupController.DestroyPersistentGameplayObjects();
        Time.timeScale = 1f;
        PlayerPrefs.SetString(TargetSceneKey, gameSceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(loadingSceneName);
    }
}
