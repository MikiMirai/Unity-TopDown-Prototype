using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Rank order – matches the order of <c>rankMultipliers</c>.
/// </summary>
public enum ComboRank { D, C, B, A, S }

public class ComboSystem : MonoBehaviour
{
    #region Inspector Fields
    [Header("Combo Settings")]
    [Tooltip("Time window in seconds to continue the combo after the last light attack.")]
    public float comboTimeout = 2f;
    [Tooltip("Maximum number of light attacks that will be counted while you’re on rank S.")]
    public int maxComboCount = 120;

    // Thresholds for each rank (inclusive), e.g. { 1,3,5,8 } means:
    [Tooltip("Hit count thresholds for each rank (D,C,B,A,S).")]
    public int[] rankThresholds = new int[] { 1, 3, 5, 8 }; // 4 thresholds => 5 ranks

    [Header("Damage Multipliers per Rank (D to S)")]
    [Tooltip("Multiplier applied to the heavy attack when a combo of that rank is used.")]
    public float[] rankMultipliers = new float[] { 1f, 1.5f, 2f, 2.5f, 3.5f };

    [Header("UI References")]
    public TMP_Text comboText;
    [Tooltip("Format string for the UI text – use {0} for hit count and {1} for rank.")]
    public string comboDisplayFormat = "Combo: {0} [{1}]";
    #endregion

    #region Internal State
    private int currentComboCount = 0;
    private int currentRankIndex = 0; // 0 -> D, 1 -> C, … 4 -> S
    private Coroutine timeoutCoroutine;

    // Event that fires whenever the combo state changes (count or rank)
    public UnityEvent OnComboChanged;
    #endregion

    #region Unity Cycles
    private void Awake()
    {
        ValidateConfig();
        UpdateUI(); // In case the UI is enabled on start
    }
    #endregion

    #region Public API
    /// <summary>
    /// Call this every time a successful light melee attack occurs.
    /// </summary>
    public void RegisterLightAttack()
    {
        if (currentComboCount < maxComboCount)
            currentComboCount++;

        // Restart the timeout coroutine
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ComboTimeout());

        UpdateRank();
        UpdateUI();
    }

    /// <summary>
    /// Call this when a heavy attack actually hits an enemy.
    /// Returns the final damage after applying the combo multiplier.
    /// </summary>
    public float ConsumeComboForHeavyAttack(float baseDamage)
    {
        float multiplier = GetCurrentMultiplier();

        // Consume & reset immediately
        ResetCombo();

        return baseDamage * multiplier;
    }

    /// <summary>Whether a combo is currently active.</summary>
    public bool IsComboActive => currentComboCount > 0;

    /// <summary>Get the current rank as an enum value.</summary>
    public ComboRank CurrentRank => (ComboRank)currentRankIndex;

    /// <summary>Get the raw multiplier for the current combo state.</summary>
    public float GetCurrentMultiplier() => IsComboActive ? rankMultipliers[currentRankIndex] : 1f;

    /// <summary>Manually reset the combo (e.g., when player dies).</summary>
    public void ForceResetCombo() => ResetCombo();
    #endregion

    #region Core Logic
    /// <summary>Resets combo state and stops any running timeout.</summary>
    private void ResetCombo()
    {
        currentComboCount = 0;
        currentRankIndex = 0;

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        UpdateUI();
        OnComboChanged?.Invoke();
    }

    /// <summary>Determine the rank index (0‑4) from the hit count.</summary>
    private void UpdateRank()
    {
        currentRankIndex = GetRankIndexFromCount(currentComboCount);
    }

    /// <summary>Coroutine that resets the combo after a timeout.</summary>
    private IEnumerator ComboTimeout()
    {
        yield return new WaitForSeconds(comboTimeout);
        ResetCombo();
    }
    #endregion

    #region UI Helpers
    private void UpdateUI()
    {
        if (comboText == null) return;

        bool hasCombo = IsComboActive;

        // If we’re not in a combo, clear the text and exit early
        if (!hasCombo)
        {
            comboText.text = string.Empty;
            return;
        }

        // If we have a combo show it
        string rankStr = GetRankFromCount(currentComboCount);
        int displayCnt = Mathf.Min(currentComboCount, maxComboCount);

        comboText.text = string.Format(comboDisplayFormat, displayCnt, rankStr);
        UpdateUIColors(rankStr);
    }

    private void UpdateUIColors(string rank)
    {
        // Simple color mapping
        comboText.color = rank switch
        {
            "S" => new Color(1f, 0.8f, 0f),
            "A" => new Color(1f, 0.3f, 0.8f),
            "B" => Color.cyan,
            "C" => Color.green,
            "D" => Color.yellow,
            _ => Color.white,
        };
    }
    #endregion

    #region Validation
    private void ValidateConfig()
    {
        if (rankMultipliers == null || rankMultipliers.Length < 5)
        {
            Debug.LogWarning("[MeleeComboSystem] rankMultipliers array is too short. " +
                             $"Adding default values to reach 5 elements.");
            Array.Resize(ref rankMultipliers, 5);
            for (int i = 0; i < rankMultipliers.Length; i++)
                if (rankMultipliers[i] == 0f) rankMultipliers[i] = 1.2f * (i + 1);
        }

        // Ensure thresholds array has one less element than ranks
        if (rankThresholds.Length != rankMultipliers.Length - 1)
        {
            Debug.LogWarning("[MeleeComboSystem] rankThresholds length should be " +
                             $"{rankMultipliers.Length - 1}. Adjusting automatically.");
            Array.Resize(ref rankThresholds, rankMultipliers.Length - 1);
            for (int i = 0; i < rankThresholds.Length; i++)
                if (rankThresholds[i] == 0) rankThresholds[i] = (i + 1) * 2;
        }
    }
    #endregion

    #region Rank Helpers (the requested method)

    /// <summary>
    /// Returns the rank string ("D","C","B","A" or "S") for a given hit count.
    /// </summary>
    private string GetRankFromCount(int count)
    {
        if (count <= 0) return "";

        // Walk through thresholds: D, C, B, A
        for (int i = 0; i < rankThresholds.Length; i++)
            if (count <= rankThresholds[i])
                return ((ComboRank)i).ToString();   // e.g. ComboRank.C -> "C"

        // Anything beyond the last threshold is rank S
        return ComboRank.S.ToString();
    }

    /// <summary>
    /// Same logic as GetRankFromCount but returns the enum index (0‑4) for internal use.
    /// </summary>
    private int GetRankIndexFromCount(int count)
    {
        if (count <= 0) return 0;

        for (int i = 0; i < rankThresholds.Length; i++)
            if (count <= rankThresholds[i])
                return i;

        return (int)ComboRank.S;
    }

    #endregion
}
