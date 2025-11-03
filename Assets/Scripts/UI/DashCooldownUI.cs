using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField, Tooltip("The Slider component (auto-assigned if on same object)")]
    private Slider slider;

    [SerializeField, Tooltip("Optional: Text to show countdown or 'Ready'")]
    private TextMeshProUGUI cooldownText;

    [Header("Display Settings")]
    [SerializeField, Tooltip("Show numeric countdown (e.g. 0.8)")]
    private bool showCountdown = true;

    [SerializeField, Tooltip("Number format (e.g. '0.0' for 1 decimal)")]
    private string timeFormat = "0.0";

    [SerializeField, Tooltip("Hide slider when dash is ready?")]
    private bool hideWhenReady = true;

    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color cooldownColor = Color.red;

    private Image fillImage;

    private void Awake()
    {
        // Auto-assign slider if not set
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider == null)
            Debug.LogError("DashCooldownUI: No Slider component found!", this);

        // Set slider range
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f; // Start ready

        fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
            fillImage.color = readyColor;
    }

    /// <summary>
    /// Call this from PlayerMovement every frame or when dash state changes.
    /// </summary>
    /// <param name="currentCooldown">Remaining cooldown time</param>
    /// <param name="maxCooldown">Full cooldown duration (from dashCooldown)</param>
    public void UpdateDashCooldown(float currentCooldown, float maxCooldown)
    {
        if (slider == null) return;

        bool isOnCooldown = currentCooldown > 0f;
        float fillAmount = maxCooldown > 0f ? currentCooldown / maxCooldown : 0f;

        // ---- Reverse fill: 1 when ready, 0 when on cooldown -----
        slider.value = 1f - fillAmount;

        // Optional: Hide when ready
        if (hideWhenReady)
            slider.gameObject.SetActive(isOnCooldown);

        // Update text
        if (cooldownText != null)
        {
            if (showCountdown && isOnCooldown)
                cooldownText.text = currentCooldown.ToString(timeFormat);
            else
                cooldownText.text = "Dash Ready";
        }

        // ----- COLOR LERP (red to green) -----
        if (fillImage != null)
            fillImage.color = Color.Lerp(cooldownColor, readyColor, slider.value);
    }

    /// <summary>
    /// Optional method: Force show "Ready" state
    /// </summary>
    public void ShowReady()
    {
        UpdateDashCooldown(0f, 1f);
    }
}
