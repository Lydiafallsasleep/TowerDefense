using UnityEngine;

public class WaterPathGenerator : PathGenerator
{
    void Reset()
    {
        // Set default values for water path
        pathType = PathType.Water;
        gizmoColor = new Color(0.2f, 0.4f, 0.8f); // Blue color for water
        gizmoRadius = 0.4f; // Slightly larger for visibility
        waypointInterval = 1.2f; // Longer interval for smoother water movement
    }

    [ContextMenu("Generate Water Path")]
    public void GenerateWaterPath()
    {
        base.GenerateWaypoints();
    }
}