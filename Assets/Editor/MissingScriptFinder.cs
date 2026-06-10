using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Missing Scripts/Find In Open Scenes")]
    private static void FindInOpenScenes()
    {
        int missingCount = 0;
        GameObject firstMissingObject = null;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                missingCount += FindMissingScriptsRecursive(rootObject.transform, ref firstMissingObject);
            }
        }

        if (firstMissingObject != null)
        {
            Selection.activeGameObject = firstMissingObject;
            EditorGUIUtility.PingObject(firstMissingObject);
        }

        Debug.Log($"Missing script scan finished. Found {missingCount} missing script component(s).");
    }

    [MenuItem("Tools/Missing Scripts/Remove From Selected Object")]
    private static void RemoveFromSelectedObject()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Select a GameObject before removing missing script components.");
            return;
        }

        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(Selection.activeGameObject);
        Debug.Log($"Removed {removedCount} missing script component(s) from {Selection.activeGameObject.name}.");
    }

    private static int FindMissingScriptsRecursive(Transform target, ref GameObject firstMissingObject)
    {
        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target.gameObject);

        if (missingCount > 0)
        {
            if (firstMissingObject == null)
            {
                firstMissingObject = target.gameObject;
            }

            Debug.LogWarning(
                $"Missing script component found on '{GetPath(target)}' ({missingCount}).",
                target.gameObject
            );
        }

        foreach (Transform child in target)
        {
            missingCount += FindMissingScriptsRecursive(child, ref firstMissingObject);
        }

        return missingCount;
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = $"{target.name}/{path}";
        }

        return path;
    }
}
