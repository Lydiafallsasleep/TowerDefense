using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

// 单例模式路径管理器
public class PathManager : Singleton<PathManager>
{
    [Header("路径设置")]
    public bool generateOnStart = true;
    public float generationDelay = 0.5f;
    [Tooltip("如果设为false，只会在路径不存在时创建路径")]
    public bool forceRegeneratePaths = false;
    
    [Header("调试")]
    public bool showDebugInfo = true;
    
    private LandPathGenerator landPathGenerator;
    private WaterPathGenerator waterPathGenerator;
    private bool pathsGenerated = false;

    protected override void Awake()
    {
        base.Awake();
        
        // 查找路径生成器
        landPathGenerator = FindObjectOfType<LandPathGenerator>();
        waterPathGenerator = FindObjectOfType<WaterPathGenerator>();
    }

    void Start()
    {
        if (generateOnStart)
        {
            LogInfo("PathManager启动，准备延迟生成路径");
            StartCoroutine(GeneratePathsWithDelay());
        }
    }
    
    IEnumerator GeneratePathsWithDelay()
    {
        LogInfo($"开始等待生成路径，延迟{generationDelay}秒...");
        yield return new WaitForSeconds(generationDelay);
        
        FindPathGenerators(); // 查找路径生成器
        
        // 检查路径是否已存在
        bool pathsExist = PathsExistAndValid();
        if (pathsExist && !forceRegeneratePaths)
        {
            LogInfo("有效的路径已存在，不再重新生成");
            pathsGenerated = true;
        }
        else
        {
            if (forceRegeneratePaths)
                LogInfo("强制重新生成路径");
            else
                LogInfo("未找到有效路径，开始生成");
                
            GeneratePaths();
        }
    }
    
    // 检查路径是否已存在且有效
    private bool PathsExistAndValid()
    {
        Transform landPath = GameObject.Find("LandPathParent")?.transform;
        Transform waterPath = GameObject.Find("WaterPathParent")?.transform;
        
        bool landValid = landPath != null && landPath.childCount > 1; // 至少需要2个路径点
        bool waterValid = waterPath != null && waterPath.childCount > 1;
        
        LogInfo($"路径检查: 陆地路径 - {(landValid ? "有效" : "无效")}, 水路径 - {(waterValid ? "有效" : "无效")}");
        
        return landValid && waterValid;
    }
    
    // 查找路径生成器组件
    private void FindPathGenerators()
    {
        // 查找路径生成器
        landPathGenerator = FindObjectOfType<LandPathGenerator>();
        if(landPathGenerator == null)
        {
            LogError("未找到陆地路径生成器！创建一个...");
            GameObject landGenObj = new GameObject("LandPathGenerator");
            landPathGenerator = landGenObj.AddComponent<LandPathGenerator>();
        }
        
        waterPathGenerator = FindObjectOfType<WaterPathGenerator>();
        if(waterPathGenerator == null)
        {
            LogError("未找到水路路径生成器！创建一个...");
            GameObject waterGenObj = new GameObject("WaterPathGenerator");
            waterPathGenerator = waterGenObj.AddComponent<WaterPathGenerator>();
        }
        
        LogInfo("路径生成器引用已设置：" + 
            $"LandPathGenerator = {(landPathGenerator != null ? "找到" : "未找到")}, " +
            $"WaterPathGenerator = {(waterPathGenerator != null ? "找到" : "未找到")}");
    }

    public void GeneratePaths()
    {
        LogInfo("开始生成路径...");
        
        // 强制销毁并重建路径父对象
        Transform landParent = ForceRecreatePathParent("LandPathParent");
        Transform waterParent = ForceRecreatePathParent("WaterPathParent");
        
        // 再次查找路径生成器（以防它们不存在）
        if (landPathGenerator == null || waterPathGenerator == null)
        {
            FindPathGenerators();
        }
        
        // 确保路径生成器引用正确的路径父对象
        if (landPathGenerator != null)
        {
            landPathGenerator.pathParent = landParent;
            LogInfo($"已为陆地路径生成器设置父对象: {landParent.name}");
        }
        
        if (waterPathGenerator != null)
        {
            waterPathGenerator.pathParent = waterParent;
            LogInfo($"已为水路路径生成器设置父对象: {waterParent.name}");
        }
        
        // 为路径生成器设置必要的Tilemap组件
        SetupPathGenerator(landPathGenerator, PathType.Land);
        SetupPathGenerator(waterPathGenerator, PathType.Water);
        
        // 生成陆地路径
        if (landPathGenerator != null)
        {
            LogInfo("开始生成陆地路径");
            landPathGenerator.GenerateLandPath();
            
            // 检查路径是否生成成功
            Transform currentLandParent = GameObject.Find("LandPathParent")?.transform;
            if (currentLandParent != null && currentLandParent.childCount > 0)
            {
                LogInfo($"成功生成陆地路径，路径点数量：{currentLandParent.childCount}");
            }
            else
            {
                LogError("陆地路径生成失败！尝试使用备用方法生成路径");
                CreateBackupPath("LandPathParent", PathType.Land);
            }
        }
        else
        {
            LogError("找不到LandPathGenerator组件！使用备用方法生成路径");
            CreateBackupPath("LandPathParent", PathType.Land);
        }
        
        // 生成水路径
        if (waterPathGenerator != null)
        {
            LogInfo("开始生成水路路径");
            waterPathGenerator.GenerateWaterPath();
            
            // 检查路径是否生成成功
            Transform currentWaterParent = GameObject.Find("WaterPathParent")?.transform;
            if (currentWaterParent != null && currentWaterParent.childCount > 0)
            {
                LogInfo($"成功生成水路路径，路径点数量：{currentWaterParent.childCount}");
            }
            else
            {
                LogError("水路路径生成失败！尝试使用备用方法生成路径");
                CreateBackupPath("WaterPathParent", PathType.Water);
            }
        }
        else
        {
            LogError("找不到WaterPathGenerator组件！使用备用方法生成路径");
            CreateBackupPath("WaterPathParent", PathType.Water);
        }
        
        // 最终验证路径是否生成
        VerifyPathsGenerated();
        
        pathsGenerated = true;
        LogInfo("路径生成过程完成");
    }
    
