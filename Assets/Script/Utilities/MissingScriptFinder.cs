using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public class MissingScriptFinder : MonoBehaviour
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    static void FindMissingScriptsInScene()
    {
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int missingCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            missingCount += FindMissingScriptsInGameObject(go);
        }
        
        Debug.Log($"Found {missingCount} missing script references");
    }
    
    [MenuItem("Tools/Find Missing Scripts in Selected Objects")]
    static void FindMissingScriptsInSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int missingCount = 0;
        
        foreach (GameObject go in selectedObjects)
        {
            missingCount += FindMissingScriptsInGameObject(go);
        }
        
        Debug.Log($"Found {missingCount} missing script references in selected objects");
    }
    
    [MenuItem("Tools/Find Missing Scripts in Prefabs")]
    static void FindMissingScriptsInPrefabs()
    {
        string[] prefabPaths = AssetDatabase.GetAllAssetPaths();
        int missingCount = 0;
        int checkedCount = 0;
        
        foreach (string path in prefabPaths)
        {
            if (!path.ToLower().EndsWith(".prefab")) continue;
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            
            checkedCount++;
            int prefabMissingCount = FindMissingScriptsInGameObject(prefab);
            if (prefabMissingCount > 0)
            {
                Debug.Log($"Prefab '{path}' has {prefabMissingCount} missing scripts", prefab);
            }
            missingCount += prefabMissingCount;
        }
        
        Debug.Log($"Checked {checkedCount} prefabs, found {missingCount} missing script references");
    }
    
    static int FindMissingScriptsInGameObject(GameObject go)
    {
        int missingCount = 0;
        Component[] components = go.GetComponents<Component>();
        
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"GameObject '{go.name}' has missing script references", go);
                missingCount++;
            }
        }
        
        foreach (Transform child in go.transform)
        {
            missingCount += FindMissingScriptsInGameObject(child.gameObject);
        }
        
        return missingCount;
    }
    
    [MenuItem("Tools/Remove Missing Scripts in Scene")]
    static void RemoveMissingScriptsInScene()
    {
        if (!EditorUtility.DisplayDialog("Dangerous Operation",
            "This operation will remove all missing script references from objects in the scene. This action cannot be undone. Do you want to continue?",
            "Confirm", "Cancel"))
        {
            return;
        }
        
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int removedCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            removedCount += RemoveMissingScriptsInGameObject(go);
        }
        
        Debug.Log($"Removed {removedCount} missing script references");
    }
    
    static int RemoveMissingScriptsInGameObject(GameObject go)
    {
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        
        foreach (Transform child in go.transform)
        {
            removedCount += RemoveMissingScriptsInGameObject(child.gameObject);
        }
        
        return removedCount;
    }
}
#endif 