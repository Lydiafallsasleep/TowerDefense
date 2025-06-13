using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Unity编辑器扩展，用于管理塔放置点
/// </summary>
[CustomEditor(typeof(TowerPlacementManager))]
public class TowerPlacementEditor : Editor
{
    private bool showPlacementPoints = true;
    private Vector3 newPointPosition = Vector3.zero;
    private string newPointGroup = "";
    private enum PlacementPointType { Normal, Obstacle }
    private PlacementPointType newPointType = PlacementPointType.Normal;
    
    public override void OnInspectorGUI()
    {
        // 绘制默认Inspector
        DrawDefaultInspector();
        
        // 获取目标组件
        TowerPlacementManager manager = (TowerPlacementManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("放置点工具", EditorStyles.boldLabel);
        
        // 在场景中创建放置点
        EditorGUILayout.LabelField("创建新放置点", EditorStyles.boldLabel);
        
        // 放置点类型
        newPointType = (PlacementPointType)EditorGUILayout.EnumPopup("放置点类型", newPointType);
        
        newPointPosition = EditorGUILayout.Vector3Field("位置", newPointPosition);
        newPointGroup = EditorGUILayout.TextField("组ID", newPointGroup);
        
        if (GUILayout.Button("在当前位置创建放置点"))
        {
            // 创建一个新的放置点
            CreatePlacementPoint(manager, newPointPosition, newPointGroup, newPointType);
        }
        
        if (GUILayout.Button("在场景视图选择位置创建放置点"))
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorUtility.DisplayDialog("创建放置点", "请在场景视图中点击鼠标左键选择位置，然后按回车确认或ESC取消", "确定");
        }
        
        EditorGUILayout.Space();
        
        // 管理现有放置点
        showPlacementPoints = EditorGUILayout.Foldout(showPlacementPoints, "现有放置点");
        
        if (showPlacementPoints && manager.placementPoints != null)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < manager.placementPoints.Count; i++)
            {
                TowerPlacementPoint point = manager.placementPoints[i];
                
                if (point != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // 显示点ID
                    EditorGUILayout.LabelField($"点 {i}: {point.pointID}");
                    
                    // 添加选择按钮
                    if (GUILayout.Button("选择", GUILayout.Width(80)))
                    {
                        // 选择该对象
                        Selection.activeGameObject = point.gameObject;
                        
                        // 聚焦场景视图到该对象
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                    
                    // 添加删除按钮
                    if (GUILayout.Button("删除", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("删除放置点", $"确定删除放置点 {point.pointID}?", "确定", "取消"))
                        {
                            // 从列表中移除
                            manager.placementPoints.RemoveAt(i);
                            
                            // 销毁游戏对象
                            DestroyImmediate(point.gameObject);
                            
                            // 设置为已修改
                            EditorUtility.SetDirty(manager);
                            
                            // 刷新Inspector
                            GUIUtility.ExitGUI();
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 批量操作
        EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
        
        if (GUILayout.Button("启用所有放置点"))
        {
            EnableAllPlacementPoints(manager, true);
        }
        
        if (GUILayout.Button("禁用所有放置点"))
        {
            EnableAllPlacementPoints(manager, false);
        }
        
        if (GUILayout.Button("调整所有点的高度..."))
        {
            float height = EditorGUILayout.FloatField("高度", 0);
            AdjustAllPointsHeight(manager, height);
        }
        
        if (GUILayout.Button("将选中对象添加到放置点列表"))
        {
            AddSelectedPlacementPoints(manager);
        }
        
        if (GUILayout.Button("自动收集场景中的放置点"))
        {
            AutoCollectPlacementPoints(manager);
        }
    }
    
    // 在场景视图中选择位置
    private void OnSceneGUI(SceneView sceneView)
    {
        // 显示提示
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 300, 20), "点击选择放置点位置，按回车确认或ESC取消");
        Handles.EndGUI();
        
        // 获取事件
        Event e = Event.current;
        
        // 如果是鼠标左键点击事件
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // 获取鼠标位置
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // 更新位置
                newPointPosition = hit.point;
                e.Use();
            }
            else
            {
                // 如果没有击中任何物体，尝试与平面相交
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                
                if (plane.Raycast(ray, out distance))
                {
                    newPointPosition = ray.GetPoint(distance);
                    e.Use();
                }
            }
            
            // 显示位置
            Handles.DrawWireCube(newPointPosition, Vector3.one * 0.5f);
            SceneView.RepaintAll();
        }
        // 如果是回车键
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        {
            TowerPlacementManager manager = (TowerPlacementManager)target;
            CreatePlacementPoint(manager, newPointPosition, newPointGroup, newPointType);
            SceneView.duringSceneGui -= OnSceneGUI;
            e.Use();
        }
        // 如果是ESC键
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            // 取消操作
            SceneView.duringSceneGui -= OnSceneGUI;
            e.Use();
        }
    }
    
    // 创建放置点 (重载，带类型)
    private void CreatePlacementPoint(TowerPlacementManager manager, Vector3 position, string groupID, PlacementPointType pointType)
    {
        // 计算网格位置
        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.y),
            Mathf.RoundToInt(position.z)
        );
        
        string pointID = $"PlacementPoint_{manager.placementPoints.Count}";
        TowerPlacementPoint point = null;
        
        if (pointType == PlacementPointType.Normal)
        {
            // 使用管理器自带的方法
            point = manager.CreatePlacementPoint(position, gridPosition, pointID);
        }
        else // Obstacle 类型
        {
            point = CreateObstaclePlacementPoint(manager, position, gridPosition, pointID);
        }
        
        if (!string.IsNullOrEmpty(groupID))
        {
            point.placementGroupID = groupID;
        }
        
        EditorUtility.SetDirty(manager);
        Selection.activeGameObject = point.gameObject;
    }
    
    // 创建与障碍物关联的放置点
    private TowerPlacementPoint CreateObstaclePlacementPoint(TowerPlacementManager manager, Vector3 position, Vector3Int gridPosition, string pointID)
    {
        GameObject pointObj = new GameObject(pointID);
        pointObj.transform.position = position;
        
        ObstaclePlacementPoint point = pointObj.AddComponent<ObstaclePlacementPoint>();
        point.pointID = pointID;
        point.gridPosition = gridPosition;
        
        // 创建视觉指示器（与管理器默认方法保持一致）
        GameObject indicatorObj = new GameObject("PlacementIndicator");
        indicatorObj.transform.SetParent(pointObj.transform);
        indicatorObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer indicator = indicatorObj.AddComponent<SpriteRenderer>();
        indicator.sprite = Resources.Load<Sprite>("UI/PlacementIndicator");
        indicator.color = point.availableColor;
        indicator.sortingOrder = -1;
        point.placementIndicator = indicator;
        
        // 加入到管理器列表
        manager.placementPoints.Add(point);
        return point;
    }
    
    // 选中对象添加到放置点列表
    private void AddSelectedPlacementPoints(TowerPlacementManager manager)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            TowerPlacementPoint p = obj.GetComponent<TowerPlacementPoint>();
            if (p != null && !manager.placementPoints.Contains(p))
            {
                manager.placementPoints.Add(p);
                EditorUtility.SetDirty(p);
            }
        }
        EditorUtility.SetDirty(manager);
        Repaint();
    }
    
    // 自动扫描场景中所有放置点并同步
    private void AutoCollectPlacementPoints(TowerPlacementManager manager)
    {
        // 查找所有基础放置点和障碍物放置点
        TowerPlacementPoint[] basicPointsInScene = FindObjectsOfType<TowerPlacementPoint>();
        ObstaclePlacementPoint[] obstaclePointsInScene = FindObjectsOfType<ObstaclePlacementPoint>();
        
        // 创建新列表以避免重复
        List<TowerPlacementPoint> allPoints = new List<TowerPlacementPoint>();
        
        // 添加基础放置点（排除障碍物放置点，因为它们会在下一步添加）
        foreach (TowerPlacementPoint point in basicPointsInScene)
        {
            if (!(point is ObstaclePlacementPoint) && !allPoints.Contains(point))
            {
                allPoints.Add(point);
            }
        }
        
        // 添加障碍物放置点
        foreach (ObstaclePlacementPoint point in obstaclePointsInScene)
        {
            if (!allPoints.Contains(point))
            {
                allPoints.Add(point);
            }
        }
        
        // 更新管理器的放置点列表
        manager.placementPoints = allPoints;
        
        // 标记为已修改
        EditorUtility.SetDirty(manager);
        
        // 刷新Inspector
        Repaint();
        
        Debug.Log($"已收集 {allPoints.Count} 个放置点（包括 {obstaclePointsInScene.Length} 个障碍物放置点）");
    }
    
    // 启用/禁用所有放置点
    private void EnableAllPlacementPoints(TowerPlacementManager manager, bool enable)
    {
        if (manager.placementPoints == null) return;
        
        foreach (TowerPlacementPoint point in manager.placementPoints)
        {
            if (point == null) continue;
            
            if (enable)
            {
                point.EnablePoint();
            }
            else
            {
                point.DisablePoint();
            }
            
            // 设置为已修改
            EditorUtility.SetDirty(point);
        }
        
        // 刷新Inspector
        Repaint();
    }
    
    // 调整所有点的高度
    private void AdjustAllPointsHeight(TowerPlacementManager manager, float height)
    {
        if (manager.placementPoints == null) return;
        
        foreach (TowerPlacementPoint point in manager.placementPoints)
        {
            if (point == null) continue;
            
            // 获取当前位置
            Vector3 position = point.transform.position;
            
            // 调整高度
            position.y = height;
            
            // 应用新位置
            point.transform.position = position;
            
            // 更新网格位置
            point.gridPosition = new Vector3Int(
                point.gridPosition.x,
                Mathf.RoundToInt(height),
                point.gridPosition.z
            );
            
            // 设置为已修改
            EditorUtility.SetDirty(point);
        }
        
        // 刷新Inspector
        Repaint();
    }
} 