    private Transform ForceRecreatePathParent(string parentName)
    {
        // 查找现有的路径父对象
        GameObject existingParent = GameObject.Find(parentName);
        
        // 如果存在，则记录它的信息并销毁
        Transform originalParent = null;
        Vector3 originalPosition = Vector3.zero;
        Quaternion originalRotation = Quaternion.identity;
        
        if (existingParent != null)
        {
            originalParent = existingParent.transform.parent;
            originalPosition = existingParent.transform.position;
            originalRotation = existingParent.transform.rotation;
            LogInfo($"销毁现有的{parentName}，子对象数量：{existingParent.transform.childCount}");
            DestroyImmediate(existingParent);
        }
        
        // 创建新的父对象
        GameObject newParent = new GameObject(parentName);
        
        // 如果有原始信息，则恢复
        if (originalParent != null)
        {
            newParent.transform.parent = originalParent;
            newParent.transform.position = originalPosition;
            newParent.transform.rotation = originalRotation;
        }
        
        LogInfo($"创建了新的路径父对象: {parentName}");
        
        // 直接在这里创建一些默认路径点，确保不会出现空路径
        PathType pathType = parentName.Contains("Land") ? PathType.Land : PathType.Water;
        CreateDefaultPathPointsFor(newParent.transform, pathType);
        
        return newParent.transform;
    }
    
