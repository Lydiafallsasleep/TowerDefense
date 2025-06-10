using UnityEngine;
using UnityEngine.Tilemaps;

public enum PathType { Land, Water }

public class PathGenerator : MonoBehaviour
{
    [Header("Path Settings")]
    public PathType pathType;
    public Tilemap pathTilemap;
    public GameObject waypointPrefab;
    public Transform pathParent;
    public float waypointInterval = 1f;
    public Color gizmoColor = Color.red;
    public float gizmoRadius = 0.3f;

    [Header("调试")]
    public bool showDebugInfo = true;

    protected virtual void Start()
    {
        if (pathTilemap == null)
        {
            Debug.LogError("Path Tilemap is not assigned!", this);
            return;
        }

        if (waypointPrefab == null)
        {
            Debug.LogError("Waypoint Prefab is not assigned!", this);
            return;
        }

        GenerateWaypoints();
    }

    public void GenerateWaypoints()
    {
        if (pathParent == null)
        {
            Debug.LogError($"Path Parent for {pathType} is not assigned!");
            return;
        }

        LogInfo($"开始生成{pathType}路径点，pathParent = {pathParent.name}");

        // 确保父对象激活
        pathParent.gameObject.SetActive(true);

        // 先记录子对象数量
        int initialChildCount = pathParent.childCount;
        LogInfo($"开始清理前，{pathParent.name}的子对象数量: {initialChildCount}");

        // 使用更彻底的方法清理路径点 - 方法1：新建一个空对象替换
        string originalName = pathParent.name;
        Transform originalParent = pathParent.parent;
        Vector3 originalPosition = pathParent.position;
        Quaternion originalRotation = pathParent.rotation;
        Vector3 originalScale = pathParent.localScale;

        // 销毁原对象
        DestroyImmediate(pathParent.gameObject);

        // 创建新的同名对象
        GameObject newParentObj = new GameObject(originalName);
        newParentObj.transform.parent = originalParent;
        newParentObj.transform.position = originalPosition;
        newParentObj.transform.rotation = originalRotation;
        newParentObj.transform.localScale = originalScale;
        pathParent = newParentObj.transform;
        
        // 确认清理完毕
        if (pathParent.childCount > 0)
        {
            LogError($"清理失败，{pathParent.name}仍有{pathParent.childCount}个子对象！");
        }
        else
        {
            LogInfo($"清理完成，现在{pathParent.name}的子对象数量: {pathParent.childCount}");
        }

        // Generate new waypoints
        Vector3? lastWaypointPos = null;
        int waypointsCreated = 0;

        if (pathTilemap == null)
        {
            LogError($"无法生成路径点：pathTilemap为null");
            // 创建一个默认的直线路径，防止生成失败
            CreateDefaultPathPoints();
            return;
        }

        // 检查Tilemap是否有任何Tile
        bool hasTiles = false;
        BoundsInt bounds = pathTilemap.cellBounds;
        LogInfo($"Tilemap边界: x={bounds.xMin}-{bounds.xMax}, y={bounds.yMin}-{bounds.yMax}");
        
        // 尝试找到至少一个有效的Tile
        foreach (var pos in pathTilemap.cellBounds.allPositionsWithin)
        {
            if (pathTilemap.HasTile(pos))
            {
                hasTiles = true;
                LogInfo($"在位置({pos.x}, {pos.y})找到Tile");
                break;
            }
        }
        
        if (!hasTiles)
        {
            LogError("Tilemap上没有找到任何Tile！创建默认路径...");
            CreateDefaultPathPoints();
            return;
        }

        foreach (var pos in pathTilemap.cellBounds.allPositionsWithin)
        {
            if (pathTilemap.HasTile(pos))
            {
                Vector3 worldPos = pathTilemap.CellToWorld(pos) + pathTilemap.cellSize / 2;
                LogInfo($"找到Tile: 单元格位置({pos.x}, {pos.y}), 世界位置({worldPos.x}, {worldPos.y}, {worldPos.z})");

                // Check if we need to place a waypoint based on interval
                if (!lastWaypointPos.HasValue ||
                    Vector3.Distance(worldPos, lastWaypointPos.Value) >= waypointInterval)
                {
                    try
                    {
                        // 使用Instantiate替代对象池获取
                        GameObject waypoint = null;
                        
                        if (waypointPrefab != null)
                        {
                            LogInfo($"使用预制体创建路径点，预制体名称: {waypointPrefab.name}");
                            waypoint = Instantiate(waypointPrefab, worldPos, Quaternion.identity);
                        }
                        else
                        {
                            LogError("waypointPrefab为null，使用空游戏对象代替");
                            waypoint = new GameObject($"WP_{waypointsCreated}");
                    waypoint.transform.position = worldPos;
                        }
                        
                        // 确保waypoint成为pathParent的子对象
                        if (pathParent != null)
                        {
                            // 使用worldPositionStays=false，确保保持本地坐标正确
                            waypoint.transform.SetParent(pathParent, false);
                            waypoint.transform.position = worldPos; // 再次确保位置正确
                            waypoint.name = $"Waypoint_{waypointsCreated}";
                    lastWaypointPos = worldPos;
                            waypointsCreated++;
                            LogInfo($"成功创建路径点 {waypointsCreated-1} 在位置: {worldPos}");
                        }
                        else
                        {
                            LogError($"pathParent为null，无法添加子对象！");
                            DestroyImmediate(waypoint);
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogError($"创建路径点时发生异常: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }

        if (pathParent != null)
        {
            LogInfo($"为{pathType}路径生成了{waypointsCreated}个路径点，pathParent.childCount = {pathParent.childCount}");
            
            // 再次验证子对象
            if (pathParent.childCount == 0 && waypointsCreated > 0)
            {
                LogError($"尽管创建了{waypointsCreated}个路径点，但{pathParent.name}仍然没有子对象！创建默认路径...");
                CreateDefaultPathPoints();
            }
            else if (pathParent.childCount == 0)
            {
                LogError($"没有生成任何路径点！创建默认路径...");
                CreateDefaultPathPoints();
            }
            else
            {
                // 输出前5个路径点的位置
                int countToShow = Mathf.Min(5, pathParent.childCount);
                for (int i = 0; i < countToShow; i++)
                {
                    LogInfo($"路径点 {i} 位置: {pathParent.GetChild(i).position}");
                }
            }
        }
    }

    // 创建默认的路径点
    private void CreateDefaultPathPoints()
    {
        LogInfo("创建默认路径点...");
        
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

    void OnDrawGizmos()
    {
        if (pathParent == null || pathParent.childCount == 0)
            return;

        Gizmos.color = gizmoColor;

        // Draw waypoints and connections
        for (int i = 0; i < pathParent.childCount; i++)
        {
            Transform waypoint = pathParent.GetChild(i);
            Gizmos.DrawSphere(waypoint.position, gizmoRadius);

            if (i > 0)
            {
                Transform prevWaypoint = pathParent.GetChild(i - 1);
                Gizmos.DrawLine(prevWaypoint.position, waypoint.position);
            }
        }
    }
    
    private void LogInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[{pathType}PathGenerator] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[{pathType}PathGenerator] {message}");
    }
}