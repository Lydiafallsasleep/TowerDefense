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

    void Start()
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
        // Clear existing waypoints
        foreach (Transform child in pathParent)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // Generate new waypoints
        Vector3? lastWaypointPos = null;

        foreach (var pos in pathTilemap.cellBounds.allPositionsWithin)
        {
            if (pathTilemap.HasTile(pos))
            {
                Vector3 worldPos = pathTilemap.CellToWorld(pos) + pathTilemap.cellSize / 2;

                // Check if we need to place a waypoint based on interval
                if (!lastWaypointPos.HasValue ||
                    Vector3.Distance(worldPos, lastWaypointPos.Value) >= waypointInterval)
                {
                    GameObject waypoint = Instantiate(waypointPrefab, worldPos, Quaternion.identity, pathParent);
                    waypoint.name = $"Waypoint_{pathParent.childCount}";
                    lastWaypointPos = worldPos;
                }
            }
        }

        Debug.Log($"Generated {pathParent.childCount} waypoints for {pathType} path");
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
}