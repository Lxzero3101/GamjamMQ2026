using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 1. Added TMPro namespace

/// <summary>
/// The brain of the gameplay loop. Loads the current spell's guide image, listens for
/// completed drawings from IncantationDrawer, scores them via ShapeComparer, checks the
/// success threshold, triggers success/failure feedback (including visible score text),
/// and advances/resets the board.
/// </summary>
public class SpellManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private IncantationDrawer _incantationDrawer;
    [Tooltip("The UI Image on the World Space Canvas that shows the faint guide image to trace.")]
    [SerializeField] private Image _guideImage;
    [Tooltip("Optional UI Text that shows the player their score / success-or-fail result.")]
    [SerializeField] private TextMeshProUGUI _feedbackText; // 2. Changed from Text to TextMeshProUGUI

    [Header("Spell Sequence")]
    [Tooltip("Spells are attempted in this order, looping back to the start after the last one.")]
    [SerializeField] private List<SpellData> _spellSequence;

    [Header("Scoring")]
    [Range(0f, 100f)]
    [Tooltip("Minimum similarity percentage from ShapeComparer required to count as a success.")]
    [SerializeField] private float _successThreshold = 80f;
    [SerializeField] private int _resampleCount = 64;

    [Header("Feedback Timing")]
    [Tooltip("Delay before the line clears and the next spell loads after a success.")]
    [SerializeField] private float _successClearDelay = 1.5f;
    [Tooltip("Delay before the line clears and the same spell resets after a failure.")]
    [SerializeField] private float _failureClearDelay = 1f;

    [Header("Feedback Text")]
    [SerializeField] private string _promptMessage = "Trace the symbol!";
    [SerializeField] private string _successMessageFormat = "✓ {0:0}% — Success!";
    [SerializeField] private string _failureMessageFormat = "✗ {0:0}% — Try Again";
    [SerializeField] private Color _successTextColor = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] private Color _failureTextColor = new Color(0.95f, 0.3f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _successSfx;
    [SerializeField] private AudioClip _failureSfx;

    public event Action<SpellData, float, LineRenderer> OnSpellSuccess;
    public event Action<SpellData, float, LineRenderer> OnSpellFailed;
    public event Action<SpellData> OnSpellChanged;

    private int _currentSpellIndex;

    private SpellData CurrentSpell =>
        (_spellSequence != null && _spellSequence.Count > 0) ? _spellSequence[_currentSpellIndex] : null;

    private void OnEnable()
    {
        if (_incantationDrawer != null)
        {
            _incantationDrawer.OnDrawingCompleted += HandleDrawingCompleted;
            _incantationDrawer.OnDrawingStarted += HandleDrawingStarted;
        }
    }

    private void OnDisable()
    {
        if (_incantationDrawer != null)
        {
            _incantationDrawer.OnDrawingCompleted -= HandleDrawingCompleted;
            _incantationDrawer.OnDrawingStarted -= HandleDrawingStarted;
        }
    }

    private void Start()
    {
        if (_spellSequence == null || _spellSequence.Count == 0)
        {
            Debug.LogError("SpellManager: no spells assigned in _spellSequence.", this);
            return;
        }

        _currentSpellIndex = 0;
        LoadCurrentSpell();
    }

    private void HandleDrawingStarted()
    {
        SetFeedbackText(_promptMessage, Color.white);
    }

    private void HandleDrawingCompleted(List<Vector2> drawnPoints, LineRenderer line)
    {
        SpellData spell = CurrentSpell;
        if (spell == null)
        {
            Debug.LogWarning("SpellManager: drawing completed but no current spell is loaded.", this);
            return;
        }

        float score = ShapeComparer.CompareShape(drawnPoints, spell, _resampleCount);
        bool success = score >= _successThreshold;

        if (success)
        {
            SetFeedbackText(string.Format(_successMessageFormat, score), _successTextColor);
            PlaySfx(_successSfx);
            OnSpellSuccess?.Invoke(spell, score, line);
            StartCoroutine(ClearThenAdvance(_successClearDelay));
        }
        else
        {
            SetFeedbackText(string.Format(_failureMessageFormat, score), _failureTextColor);
            PlaySfx(_failureSfx);
            OnSpellFailed?.Invoke(spell, score, line);
            StartCoroutine(ClearThenRetry(_failureClearDelay));
        }
    }

    private IEnumerator ClearThenAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        _incantationDrawer.ClearCurrentLine();
        AdvanceToNextSpell();
    }

    private IEnumerator ClearThenRetry(float delay)
    {
        yield return new WaitForSeconds(delay);
        _incantationDrawer.ClearCurrentLine();
    }

    private void AdvanceToNextSpell()
    {
        _currentSpellIndex = (_currentSpellIndex + 1) % _spellSequence.Count;
        LoadCurrentSpell();
    }

    private void LoadCurrentSpell()
    {
        SpellData spell = CurrentSpell;
        if (spell == null) return;

        if (_guideImage != null)
            _guideImage.sprite = spell.IncantationGuideImage;

        SetFeedbackText(_promptMessage, Color.white);

        OnSpellChanged?.Invoke(spell);
    }

    private void SetFeedbackText(string message, Color color)
    {
        if (_feedbackText == null) return;
        _feedbackText.text = message;
        _feedbackText.color = color;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
            _audioSource.PlayOneShot(clip);
    }
}