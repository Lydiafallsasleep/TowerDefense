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
        
        Debug.Log($"发现 {missingCount} 个缺失的脚本引用");
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
        
        Debug.Log($"在选定对象中发现 {missingCount} 个缺失的脚本引用");
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
                Debug.Log($"预制体 '{path}' 有 {prefabMissingCount} 个缺失脚本", prefab);
            }
            missingCount += prefabMissingCount;
        }
        
        Debug.Log($"检查了 {checkedCount} 个预制体，发现 {missingCount} 个缺失的脚本引用");
    }
    
    static int FindMissingScriptsInGameObject(GameObject go)
    {
        int missingCount = 0;
        Component[] components = go.GetComponents<Component>();
        
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"游戏对象 '{go.name}' 有缺失的脚本引用", go);
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
        if (!EditorUtility.DisplayDialog("危险操作", 
            "此操作将移除场景中所有对象上的缺失脚本引用。这个操作不可撤销。是否继续？", 
            "确认", "取消"))
        {
            return;
        }
        
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int removedCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            removedCount += RemoveMissingScriptsInGameObject(go);
        }
        
        Debug.Log($"移除了 {removedCount} 个缺失的脚本引用");
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