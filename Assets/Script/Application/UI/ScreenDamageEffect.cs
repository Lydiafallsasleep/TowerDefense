using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Screen damage effect for showing visual feedback when player takes damage
/// </summary>
public class ScreenDamageEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    public Image damageImage;               // The UI image that will be used for the effect
    public float flashSpeed = 5f;           // How fast the image will fade
    public Color flashColor = new Color(1f, 0f, 0f, 0.3f);  // Red with some transparency
    
    private Color originalColor;
    private bool isDamaged = false;
    
    void Awake()
    {
        // If no damage image is assigned, try to get it from this GameObject
        if (damageImage == null)
        {
            damageImage = GetComponent<Image>();
        }
        
        if (damageImage != null)
        {
            originalColor = damageImage.color;
            // Make sure the image is invisible at start
            damageImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        else
        {
            Debug.LogWarning("[ScreenDamageEffect] No damage image assigned!");
        }
    }
    
    void Update()
    {
        // If we're damaged, fade the damage image
        if (isDamaged && damageImage != null)
        {
            // Fade the damage image
            damageImage.color = Color.Lerp(damageImage.color, new Color(originalColor.r, originalColor.g, originalColor.b, 0f), flashSpeed * Time.deltaTime);
            
            // If the alpha is almost zero, we're no longer damaged
            if (damageImage.color.a < 0.05f)
            {
                isDamaged = false;
                damageImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            }
        }
    }
    
    /// <summary>
    /// Show damage effect when player takes damage
    /// </summary>
    public void ShowDamageEffect()
    {
        if (damageImage != null)
        {
            isDamaged = true;
            damageImage.color = flashColor;
        }
    }
    
    /// <summary>
    /// Set custom flash color
    /// </summary>
    public void SetFlashColor(Color color)
    {
        flashColor = color;
    }
    
    /// <summary>
    /// Set custom flash speed
    /// </summary>
    public void SetFlashSpeed(float speed)
    {
        flashSpeed = speed;
    }
} 