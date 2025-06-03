using UnityEngine;

public class LandPathGenerator : PathGenerator
{
    void Reset()
    {
        // Set default values for land path
        pathType = PathType.Land;
        gizmoColor = new Color(0.6f, 0.4f, 0.2f); // Brown color for land
        gizmoRadius = 0.3f;
        waypointInterval = 1f;
    }

    [ContextMenu("Generate Land Path")]
    public void GenerateLandPath()
    {
        base.GenerateWaypoints();
    }
}