using UnityEngine;
using UnityEditor;

/// <summary>
/// 这个脚本用于修复Unity编辑器中AudioClipInspector的空引用错误
/// 通过添加一个空的OnPreviewGUI处理函数来捕获错误
/// </summary>
[CustomEditor(typeof(AudioClip))]
public class AudioClipInspectorFix : Editor
{
    // 原始的AudioClipInspector
    private Editor defaultEditor;
    
    void OnEnable()
    {
        // 获取默认的音频剪辑编辑器
        if (defaultEditor == null)
        {
            // 创建默认编辑器，排除我们自己的类以避免递归
            defaultEditor = CreateEditor(target, System.Type.GetType("UnityEditor.AudioImporterInspector, UnityEditor"));
        }
    }
    
    void OnDisable()
    {
        // 销毁默认编辑器
        if (defaultEditor != null)
        {
            DestroyImmediate(defaultEditor);
            defaultEditor = null;
        }
    }
    
    // 捕获OnPreviewGUI调用，防止出现空引用异常
    public override void OnPreviewGUI(Rect rect, GUIStyle background)
    {
        try
        {
            // 如果有可用的默认编辑器，使用它
            if (defaultEditor != null)
            {
                defaultEditor.OnPreviewGUI(rect, background);
            }
            else
            {
                // 提供一个简单的回退预览
                EditorGUI.LabelField(rect, "Audio Preview", EditorStyles.centeredGreyMiniLabel);
            }
        }
        catch (System.NullReferenceException)
        {
            // 捕获并忽略空引用异常
            EditorGUI.LabelField(rect, "Audio Preview Unavailable", EditorStyles.centeredGreyMiniLabel);
        }
    }
    
    // 转发所有其他inspector功能到默认编辑器
    public override void OnInspectorGUI()
    {
        if (defaultEditor != null)
        {
            defaultEditor.OnInspectorGUI();
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
} 