using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container for a single spell/incantation shape.
/// Create instances via Assets > Create > Spell Drawing > Spell Data.
/// One asset per spell — no code changes needed to add new spells.
/// </summary>
[CreateAssetMenu(fileName = "NewSpellData", menuName = "Spell Drawing/Spell Data")]
public class SpellData : ScriptableObject
{
    [Header("Spell Info")]
    [SerializeField] private string _spellName;

    [Tooltip("The faint background image the player traces over, shown on the World Space Canvas.")]
    [SerializeField] private Sprite _incantationGuideImage;

    [Header("Template Shape")]
    [Tooltip("The ideal shape points that define this spell's rune. " +
             "These do NOT need to be pre-normalized — ShapeComparer normalizes them at runtime. " +
             "You can author these by hand, or (recommended) use a small editor tool later to draw " +
             "a shape once and capture its points automatically.")]
    [SerializeField] private List<Vector2> _templatePoints = new List<Vector2>();

    // ---- Public read-only accessors ----
    // Exposed as properties rather than public fields so external scripts
    // can read but never accidentally overwrite spell data at runtime.
    public string SpellName => _spellName;
    public Sprite IncantationGuideImage => _incantationGuideImage;
    public IReadOnlyList<Vector2> TemplatePoints => _templatePoints;

    // ---- Normalization cache ----
    // ShapeComparer will normalize _templatePoints (resample, center, scale) once
    // and store the result here, since the template never changes at runtime.
    // We also cache a reversed copy to support the "compare both directions"
    // fix for start-point/direction sensitivity.
    // These are not [SerializeField] — they're runtime-only derived data, not authored data.
    private List<Vector2> _cachedNormalizedPoints;
    private List<Vector2> _cachedReversedNormalizedPoints;

    public bool HasCachedNormalizedPoints => _cachedNormalizedPoints != null;

    public IReadOnlyList<Vector2> CachedNormalizedPoints => _cachedNormalizedPoints;
    public IReadOnlyList<Vector2> CachedReversedNormalizedPoints => _cachedReversedNormalizedPoints;

    /// <summary>
    /// Called by ShapeComparer (or SpellManager) the first time this spell is compared,
    /// so the (relatively) expensive normalization work only happens once per spell,
    /// not on every single comparison attempt.
    /// </summary>
    public void SetCachedNormalizedPoints(List<Vector2> normalized, List<Vector2> reversedNormalized)
    {
        _cachedNormalizedPoints = normalized;
        _cachedReversedNormalizedPoints = reversedNormalized;
    }

    // If someone edits the template points in the Inspector at edit time,
    // invalidate the cache so it gets recomputed with the new shape instead
    // of silently comparing against stale data.
    private void OnValidate()
    {
        _cachedNormalizedPoints = null;
        _cachedReversedNormalizedPoints = null;

        if (_templatePoints != null && _templatePoints.Count > 0 && _templatePoints.Count < 2)
        {
            Debug.LogWarning($"SpellData '{name}': needs at least 2 template points to form a shape.", this);
        }
    }
}