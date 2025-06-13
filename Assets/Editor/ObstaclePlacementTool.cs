using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 在障碍物位置自动创建塔放置点的编辑器工具
/// </summary>
public class ObstaclePlacementTool : EditorWindow
{
    private ObstacleManager obstacleManager;
    private Tilemap[] obstacleTilemaps; // 多个障碍物图层
    private Tilemap obstacleTilemap;    // 向后兼容-单个障碍物图层
    private GameObject placementPointPrefab;
    private Transform placementPointsParent;
    private string parentName = "ObstaclePlacementPoints";
    private float placementPointScale = 1.0f;
    private bool createVisualIndicator = true;
    private Color placementPointColor = new Color(0.2f, 0.7f, 1f, 0.5f);
    
    [MenuItem("Tools/Tower Defense/Obstacle Placement Points Generator")]
    public static void ShowWindow()
    {
        GetWindow<ObstaclePlacementTool>("障碍物放置点生成器");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("障碍物放置点生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("此工具可以在障碍物位置自动创建塔防放置点，这些放置点将在障碍物被清除后可用。", MessageType.Info);
        EditorGUILayout.Space();
        
        obstacleManager = (ObstacleManager)EditorGUILayout.ObjectField("障碍物管理器", obstacleManager, typeof(ObstacleManager), true);
        
        if (obstacleManager != null)
        {
            if (obstacleManager.obstacleTilemaps != null && obstacleManager.obstacleTilemaps.Length > 0)
            {
                obstacleTilemaps = obstacleManager.obstacleTilemaps;
                EditorGUILayout.LabelField($"已找到 {obstacleTilemaps.Length} 个障碍物图层");
                
                // 显示所有图层
                EditorGUI.indentLevel++;
                for (int i = 0; i < obstacleTilemaps.Length; i++)
                {
                    if (obstacleTilemaps[i] != null)
                    {
                        EditorGUILayout.LabelField($"图层 {i+1}: {obstacleTilemaps[i].name}");
                    }
                }
                EditorGUI.indentLevel--;
            }
            else if (obstacleManager.obstacleTilemap != null)
            {
                obstacleTilemap = obstacleManager.obstacleTilemap;
                EditorGUILayout.LabelField("已找到障碍物Tilemap: " + obstacleTilemap.name);
            }
            else
            {
                EditorGUILayout.HelpBox("障碍物管理器未设置任何Tilemap", MessageType.Warning);
            }
        }
        else
        {
            // 允许手动设置单个图层
            obstacleTilemap = (Tilemap)EditorGUILayout.ObjectField("障碍物Tilemap", obstacleTilemap, typeof(Tilemap), true);
        }
        
        placementPointPrefab = (GameObject)EditorGUILayout.ObjectField("放置点预制体", placementPointPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        parentName = EditorGUILayout.TextField("父物体名称", parentName);
        placementPointScale = EditorGUILayout.Slider("放置点缩放", placementPointScale, 0.1f, 2f);
        
        EditorGUILayout.Space();
        createVisualIndicator = EditorGUILayout.Toggle("创建视觉指示器", createVisualIndicator);
        if (createVisualIndicator)
        {
            placementPointColor = EditorGUILayout.ColorField("指示器颜色", placementPointColor);
        }
        
        EditorGUILayout.Space();
        GUI.enabled = (obstacleTilemap != null) || (obstacleTilemaps != null && obstacleTilemaps.Length > 0);
        
        if (GUILayout.Button("生成放置点"))
        {
            GeneratePlacementPoints();
        }
        
        GUI.enabled = true;
    }
    
    private void GeneratePlacementPoints()
    {
        // 检查是否有可用的Tilemap
        bool hasValidTilemap = (obstacleTilemap != null) || (obstacleTilemaps != null && obstacleTilemaps.Length > 0);
        if (!hasValidTilemap)
        {
            EditorUtility.DisplayDialog("错误", "请先设置障碍物Tilemap", "确定");
            return;
        }
        
        // 查找或创建父物体
        GameObject parentObj = GameObject.Find(parentName);
        if (parentObj == null)
        {
            parentObj = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parentObj, "Create Placement Points Parent");
        }
        placementPointsParent = parentObj.transform;
        
        // 获取所有障碍物位置 (避免重复)
        HashSet<Vector3Int> obstaclePositions = new HashSet<Vector3Int>();
        
        // 从多个图层获取障碍物位置
        if (obstacleTilemaps != null && obstacleTilemaps.Length > 0)
        {
            foreach (Tilemap tilemap in obstacleTilemaps)
            {
                if (tilemap != null)
                {
                    CollectObstaclePositions(tilemap, obstaclePositions);
                }
            }
        }
        // 向后兼容 - 从单个图层获取障碍物位置
        else if (obstacleTilemap != null)
        {
            CollectObstaclePositions(obstacleTilemap, obstaclePositions);
        }
        
        int createdCount = 0;
        
        // 在每个障碍物位置创建放置点
        foreach (Vector3Int cellPos in obstaclePositions)
        {
            // 获取世界坐标 (使用第一个有效的图层)
            Vector3 worldPos = Vector3.zero;
            if (obstacleTilemaps != null && obstacleTilemaps.Length > 0)
            {
                foreach (Tilemap tilemap in obstacleTilemaps)
                {
                    if (tilemap != null)
                    {
                        worldPos = tilemap.GetCellCenterWorld(cellPos);
                        break;
                    }
                }
            }
            else if (obstacleTilemap != null)
            {
                worldPos = obstacleTilemap.GetCellCenterWorld(cellPos);
            }
            
            // 创建放置点
            GameObject pointObj;
            if (placementPointPrefab != null)
            {
                pointObj = PrefabUtility.InstantiatePrefab(placementPointPrefab) as GameObject;
                pointObj.transform.position = worldPos;
            }
            else
            {
                pointObj = new GameObject("ObstaclePlacementPoint_" + cellPos.x + "_" + cellPos.y);
                pointObj.transform.position = worldPos;
                
                // 添加必要的组件
                SpriteRenderer sr = pointObj.AddComponent<SpriteRenderer>();
                if (createVisualIndicator)
                {
                    sr.sprite = CreateCircleSprite();
                    sr.color = placementPointColor;
                }
            }
            
            // 设置父物体
            pointObj.transform.SetParent(placementPointsParent);
            pointObj.transform.localScale = new Vector3(placementPointScale, placementPointScale, placementPointScale);
            
            // 添加ObstaclePlacementPoint组件
            ObstaclePlacementPoint placementPoint = pointObj.AddComponent<ObstaclePlacementPoint>();
            placementPoint.obstaclePosition = cellPos;
            placementPoint.autoDetectPosition = false;
            
            Undo.RegisterCreatedObjectUndo(pointObj, "Create Placement Point");
            createdCount++;
        }
        EditorUtility.DisplayDialog("完成", $"已创建 {createdCount} 个障碍物放置点。", "确定");
    }
    
    // 从一个图层收集障碍物位置
    private void CollectObstaclePositions(Tilemap tilemap, HashSet<Vector3Int> positions)
    {
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.GetTile(pos) != null)
                {
                    positions.Add(pos);
                }
            }
        }
    }
    
    // 创建一个圆形Sprite
    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distX = x - size / 2f;
                float distY = y - size / 2f;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);
                float alpha = dist <= size / 2f ? 1f : 0f;
                
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }
}
