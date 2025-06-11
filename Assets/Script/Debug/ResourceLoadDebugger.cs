using UnityEngine;
using System.IO;

public class ResourceLoadDebugger : MonoBehaviour
{
    [Header("调试目标")]
    public string resourcePath = "enemy/Slime";
    public string originalResourceDir = "enemy";

    [Header("测试结果")]
    [TextArea(3, 10)]
    public string loadResults = "";
    
    void Start()
    {
        // 延迟一帧再测试，确保Unity加载完成
        Invoke("TestResourceLoading", 0.5f);
    }
    
    public void TestResourceLoading()
    {
        loadResults = "资源加载诊断开始...\n";
        
        // 测试直接Resources.Load
        GameObject directResult = Resources.Load<GameObject>(resourcePath);
        loadResults += $"直接Resources.Load(\"{resourcePath}\") 结果: {(directResult != null ? "成功" : "失败")}\n";
        
        // 测试添加Resources前缀
        GameObject invalidResult = Resources.Load<GameObject>("Resources/" + resourcePath);
        loadResults += $"错误前缀Resources.Load(\"Resources/{resourcePath}\") 结果: {(invalidResult != null ? "成功" : "失败")}\n";
        
        // 测试ObjectPool方式
        string path = originalResourceDir + "/" + resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
        GameObject poolResult = Resources.Load<GameObject>(path);
        loadResults += $"ObjectPool方式Resources.Load(\"{path}\") 结果: {(poolResult != null ? "成功" : "失败")}\n";

        // 检查资源文件是否存在
        string absolutePath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".prefab");
        loadResults += $"物理文件检查 \"{absolutePath}\" 存在: {File.Exists(absolutePath)}\n";
        
        // 尝试列出所有预制体资源
        loadResults += "Resources目录中的所有预制体资源:\n";
        var allObjects = Resources.LoadAll<GameObject>("");
        foreach (var obj in allObjects)
        {
            loadResults += $"- {obj.name}\n";
        }
        
        // 尝试指定文件夹查找
        var enemyObjects = Resources.LoadAll<GameObject>("enemy");
        loadResults += $"\nenemy文件夹预制体数量: {enemyObjects.Length}\n";
        foreach (var obj in enemyObjects)
        {
            loadResults += $"- {obj.name}\n";
        }

        // 输出诊断信息
        Debug.Log(loadResults);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestResourceLoading();
        }
    }
} 