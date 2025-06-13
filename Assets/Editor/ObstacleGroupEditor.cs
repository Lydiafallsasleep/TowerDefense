using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using ObstacleEditorTools;

/// <summary>
/// 障碍物组的自定义编辑器工具
/// 帮助开发者更方便地设置和管理障碍物组
/// </summary>
[CustomEditor(typeof(EnhancedObstacleManager))]
public class ObstacleGroupEditor : Editor
{
    private bool showAutoGroupSettings = false;
    private string groupPrefix = "Group";
    private bool showObstacleTypes = false;
    private bool showGroups = false;
    
    // 当前选中的组
    private int selectedGroupIndex = -1;
    
    // 用于可视化的参数
    private bool visualizeGroups = false;
    private Color[] groupColors = new Color[] 
    {
        new Color(1, 0, 0, 0.5f),    // 红色
        new Color(0, 1, 0, 0.5f),    // 绿色
        new Color(0, 0, 1, 0.5f),    // 蓝色
        new Color(1, 1, 0, 0.5f),    // 黄色
        new Color(1, 0, 1, 0.5f),    // 紫色
        new Color(0, 1, 1, 0.5f),    // 青色
        new Color(1, 0.5f, 0, 0.5f), // 橙色
        new Color(0.5f, 0, 1, 0.5f)  // 靛色
    };
    
