using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace ObstacleEditorTools
{
    // 障碍物类型编辑窗口
    public class ObstacleTypeEditorWindow : EditorWindow
    {
        private EnhancedObstacleManager manager;
        private int typeIndex;
        
        public static void ShowWindow(EnhancedObstacleManager manager, int index)
        {
            ObstacleTypeEditorWindow window = GetWindow<ObstacleTypeEditorWindow>("障碍物类型编辑");
            window.manager = manager;
            window.typeIndex = index;
            window.Show();
        }
        
        private void OnGUI()
        {
            if (manager == null || manager.obstacleTypes == null || typeIndex < 0 || typeIndex >= manager.obstacleTypes.Length)
            {
                Close();
                return;
            }
            
            EnhancedObstacleManager.ObstacleTypeInfo typeInfo = manager.obstacleTypes[typeIndex];
            
            EditorGUILayout.LabelField("编辑障碍物类型", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            // 类型名称
            typeInfo.type = (EnhancedObstacleManager.ObstacleType)EditorGUILayout.EnumPopup("类型", typeInfo.type);
            
            // 显示名称
            typeInfo.displayName = EditorGUILayout.TextField("显示名称", typeInfo.displayName);
            
            // 清除成本
            typeInfo.clearCost = EditorGUILayout.IntField("清除成本", typeInfo.clearCost);
            
            // 图标
            typeInfo.icon = (Sprite)EditorGUILayout.ObjectField("图标", typeInfo.icon, typeof(Sprite), false);
            
            // 清除特效
            typeInfo.clearEffect = (GameObject)EditorGUILayout.ObjectField("清除特效", typeInfo.clearEffect, typeof(GameObject), false);
            
            // 瓦片
            EditorGUILayout.LabelField("瓦片列表", EditorStyles.boldLabel);
            if (typeInfo.tiles == null)
            {
                typeInfo.tiles = new TileBase[0];
            }
            
            if (GUILayout.Button("添加瓦片"))
            {
                TileBase[] newTiles = new TileBase[typeInfo.tiles.Length + 1];
                System.Array.Copy(typeInfo.tiles, newTiles, typeInfo.tiles.Length);
                typeInfo.tiles = newTiles;
            }
            
            for (int i = 0; i < typeInfo.tiles.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                typeInfo.tiles[i] = (TileBase)EditorGUILayout.ObjectField($"瓦片 {i+1}", typeInfo.tiles[i], typeof(TileBase), false);
                
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    TileBase[] newTiles = new TileBase[typeInfo.tiles.Length - 1];
                    
                    for (int j = 0, k = 0; j < typeInfo.tiles.Length; j++)
                    {
                        if (j != i)
                        {
                            newTiles[k++] = typeInfo.tiles[j];
                        }
                    }
                    
                    typeInfo.tiles = newTiles;
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(manager);
            }
        }
    }
} 