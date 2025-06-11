using UnityEngine;

/// <summary>
/// 使面板跟随目标物体移动的组件
/// </summary>
public class PanelFollower : MonoBehaviour
{
    [Header("跟随设置")]
    public GameObject targetObject;   // 目标物体
    public Vector3 offset = new Vector3(0, 1.5f, 0);  // 相对目标物体的偏移量
    [Tooltip("如果启用，将移动整个Canvas而不只是面板")]
    public bool moveEntireCanvas = false;    // 是否移动整个Canvas
    
    [Header("高级设置")]
    [Tooltip("是否使用屏幕空间偏移而不是世界空间偏移")]
    public bool useScreenOffset = false;
    [Tooltip("屏幕空间X轴偏移(单位:像素)")]
    public float screenOffsetX = 0f;
    [Tooltip("屏幕空间Y轴偏移(单位:像素)")]
    public float screenOffsetY = 100f;
    [Tooltip("是否根据鼠标位置动态调整位置")]
    public bool useMousePosition = false;
    [Tooltip("是否限制在屏幕内")]
    public bool keepOnScreen = true;
    [Tooltip("与屏幕边缘的最小距离")]
    public float screenMargin = 20f;
    
    [Header("引用")]
    public RectTransform panelRect;   // 面板的RectTransform组件
    
    private Canvas parentCanvas;      // 父Canvas组件
    private Camera uiCamera;          // UI相机引用
    
    void Start()
    {
        // 获取父Canvas组件
        parentCanvas = GetComponentInParent<Canvas>();
        
        // 获取UI相机
        if (parentCanvas != null && parentCanvas.worldCamera != null)
        {
            uiCamera = parentCanvas.worldCamera;
        }
        else
        {
            uiCamera = Camera.main;
        }
        
        // 如果没有指定panelRect，尝试获取自身的RectTransform
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }
        
        // 确保Canvas设置为World Space模式
        if (moveEntireCanvas && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("要移动整个Canvas，Canvas必须设置为World Space模式!");
            moveEntireCanvas = false;
        }
    }
    
    void Update()
    {
        // 如果没有目标物体，则不执行
        if (targetObject == null)
            return;
            
        // 如果移动整个Canvas
        if (moveEntireCanvas && parentCanvas != null)
        {
            MoveCanvasToTarget();
            return;
        }
        
        // 否则只移动面板
        if (panelRect == null)
            return;
            
        // 获取目标物体的世界坐标
        Vector3 worldPosition = targetObject.transform.position;
        
        if (useScreenOffset || useMousePosition)
        {
            // 将目标世界坐标转换为屏幕坐标
            Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPosition);
            
            if (useMousePosition)
            {
                // 使用鼠标位置替代目标位置
                screenPos = Input.mousePosition;
            }
            
            // 应用屏幕空间偏移
            screenPos.x += screenOffsetX;
            screenPos.y += screenOffsetY;
            
            if (keepOnScreen)
            {
                // 限制在屏幕内
                screenPos.x = Mathf.Clamp(screenPos.x, screenMargin, Screen.width - screenMargin);
                screenPos.y = Mathf.Clamp(screenPos.y, screenMargin, Screen.height - screenMargin);
            }
            
            // 如果使用World Space Canvas模式
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // 将屏幕坐标转回世界坐标
                worldPosition = uiCamera.ScreenToWorldPoint(new Vector3(
                    screenPos.x, 
                    screenPos.y, 
                    Vector3.Distance(uiCamera.transform.position, parentCanvas.transform.position)));
                
                // 转换为Canvas本地坐标
                Vector3 localPosition = parentCanvas.transform.InverseTransformPoint(worldPosition);
                
                // 设置面板的本地位置
                panelRect.localPosition = localPosition;
            }
            else // Screen Space模式
            {
                // 转换为Canvas本地坐标
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    screenPos,
                    uiCamera,
                    out localPoint);
                
                // 设置面板的本地位置
                panelRect.anchoredPosition = localPoint;
            }
        }
        else // 使用世界空间偏移
        {
            // 应用世界空间偏移
            worldPosition += offset;
            
            // 如果使用World Space Canvas模式
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // 转换为Canvas本地坐标
                Vector3 localPosition = parentCanvas.transform.InverseTransformPoint(worldPosition);
                
                // 设置面板的本地位置
                panelRect.localPosition = localPosition;
            }
            else // Screen Space模式
            {
                // 将世界坐标转换为屏幕坐标
                Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPosition);
                
                // 转换为Canvas本地坐标
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    screenPos,
                    uiCamera,
                    out localPoint);
                
                // 设置面板的本地位置
                panelRect.anchoredPosition = localPoint;
            }
        }
    }
    
    /// <summary>
    /// 移动整个Canvas到目标位置
    /// </summary>
    private void MoveCanvasToTarget()
    {
        if (parentCanvas == null || parentCanvas.renderMode != RenderMode.WorldSpace)
            return;
            
        // 获取目标世界坐标
        Vector3 targetPosition = targetObject.transform.position;
        
        // 应用偏移量
        Vector3 finalPosition = targetPosition + offset;
        
        // 设置Canvas的世界坐标
        parentCanvas.transform.position = finalPosition;
        
        Debug.Log($"Canvas位置已设置为: {finalPosition}, 目标位置: {targetPosition}");
    }
    
    /// <summary>
    /// 设置跟随的目标物体
    /// </summary>
    /// <param name="target">要跟随的目标物体</param>
    public void SetTarget(GameObject target)
    {
        targetObject = target;
        
        // 如果有目标，立即更新位置
        if (targetObject != null)
        {
            // 触发立即更新
            Update();
        }
    }
    
    /// <summary>
    /// 切换是否移动整个Canvas
    /// </summary>
    public void ToggleMoveEntireCanvas(bool value)
    {
        moveEntireCanvas = value;
        
        // 确保在World Space模式下才能移动Canvas
        if (moveEntireCanvas && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("要移动整个Canvas，Canvas必须设置为World Space模式!");
            moveEntireCanvas = false;
        }
        
        // 立即更新位置
        if (gameObject.activeInHierarchy && targetObject != null)
        {
            Update();
        }
    }
    
    /// <summary>
    /// 设置自定义偏移量
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        // 立即更新位置
        if (gameObject.activeInHierarchy && targetObject != null)
            Update();
    }
    
    /// <summary>
    /// 设置屏幕空间偏移
    /// </summary>
    public void SetScreenOffset(float x, float y)
    {
        useScreenOffset = true;
        screenOffsetX = x;
        screenOffsetY = y;
        // 立即更新位置
        if (gameObject.activeInHierarchy && targetObject != null)
            Update();
    }
} 