    // 为指定的路径父对象创建默认路径点
    private void CreateDefaultPathPointsFor(Transform pathParent, PathType pathType)
    {
        LogInfo($"直接为{pathParent.name}创建默认路径点");
        
        Vector3[] points;
        // 在场景中央附近创建路径
        float centerX = 0f;
        float centerY = 0f;
        
        if (pathType == PathType.Land)
        {
            // 创建Z字形陆地路径
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0)
            };
        }
        else // Water
        {
            // 创建环形水路径
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY - 5f, 0)
            };
        }
        
        // 创建路径点
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParent);
            LogInfo($"创建默认路径点 {i} 在位置: {points[i]}");
        }
        
        LogInfo($"默认路径创建完成，路径点数量: {pathParent.childCount}");
    }

    // 设置路径生成器的必要组件
    private void SetupPathGenerator(PathGenerator generator, PathType type)
    {
        if (generator == null) return;
        
        // 设置路径类型
        generator.pathType = type;
        
        // 检查并设置Tilemap
        if (generator.pathTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            if (tilemaps.Length > 0)
            {
                foreach (Tilemap tilemap in tilemaps)
                {
                    if ((type == PathType.Land && tilemap.name.Contains("Land")) || 
                        (type == PathType.Water && tilemap.name.Contains("Water")))
                    {
                        generator.pathTilemap = tilemap;
                        LogInfo($"为{type}路径生成器自动设置了Tilemap: {tilemap.name}");
                        break;
                    }
                }
                
                // 如果没有找到匹配的，使用第一个
                if (generator.pathTilemap == null && tilemaps.Length > 0)
                {
                    generator.pathTilemap = tilemaps[0];
                    LogInfo($"没有找到匹配的Tilemap，为{type}路径生成器设置默认Tilemap: {tilemaps[0].name}");
                }
            }
            else
            {
                LogError($"场景中没有找到任何Tilemap! {type}路径生成器将无法正常工作");
            }
        }
        
        // 检查并设置路径点预制体
        if (generator.waypointPrefab == null)
        {
            GameObject waypoint = new GameObject("WaypointPrefab");
            generator.waypointPrefab = waypoint;
            LogInfo($"为{type}路径生成器创建了默认的路径点预制体");
        }
        
        // 开启调试信息
        generator.showDebugInfo = showDebugInfo;
    }

    // 备用方法：创建基本路径
    private void CreateBackupPath(string parentName, PathType pathType)
    {
        LogInfo($"使用备用方法为{pathType}创建路径");
        
        // 确保父对象存在
        GameObject pathParentObj = GameObject.Find(parentName);
        if (pathParentObj == null)
        {
            pathParentObj = new GameObject(parentName);
        }
        else
        {
            // 清理现有子对象
            foreach (Transform child in pathParentObj.transform)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        // 创建路径点
        Vector3[] points;
        float centerX = 0f;
        float centerY = 0f;
        
        if (pathType == PathType.Land)
        {
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0)
            };
        }
        else
        {
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY - 5f, 0)
            };
        }
        
        // 创建路径点
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParentObj.transform);
            LogInfo($"创建备用路径点 {i} 在位置: {points[i]}");
        }
        
        LogInfo($"备用路径创建完成，{parentName}路径点数量: {pathParentObj.transform.childCount}");
    }

    // 验证路径是否生成
    private void VerifyPathsGenerated()
    {
        Transform landPath = GameObject.Find("LandPathParent")?.transform;
        Transform waterPath = GameObject.Find("WaterPathParent")?.transform;
        
        int landPointCount = landPath?.childCount ?? 0;
        int waterPointCount = waterPath?.childCount ?? 0;
        
        LogInfo($"路径生成验证：陆地路径点数量={landPointCount}，水路路径点数量={waterPointCount}");
        
        if (landPointCount == 0)
        {
            LogError("!!! 陆地路径生成失败，或路径中没有路径点 !!!");
            CreateBackupPath("LandPathParent", PathType.Land);
        }
        
        if (waterPointCount == 0)
        {
            LogError("!!! 水路路径生成失败，或路径中没有路径点 !!!");
            CreateBackupPath("WaterPathParent", PathType.Water);
        }
    }

    // 检查路径是否已生成
    public bool ArePathsGenerated()
    {
        // 如果路径已标记为生成完成，且两种路径都有效，则直接返回true
        if (pathsGenerated)
        {
            // 双重检查路径对象是否真的存在
            bool landPathValid = false;
            bool waterPathValid = false;
            
            Transform landParent = GameObject.Find("LandPathParent")?.transform;
            if (landParent != null && landParent.childCount > 0)
            {
                landPathValid = true;
            }
            
            Transform waterParent = GameObject.Find("WaterPathParent")?.transform;
            if (waterParent != null && waterParent.childCount > 0)
            {
                waterPathValid = true;
            }
            
            // 如果路径不存在，重置pathsGenerated状态
            if (!landPathValid || !waterPathValid)
            {
                pathsGenerated = false;
            }
            
            return landPathValid && waterPathValid;
        }
        
        // 否则检查路径是否存在
        bool landValid = false;
        bool waterValid = false;
        
        Transform land = GameObject.Find("LandPathParent")?.transform;
        if (land != null && land.childCount > 0)
        {
            landValid = true;
        }
        
        Transform water = GameObject.Find("WaterPathParent")?.transform;
        if (water != null && water.childCount > 0)
        {
            waterValid = true;
        }
        
        // 更新路径生成状态
        pathsGenerated = landValid && waterValid;
        
        return landValid && waterValid;
    }
    
    private void LogInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[PathManager] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[PathManager] {message}");
    }

    // 修复空路径问题
    public void FixEmptyPath(string pathName)
    {
        LogInfo($"尝试修复空路径: {pathName}");
        
        GameObject pathObj = GameObject.Find(pathName);
        if (pathObj != null && pathObj.transform.childCount == 0)
        {
            LogInfo($"找到{pathName}但没有路径点，创建默认路径点");
            
            // 确保我们知道是什么类型的路径
            PathType pathType = pathName.Contains("Land") ? PathType.Land : PathType.Water;
            
            // 先清除所有子对象（虽然已经是空的）
            foreach (Transform child in pathObj.transform)
            {
                DestroyImmediate(child.gameObject);
            }
            
            // 创建默认路径点
            CreateDefaultPathPointsFor(pathObj.transform, pathType);
            
            LogInfo($"为{pathName}创建了{pathObj.transform.childCount}个默认路径点");
        }
        else if (pathObj == null)
        {
            LogError($"修复路径时找不到{pathName}对象，将创建新的");
            
            // 创建新的路径父对象
            Transform newParent = ForceRecreatePathParent(pathName);
            
            LogInfo($"为{pathName}创建了新对象，路径点数量: {newParent.childCount}");
        }
        else
        {
            LogInfo($"{pathName}已有{pathObj.transform.childCount}个路径点，无需修复");
        }
    }
}