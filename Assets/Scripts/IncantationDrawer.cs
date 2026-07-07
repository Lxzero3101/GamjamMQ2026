//Minigame: Spell Drawing


using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Captures the player's drawing by listening to InputReader, converts screen input into
/// world space points (to match the World Space Canvas holding the incantation guide image),
/// and renders the stroke live via an instantiated LineRenderer.
/// Only responsible for *capturing* the line — knows nothing about scoring or spell logic.
///
/// Only one stroke is "active" at a time. A new stroke is ignored (ignored input, not queued)
/// until the previous one has been cleared via ClearCurrentLine() — this is what SpellManager
/// calls after its success/failure feedback delay. Without this gate, starting a second stroke
/// while the first one's delayed clear is still pending causes the pending clear to destroy
/// the *new* line instead of the old one.
/// </summary>
public class IncantationDrawer : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private LineRenderer _lineRendererPrefab;
    [SerializeField] private Transform _lineContainer;

    [Header("Draw Settings")]
    [SerializeField] private Camera _worldCamera;
    [Tooltip("Distance from the camera to the drawing plane, in world units. " +
             "Must match your camera setup (e.g. an orthographic camera at z=-10 looking at a z=0 plane needs 10).")]
    [SerializeField] private float _distanceFromCamera = 10f;
    [Tooltip("Minimum distance (world units) the pointer must move before a new point is added.")]
    [SerializeField] private float _minPointDistance = 0.1f;
    [SerializeField] private int _minPointsForValidDrawing = 2;

    // Fired the moment a new stroke actually starts (i.e. wasn't ignored because a previous
    // line was still active). Lets SpellManager clear old feedback text/UI for the new attempt.
    public event Action OnDrawingStarted;

    // Fired when the player lifts their finger/mouse with a valid drawing.
    // Passes the raw world-space points AND the LineRenderer instance so downstream
    // scripts (SpellManager / SpellVisualEffects) can animate that exact line
    // (turn it red, add particles, fizzle it, etc.) before it's cleared.
    public event Action<List<Vector2>, LineRenderer> OnDrawingCompleted;

    private LineRenderer _currentLine;
    private List<Vector2> _currentPoints;
    private Vector2 _lastSampledPoint;
    private bool _isDrawing;

    // True from the moment a stroke starts until ClearCurrentLine() is called on it.
    // Gates HandleDrawStarted so only one stroke can be "in flight" (drawing, or
    // finished-but-awaiting-clear) at any given time.
    private bool _isLineActive;

    private void OnEnable()
    {
        if (_inputReader == null) return;
        _inputReader.OnDrawStarted += HandleDrawStarted;
        _inputReader.OnDrawCanceled += HandleDrawCanceled;
    }

    private void OnDisable()
    {
        if (_inputReader == null) return;
        _inputReader.OnDrawStarted -= HandleDrawStarted;
        _inputReader.OnDrawCanceled -= HandleDrawCanceled;
    }

    private void Update()
    {
        if (!_isDrawing) return;

        Vector2 worldPoint = ScreenToDrawPlane(_inputReader.CurrentMouseOrTouchPosition);
        if (Vector2.Distance(worldPoint, _lastSampledPoint) >= _minPointDistance)
        {
            AddPoint(worldPoint);
        }
    }

    private void HandleDrawStarted()
    {
        // Ignore new strokes entirely while one is still active (drawing, or finished
        // and waiting on SpellManager's feedback delay before being cleared).
        if (_isLineActive) return;

        _isLineActive = true;
        _currentLine = Instantiate(_lineRendererPrefab, _lineContainer != null ? _lineContainer : transform);
        _currentLine.positionCount = 0;
        _currentPoints = new List<Vector2>();
        _isDrawing = true;

        AddPoint(ScreenToDrawPlane(_inputReader.CurrentMouseOrTouchPosition));

        OnDrawingStarted?.Invoke();
    }

    private void HandleDrawCanceled()
    {
        // If we never actually started a stroke (because HandleDrawStarted ignored the
        // input above), there's nothing to cancel — bail out rather than re-processing
        // stale _currentPoints/_currentLine from a previous stroke.
        if (!_isDrawing) return;

        _isDrawing = false;

        if (_currentPoints != null && _currentPoints.Count >= _minPointsForValidDrawing)
        {
            OnDrawingCompleted?.Invoke(_currentPoints, _currentLine);
        }
        else
        {
            // Too short to count as an attempt (e.g. a stray click) — discard silently,
            // and immediately free up the line so the player can try again right away.
            ClearCurrentLine();
        }
    }

    private void AddPoint(Vector2 worldPoint)
    {
        _currentPoints.Add(worldPoint);
        _lastSampledPoint = worldPoint;

        _currentLine.positionCount = _currentPoints.Count;
        _currentLine.SetPosition(_currentPoints.Count - 1, new Vector3(worldPoint.x, worldPoint.y, 0f));
    }

    private Vector2 ScreenToDrawPlane(Vector2 screenPosition)
    {
        Vector3 world = _worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, _distanceFromCamera));
        return new Vector2(world.x, world.y);
    }

    /// <summary>
    /// Destroys the currently displayed line and re-opens the gate for a new stroke.
    /// Called by SpellManager/SpellVisualEffects once success/failure feedback has finished
    /// playing, to reset the board for the next spell.
    /// </summary>
    public void ClearCurrentLine()
    {
        if (_currentLine != null)
        {
            Destroy(_currentLine.gameObject);
            _currentLine = null;
        }

        _isLineActive = false;
    }
}