    // 绘制Inspector界面
    public override void OnInspectorGUI()
    {
        // 获取目标组件
        EnhancedObstacleManager manager = (EnhancedObstacleManager)target;
        
        // 绘制默认Inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("障碍物组工具", EditorStyles.boldLabel);
        
        // 绘制自动组生成设置
        DrawAutoGroupSettings(manager);
        
        EditorGUILayout.Space();
        
        // 可视化设置
        DrawVisualizationSettings(manager);
        
        // 障碍物类型设置
        DrawObstacleTypeSettings(manager);
        
        // 组设置
        DrawGroupSettings(manager);
        
        // 如果进行了修改，标记为脏，以便保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }
    
    // 绘制自动组生成设置
    private void DrawAutoGroupSettings(EnhancedObstacleManager manager)
    {
        showAutoGroupSettings = EditorGUILayout.Foldout(showAutoGroupSettings, "自动组生成设置", true);
        
        if (showAutoGroupSettings)
        {
            EditorGUI.indentLevel++;
            
            groupPrefix = EditorGUILayout.TextField("组名称前缀", groupPrefix);
            
            if (GUILayout.Button("自动创建障碍物组"))
            {
                Undo.RecordObject(manager, "Auto Create Obstacle Groups");
                manager.AutoCreateGroups(groupPrefix);
                EditorUtility.SetDirty(manager);
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    // 绘制可视化设置
    private void DrawVisualizationSettings(EnhancedObstacleManager manager)
    {
        EditorGUILayout.LabelField("可视化", EditorStyles.boldLabel);
        
        bool newVisualizeGroups = EditorGUILayout.Toggle("可视化障碍物组", visualizeGroups);
        
        if (newVisualizeGroups != visualizeGroups)
        {
            visualizeGroups = newVisualizeGroups;
            SceneView.RepaintAll(); // 重绘场景视图
        }
    }
    
    // 绘制障碍物类型设置
    private void DrawObstacleTypeSettings(EnhancedObstacleManager manager)
    {
        showObstacleTypes = EditorGUILayout.Foldout(showObstacleTypes, "障碍物类型设置", true);
        
        if (showObstacleTypes)
        {
            EditorGUI.indentLevel++;
            
            if (manager.obstacleTypes == null || manager.obstacleTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("未定义障碍物类型", MessageType.Warning);
                
                if (GUILayout.Button("创建默认类型"))
                {
                    Undo.RecordObject(manager, "Create Default Obstacle Types");
                    CreateDefaultObstacleTypes(manager);
                    EditorUtility.SetDirty(manager);
                }
            }
            else
            {
                for (int i = 0; i < manager.obstacleTypes.Length; i++)
                {
                    DrawObstacleTypeInfo(manager, i);
                }
                
                if (GUILayout.Button("添加类型"))
                {
                    Undo.RecordObject(manager, "Add Obstacle Type");
                    AddObstacleType(manager);
                    EditorUtility.SetDirty(manager);
                }
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    // 绘制单个障碍物类型信息
    private void DrawObstacleTypeInfo(EnhancedObstacleManager manager, int index)
    {
        EnhancedObstacleManager.ObstacleTypeInfo typeInfo = manager.obstacleTypes[index];
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(typeInfo.type.ToString(), GUILayout.Width(100));
        
        if (GUILayout.Button("编辑", GUILayout.Width(60)))
        {
            // 打开编辑窗口，使用命名空间中的类
            ObstacleEditorTools.ObstacleTypeEditorWindow.ShowWindow(manager, index);
        }
        
        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("确认删除", 
                $"确定要删除障碍物类型 {typeInfo.type} 吗?", "删除", "取消"))
            {
                Undo.RecordObject(manager, "Delete Obstacle Type");
                DeleteObstacleType(manager, index);
                EditorUtility.SetDirty(manager);
                return;
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    // 绘制组设置
    private void DrawGroupSettings(EnhancedObstacleManager manager)
    {
        showGroups = EditorGUILayout.Foldout(showGroups, "障碍物组设置", true);
        
        if (showGroups)
        {
            EditorGUI.indentLevel++;
            
            if (manager.obstacleGroups == null || manager.obstacleGroups.Count == 0)
            {
                EditorGUILayout.HelpBox("未定义障碍物组", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < manager.obstacleGroups.Count; i++)
                {
                    DrawGroupInfo(manager, i);
                }
            }
            
            if (GUILayout.Button("添加空组"))
            {
                Undo.RecordObject(manager, "Add Empty Group");
                AddEmptyGroup(manager);
                EditorUtility.SetDirty(manager);
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    // 绘制组信息
    private void DrawGroupInfo(EnhancedObstacleManager manager, int index)
    {
        ObstacleGroup group = manager.obstacleGroups[index];
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 组标题和基本信息
        EditorGUILayout.BeginHorizontal();
        
        bool isSelected = selectedGroupIndex == index;
        bool newIsSelected = EditorGUILayout.ToggleLeft(
            $"{group.groupName} ({group.positions.Count}个位置)", 
            isSelected, 
            EditorStyles.boldLabel);
            
        if (newIsSelected != isSelected)
        {
            selectedGroupIndex = newIsSelected ? index : -1;
            SceneView.RepaintAll();
        }
        
        if (GUILayout.Button("编辑", GUILayout.Width(60)))
        {
            // 打开编辑窗口，使用命名空间中的类
            ObstacleGroupEditorWindow.ShowWindow(manager, index);
        }
        
        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("确认删除", 
                $"确定要删除障碍物组 {group.groupName} 吗?", "删除", "取消"))
            {
                Undo.RecordObject(manager, "Delete Obstacle Group");
                manager.obstacleGroups.RemoveAt(index);
                EditorUtility.SetDirty(manager);
                return;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 如果选中，显示详细信息
        if (selectedGroupIndex == index)
        {
            EditorGUI.indentLevel++;
            
            // 组名称
            group.groupName = EditorGUILayout.TextField("组名称", group.groupName);
            
            // 成本设置
            group.useGroupCost = EditorGUILayout.Toggle("使用组统一成本", group.useGroupCost);
            
            if (group.useGroupCost)
            {
                group.clearCost = EditorGUILayout.IntField("组清除成本", group.clearCost);
            }
            
            // 显示位置数量
            EditorGUILayout.LabelField($"位置数量: {group.positions.Count}");
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // 创建默认障碍物类型
    private void CreateDefaultObstacleTypes(EnhancedObstacleManager manager)
    {
        EnhancedObstacleManager.ObstacleTypeInfo[] defaultTypes = new EnhancedObstacleManager.ObstacleTypeInfo[]
        {
            new EnhancedObstacleManager.ObstacleTypeInfo
            {
                type = EnhancedObstacleManager.ObstacleType.Default,
                displayName = "障碍物",
                clearCost = 50,
                tiles = new TileBase[0]
            },
            new EnhancedObstacleManager.ObstacleTypeInfo
            {
                type = EnhancedObstacleManager.ObstacleType.Rubble,
                displayName = "碎石",
                clearCost = 30,
                tiles = new TileBase[0]
            },
            new EnhancedObstacleManager.ObstacleTypeInfo
            {
                type = EnhancedObstacleManager.ObstacleType.Trees,
                displayName = "树木",
                clearCost = 70,
                tiles = new TileBase[0]
            },
            new EnhancedObstacleManager.ObstacleTypeInfo
            {
                type = EnhancedObstacleManager.ObstacleType.Water,
                displayName = "水面",
                clearCost = 100,
                tiles = new TileBase[0]
            },
            new EnhancedObstacleManager.ObstacleTypeInfo
            {
                type = EnhancedObstacleManager.ObstacleType.Building,
                displayName = "建筑",
                clearCost = 150,
                tiles = new TileBase[0]
            }
        };
        
        manager.obstacleTypes = defaultTypes;
    }
    
    // 添加障碍物类型
    private void AddObstacleType(EnhancedObstacleManager manager)
    {
        // 创建一个新的类型
        EnhancedObstacleManager.ObstacleTypeInfo newType = new EnhancedObstacleManager.ObstacleTypeInfo
        {
            type = EnhancedObstacleManager.ObstacleType.Default,
            displayName = "新障碍物类型",
            clearCost = 50,
            tiles = new TileBase[0]
        };
        
        // 添加到数组
        EnhancedObstacleManager.ObstacleTypeInfo[] newTypes = new EnhancedObstacleManager.ObstacleTypeInfo[manager.obstacleTypes.Length + 1];
        System.Array.Copy(manager.obstacleTypes, newTypes, manager.obstacleTypes.Length);
        newTypes[newTypes.Length - 1] = newType;
        manager.obstacleTypes = newTypes;
    }
    
    // 删除障碍物类型
    private void DeleteObstacleType(EnhancedObstacleManager manager, int index)
    {
        if (index < 0 || index >= manager.obstacleTypes.Length)
            return;
            
        EnhancedObstacleManager.ObstacleTypeInfo[] newTypes = new EnhancedObstacleManager.ObstacleTypeInfo[manager.obstacleTypes.Length - 1];
        
        for (int i = 0, j = 0; i < manager.obstacleTypes.Length; i++)
        {
            if (i != index)
            {
                if (j < newTypes.Length)
                {
                    newTypes[j++] = manager.obstacleTypes[i];
                }
            }
        }
        
        manager.obstacleTypes = newTypes;
    }
    
    // 添加空组
    private void AddEmptyGroup(EnhancedObstacleManager manager)
    {
        ObstacleGroup newGroup = new ObstacleGroup
        {
            groupName = $"新组_{manager.obstacleGroups.Count + 1}",
            positions = new List<Vector3Int>(),
            clearCost = 50,
            useGroupCost = true
        };
        
        if (manager.obstacleGroups == null)
        {
            manager.obstacleGroups = new List<ObstacleGroup>();
        }
        
        manager.obstacleGroups.Add(newGroup);
    }
    
    // 在场景视图中绘制
    private void OnSceneGUI()
    {
        EnhancedObstacleManager manager = (EnhancedObstacleManager)target;
        
        if (!visualizeGroups || manager.obstacleGroups == null)
            return;
            
        // 如果有选中的组，只显示该组
        if (selectedGroupIndex >= 0 && selectedGroupIndex < manager.obstacleGroups.Count)
        {
            ObstacleGroup group = manager.obstacleGroups[selectedGroupIndex];
            DrawGroupGizmos(group, groupColors[selectedGroupIndex % groupColors.Length]);
        }
        // 否则显示所有组
        else
        {
            for (int i = 0; i < manager.obstacleGroups.Count; i++)
            {
                Color color = groupColors[i % groupColors.Length];
                DrawGroupGizmos(manager.obstacleGroups[i], color);
            }
        }
    }
    
    // 绘制组的可视化
    private void DrawGroupGizmos(ObstacleGroup group, Color color)
    {
        if (group == null || group.positions == null || group.positions.Count == 0)
            return;
            
        EnhancedObstacleManager manager = (EnhancedObstacleManager)target;
        Tilemap tilemap = manager.obstacleTilemap;
        
        if (tilemap == null)
            return;
            
        // 绘制每个位置
        foreach (Vector3Int cellPos in group.positions)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
            float size = 0.45f; // 稍微小于格子大小
            
            // 绘制方块
            Vector3[] corners = new Vector3[]
            {
                worldPos + new Vector3(-size, -size, 0),
                worldPos + new Vector3(size, -size, 0),
                worldPos + new Vector3(size, size, 0),
                worldPos + new Vector3(-size, size, 0)
            };
            
            Handles.DrawSolidRectangleWithOutline(corners, new Color(color.r, color.g, color.b, 0.2f), color);
        }
        
        // 如果有多个位置，绘制连接线
        if (group.positions.Count > 1)
        {
            for (int i = 0; i < group.positions.Count - 1; i++)
            {
                Vector3 pos1 = tilemap.GetCellCenterWorld(group.positions[i]);
                Vector3 pos2 = tilemap.GetCellCenterWorld(group.positions[i + 1]);
                
                Handles.DrawDottedLine(pos1, pos2, 2f);
            }
        }
    }
} 