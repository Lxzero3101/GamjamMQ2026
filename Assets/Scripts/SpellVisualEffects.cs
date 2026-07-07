using System.Collections;
using UnityEngine;

/// <summary>
/// Optional polish script. Listens to SpellManager's success/failure events and handles
/// the *visual* flourish on the drawn stroke — particles/glow on success, red-and-fizzle
/// on failure, plus a touch-platform haptic pulse for either outcome. Keeps this juice out
/// of SpellManager so the game-rules script doesn't get cluttered with animation details.
///
/// This script only reacts to LineRenderer instances it's handed by the event — it never
/// destroys them. SpellManager owns the line's lifetime (it calls IncantationDrawer.ClearCurrentLine()
/// after its own delay), so this script just needs to finish its animation within roughly
/// that same window.
/// </summary>
public class SpellVisualEffects : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SpellManager _spellManager;

    [Header("Success Feedback")]
    [Tooltip("Particle system prefab spawned along the drawn line on success (e.g. sparkles/glow).")]
    [SerializeField] private ParticleSystem _successParticlesPrefab;
    [Tooltip("How many points along the line get a particle burst. Higher = denser trail effect.")]
    [SerializeField] private int _successParticlesPerLine = 8;
    [SerializeField] private Color _successLineColor = new Color(0.4f, 0.9f, 1f, 1f);
    [Tooltip("How long the success glow color transition takes.")]
    [SerializeField] private float _successGlowDuration = 0.3f;

    [Header("Failure Feedback")]
    [SerializeField] private Color _failureLineColor = Color.red;
    [Tooltip("Total time for the line to redden then fade to transparent. " +
             "Keep this roughly in line with SpellManager's failure clear delay.")]
    [SerializeField] private float _failureFizzleDuration = 1f;
    [Tooltip("Portion (0-1) of _failureFizzleDuration spent turning red before the fade-out begins.")]
    [SerializeField] private float _failureRedenPortion = 0.35f;

    [Header("Haptics")]
    [Tooltip("Vibrate on touch platforms for success/failure. Cheap and this is a touch-friendly game.")]
    [SerializeField] private bool _useHapticsOnMobile = true;

    private void OnEnable()
    {
        if (_spellManager == null) return;
        _spellManager.OnSpellSuccess += HandleSpellSuccess;
        _spellManager.OnSpellFailed += HandleSpellFailed;
    }

    private void OnDisable()
    {
        if (_spellManager == null) return;
        _spellManager.OnSpellSuccess -= HandleSpellSuccess;
        _spellManager.OnSpellFailed -= HandleSpellFailed;
    }

    private void HandleSpellSuccess(SpellData spell, float score, LineRenderer line)
    {
        if (line != null)
        {
            SpawnSuccessParticles(line);
            StartCoroutine(GlowLine(line));
        }

        TriggerHaptic();
    }

    private void HandleSpellFailed(SpellData spell, float score, LineRenderer line)
    {
        if (line != null)
        {
            StartCoroutine(FizzleLine(line));
        }

        TriggerHaptic();
    }

    /// <summary>
    /// Scatters short-lived particle bursts along the drawn line's world-space points,
    /// so the "glow" reads as following the exact shape the player drew rather than
    /// a generic effect at one spot.
    /// </summary>
    private void SpawnSuccessParticles(LineRenderer line)
    {
        if (_successParticlesPrefab == null || line.positionCount == 0) return;

        int step = Mathf.Max(1, line.positionCount / Mathf.Max(1, _successParticlesPerLine));

        for (int i = 0; i < line.positionCount; i += step)
        {
            Vector3 worldPoint = line.useWorldSpace ? line.GetPosition(i) : line.transform.TransformPoint(line.GetPosition(i));
            ParticleSystem instance = Instantiate(_successParticlesPrefab, worldPoint, Quaternion.identity);
            instance.Play();

            // Prefab should already be configured to self-destruct (Stop Action: Destroy),
            // but guard with a manual cleanup based on its main duration in case it isn't.
            Destroy(instance.gameObject, instance.main.duration + instance.main.startLifetime.constantMax);
        }
    }

    /// <summary>
    /// Smoothly transitions the line's color to the success color as a quick "glow" pulse.
    /// </summary>
    private IEnumerator GlowLine(LineRenderer line)
    {
        Color startColor = line.startColor;
        Color endColor = line.endColor;
        float elapsed = 0f;

        while (elapsed < _successGlowDuration)
        {
            if (line == null) yield break; // Line may already have been cleared.

            elapsed += Time.deltaTime;
            float t = elapsed / _successGlowDuration;

            line.startColor = Color.Lerp(startColor, _successLineColor, t);
            line.endColor = Color.Lerp(endColor, _successLineColor, t);

            yield return null;
        }
    }

    /// <summary>
    /// Turns the line red, then fades its alpha to zero — the "break apart" failure feedback.
    /// Uses vanishing alpha rather than a shader/physics break for simplicity, per the plan.
    /// </summary>
    private IEnumerator FizzleLine(LineRenderer line)
    {
        Color startColor = line.startColor;
        float redenDuration = _failureFizzleDuration * _failureRedenPortion;
        float fadeDuration = _failureFizzleDuration - redenDuration;

        // Phase 1: transition current color to red.
        float elapsed = 0f;
        while (elapsed < redenDuration)
        {
            if (line == null) yield break;

            elapsed += Time.deltaTime;
            float t = redenDuration > 0f ? elapsed / redenDuration : 1f;

            Color c = Color.Lerp(startColor, _failureLineColor, t);
            line.startColor = c;
            line.endColor = c;

            yield return null;
        }

        // Phase 2: fade alpha to zero from the now-red color.
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (line == null) yield break;

            elapsed += Time.deltaTime;
            float t = fadeDuration > 0f ? elapsed / fadeDuration : 1f;

            float alpha = Mathf.Lerp(1f, 0f, t);
            Color c = _failureLineColor;
            c.a = alpha;
            line.startColor = c;
            line.endColor = c;

            yield return null;
        }
    }

    private void TriggerHaptic()
    {
        if (!_useHapticsOnMobile) return;

#if UNITY_IOS || UNITY_ANDROID
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
#endif
    }
}