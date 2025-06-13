using UnityEngine;

/// <summary>
/// 碰撞体检测工具 - 用于检测场景中点击位置是否有碰撞体
/// </summary>
public class CollisionDetector : MonoBehaviour
{
    [Header("设置")]
    public KeyCode activateKey = KeyCode.C; // 按C键激活检测
    public float rayDistance = 100f;        // 射线长度
    public Color rayColor = Color.red;      // 射线颜色
    
    [Header("调试信息")]
    [SerializeField] private string lastHitObjectName;
    [SerializeField] private Vector3 lastHitPoint;
    [SerializeField] private bool didHitCollider;
    
    private Camera mainCamera;
    private LineRenderer lineRenderer;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        // 创建LineRenderer用于可视化射线
        if (GetComponent<LineRenderer>() == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.3f);
            lineRenderer.positionCount = 2;
        }
        else
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        lineRenderer.enabled = false;
    }
    
    private void Update()
    {
        // 按下激活键进行碰撞检测
        if (Input.GetKeyDown(activateKey))
        {
            DetectCollision();
        }
        
        // 按住激活键时持续检测
        if (Input.GetKey(activateKey))
        {
            UpdateRayVisualization();
        }
        else if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
        }
    }
    
    /// <summary>
    /// 执行碰撞检测
    /// </summary>
    public void DetectCollision()
    {
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // 显示射线可视化
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, ray.origin + ray.direction * rayDistance);
        }
        
        // 执行射线检测
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            didHitCollider = true;
            lastHitObjectName = hit.collider.gameObject.name;
            lastHitPoint = hit.point;
            
            Debug.Log($"<color=green>击中碰撞体</color>: {lastHitObjectName}");
            Debug.Log($"碰撞体类型: {hit.collider.GetType()}");
            Debug.Log($"碰撞点: {hit.point}");
            Debug.Log($"碰撞法线: {hit.normal}");
            
            // 如果有MeshCollider，显示三角形信息
            if (hit.collider is MeshCollider)
            {
                Debug.Log($"击中的三角形索引: {hit.triangleIndex}");
            }
        }
        else
        {
            didHitCollider = false;
            lastHitObjectName = "无";
            lastHitPoint = ray.GetPoint(rayDistance);
            
            Debug.Log("<color=red>未击中任何碰撞体</color>");
        }
    }
    
    /// <summary>
    /// 更新射线可视化
    /// </summary>
    private void UpdateRayVisualization()
    {
        if (mainCamera == null || lineRenderer == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, ray.origin);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // 如果射线击中，绘制到击中点
            lineRenderer.SetPosition(1, hit.point);
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = new Color(0, 1, 0, 0.3f);
        }
        else
        {
            // 如果未击中，绘制整个射线
            lineRenderer.SetPosition(1, ray.origin + ray.direction * rayDistance);
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.3f);
        }
    }
    
    /// <summary>
    /// 在场景视图中绘制辅助线
    /// </summary>
    private void OnDrawGizmos()
    {
        if (didHitCollider && lastHitPoint != Vector3.zero)
        {
            // 在击中点绘制球体
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastHitPoint, 0.2f);
            
            // 绘制标签
#if UNITY_EDITOR
            UnityEditor.Handles.Label(lastHitPoint, $"Hit: {lastHitObjectName}");
#endif
        }
    }
} 