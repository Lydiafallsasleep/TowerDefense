using UnityEngine;

/// <summary>
/// 面板位置调整器 - 允许在游戏运行时动态调整面板位置
/// </summary>
public class PanelPositionAdjuster : MonoBehaviour
{
    [Header("调整对象")]
    public PanelFollower targetPanel;  // 要调整的面板
    
    [Header("调整设置")]
    public KeyCode toggleKey = KeyCode.F1;   // 开关调整模式的按键
    public KeyCode resetKey = KeyCode.F2;    // 重置位置的按键
    public float moveSpeed = 100f;           // 移动速度
    public float screenStep = 10f;           // 屏幕空间调整步长
    
    [Header("状态")]
    public bool adjustmentMode = false;      // 是否处于调整模式
    public Vector2 currentScreenOffset;      // 当前的屏幕空间偏移
    
    // 初始设置
    private void Start()
    {
        if (targetPanel != null)
        {
            // 获取初始偏移量
            currentScreenOffset = new Vector2(targetPanel.screenOffsetX, targetPanel.screenOffsetY);
        }
    }
    
    private void Update()
    {
        // 按下F1键切换调整模式
        if (Input.GetKeyDown(toggleKey))
        {
            adjustmentMode = !adjustmentMode;
            Debug.Log(adjustmentMode ? "面板位置调整模式: <color=green>开启</color>" : "面板位置调整模式: <color=red>关闭</color>");
        }
        
        // 按下F2键重置位置
        if (Input.GetKeyDown(resetKey) && targetPanel != null)
        {
            currentScreenOffset = new Vector2(0f, 100f);
            targetPanel.SetScreenOffset(currentScreenOffset.x, currentScreenOffset.y);
            Debug.Log($"面板位置已重置为初始值: {currentScreenOffset}");
        }
        
        // 如果不在调整模式或没有目标面板，不执行下面的代码
        if (!adjustmentMode || targetPanel == null) return;
        
        // 使用方向键调整屏幕偏移
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        if (horizontalInput != 0 || verticalInput != 0)
        {
            // 计算新的偏移值
            currentScreenOffset.x += horizontalInput * screenStep * Time.deltaTime * moveSpeed;
            currentScreenOffset.y += verticalInput * screenStep * Time.deltaTime * moveSpeed;
            
            // 应用新的偏移值
            targetPanel.SetScreenOffset(currentScreenOffset.x, currentScreenOffset.y);
            
            // 打印当前偏移值
            Debug.Log($"面板偏移已调整为: X={currentScreenOffset.x:F1}, Y={currentScreenOffset.y:F1}");
        }
    }
    
    void OnGUI()
    {
        // 如果处于调整模式，显示一个简单的HUD
        if (adjustmentMode && targetPanel != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(10, 10, 5, 5);
            
            string info = $"面板位置调整模式\n" +
                          $"偏移: X={currentScreenOffset.x:F1}, Y={currentScreenOffset.y:F1}\n" +
                          $"使用方向键调整 | {resetKey}键重置 | {toggleKey}键退出";
            
            GUI.Box(new Rect(10, 10, 300, 60), info, style);
        }
    }
} 