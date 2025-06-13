using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace ObstacleEditorTools
{
    // 障碍物组位置编辑窗口
    public class ObstacleGroupEditorWindow : EditorWindow
    {
        private EnhancedObstacleManager manager;
        private int groupIndex;
        private Vector2 scrollPosition;
        
        private bool isPickingTile = false;
        private bool isRemovingTile = false;
        
        public static void ShowWindow(EnhancedObstacleManager manager, int index)
        {
            ObstacleGroupEditorWindow window = GetWindow<ObstacleGroupEditorWindow>("障碍物组位置编辑");
            window.manager = manager;
            window.groupIndex = index;
            window.Show();
        }
        
        private void OnGUI()
        {
            if (manager == null || manager.obstacleGroups == null || groupIndex < 0 || groupIndex >= manager.obstacleGroups.Count)
            {
                Close();
                return;
            }
            
            ObstacleGroup group = manager.obstacleGroups[groupIndex];
            
            EditorGUILayout.LabelField($"编辑障碍物组位置 - {group.groupName}", EditorStyles.boldLabel);
            
            // 添加/移除模式
            EditorGUILayout.BeginHorizontal();
            
            // 添加位置按钮
            GUI.backgroundColor = isPickingTile ? Color.green : Color.white;
            if (GUILayout.Button("添加位置"))
            {
                isPickingTile = !isPickingTile;
                isRemovingTile = false;
                SceneView.lastActiveSceneView.Focus(); // 切换到场景视图
            }
            
            // 移除位置按钮
            GUI.backgroundColor = isRemovingTile ? Color.red : Color.white;
            if (GUILayout.Button("移除位置"))
            {
                isRemovingTile = !isRemovingTile;
                isPickingTile = false;
                SceneView.lastActiveSceneView.Focus();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // 从Tilemap添加
            if (GUILayout.Button("从Tilemap添加所有障碍物"))
            {
                AddAllFromTilemap();
            }
            
            if (GUILayout.Button("清空所有位置"))
            {
                if (EditorUtility.DisplayDialog("确认清空", 
                    "确定要清空所有障碍物位置吗?", "清空", "取消"))
                {
                    Undo.RecordObject(manager, "Clear Group Positions");
                    group.positions.Clear();
                    EditorUtility.SetDirty(manager);
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"当前位置数量: {(group.positions != null ? group.positions.Count : 0)}");
            
            // 显示所有位置
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (group.positions != null)
            {
                for (int i = 0; i < group.positions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField($"位置 {i+1}: {group.positions[i]}");
                    
                    if (GUILayout.Button("定位", GUILayout.Width(60)))
                    {
                        // 在场景视图中聚焦到该位置
                        FocusOnPosition(group.positions[i]);
                    }
                    
                    if (GUILayout.Button("移除", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(manager, "Remove Position From Group");
                        group.positions.RemoveAt(i);
                        EditorUtility.SetDirty(manager);
                        break;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // 从Tilemap添加所有障碍物
        private void AddAllFromTilemap()
        {
            if (manager == null || manager.obstacleTilemap == null) return;
            
            Tilemap tilemap = manager.obstacleTilemap;
            ObstacleGroup group = manager.obstacleGroups[groupIndex];
            
            Undo.RecordObject(manager, "Add All From Tilemap");
            
            // 获取Tilemap中的所有障碍物
            BoundsInt bounds = tilemap.cellBounds;
            
            for (int x = bounds.min.x; x < bounds.max.x; x++)
            {
                for (int y = bounds.min.y; y < bounds.max.y; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(pos) && !group.positions.Contains(pos))
                    {
                        group.positions.Add(pos);
                    }
                }
            }
            
            EditorUtility.SetDirty(manager);
        }
        
        // 在场景视图中聚焦到位置
        private void FocusOnPosition(Vector3Int cellPos)
        {
            if (manager == null || manager.obstacleTilemap == null) return;
            
            Tilemap tilemap = manager.obstacleTilemap;
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
            
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.LookAt(worldPos);
            }
        }
        
        // 场景视图交互
        private void OnSceneGUI(SceneView sceneView)
        {
            if (manager == null || !isPickingTile && !isRemovingTile) return;
            
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 获取鼠标位置
                Vector2 mousePos = e.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
                
                // 将射线转换为2D平面上的点
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                float distance;
                if (plane.Raycast(ray, out distance))
                {
                    Vector3 worldPos = ray.GetPoint(distance);
                    Vector3Int cellPos = manager.obstacleTilemap.WorldToCell(worldPos);
                    
                    if (isPickingTile)
                    {
                        AddPositionToGroup(cellPos);
                    }
                    else if (isRemovingTile)
                    {
                        RemovePositionFromGroup(cellPos);
                    }
                }
                
                e.Use(); // 阻止事件传递
            }
        }
        
        // 添加位置到组
        private void AddPositionToGroup(Vector3Int position)
        {
            if (manager == null || groupIndex < 0 || groupIndex >= manager.obstacleGroups.Count) return;
            
            ObstacleGroup group = manager.obstacleGroups[groupIndex];
            
            // 检查是否已经包含该位置
            if (group.positions.Contains(position))
                return;
                
            // 检查该位置是否有障碍物
            if (!manager.IsObstacle(position))
                return;
                
            Undo.RecordObject(manager, "Add Position To Group");
            group.positions.Add(position);
            EditorUtility.SetDirty(manager);
            
            Repaint(); // 重绘窗口
        }
        
        // 从组中移除位置
        private void RemovePositionFromGroup(Vector3Int position)
        {
            if (manager == null || groupIndex < 0 || groupIndex >= manager.obstacleGroups.Count) return;
            
            ObstacleGroup group = manager.obstacleGroups[groupIndex];
            
            // 检查是否包含该位置
            if (!group.positions.Contains(position))
                return;
                
            Undo.RecordObject(manager, "Remove Position From Group");
            group.positions.Remove(position);
            EditorUtility.SetDirty(manager);
            
            Repaint(); // 重绘窗口
        }
        
        private void OnEnable()
        {
            // 注册场景视图回调
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            // 取消注册场景视图回调
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
} 