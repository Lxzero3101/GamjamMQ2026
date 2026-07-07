using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point for using the incantation-drawing loop as a mini-game inside a larger game
/// (e.g. a turn-based battle triggering a "cast your spell" sequence).
///
/// Call StartMiniGame() from a button's OnClick, or from any other script (an NPC's turn
/// logic, a battle manager, etc.) to swap to the drawing scene. This component doesn't
/// modify SpellManager at all — it just listens to the OnSpellSuccess/OnSpellFailed events
/// SpellManager already exposes, and decides for itself when the session is "done" based on
/// its own settings below. SpellManager has no idea it's being used as a mini-game.
///
/// Because this fully swaps scenes (SceneManager.LoadScene, not additive) rather than
/// overlaying one on top of the other, there's no risk of two Cameras/Canvases/EventSystems
/// fighting each other. The tradeoff is that this component must survive the scene swap to
/// keep tracking the session, so it's a small DontDestroyOnLoad singleton.
///
/// SETUP REQUIRED: the mini-game scene must be added to
/// File > Build Settings > Scenes In Build, or SceneManager.LoadScene will fail to find it
/// by name at runtime.
/// </summary>
public class TriggerMiniGame : MonoBehaviour
{
    public static TriggerMiniGame Instance { get; private set; }

    [Header("Mini-Game Scene")]
    [Tooltip("Exact name of the Scene asset containing your Managers/Canvas/Camera drawing-game setup.")]
    [SerializeField] private string _miniGameSceneName = "SpellDrawingMiniGame";

    [Header("Session Success Criteria")]
    [Tooltip("How many successful spell casts count as an overall win for this mini-game session.")]
    [SerializeField] private int _successfulCastsRequired = 1;
    [Tooltip("If greater than 0, the session ends in failure once this many attempts have failed, " +
             "without waiting for successes. Set to 0 for unlimited retries (session only ends on success).")]
    [SerializeField] private int _maxFailedAttempts = 0;

    [Header("Result Display")]
    [Tooltip("How long to let the player see their final result (SpellManager's own feedback " +
             "text/line already shows this) before swapping back to the calling scene.")]
    [SerializeField] private float _resultDisplayDuration = 2f;

    // Fired once the mini-game session is fully finished and the calling scene has reloaded.
    public event Action<bool> OnMiniGameFinished;

    private string _originSceneName;
    private SpellManager _activeSpellManager;
    private int _successfulCasts;
    private int _failedAttempts;
    private bool _sessionEnded;
    private Action<bool> _pendingCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Guard against duplicates if this object happens to exist in multiple scenes.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Convenience overload for hooking directly to a UI Button's OnClick (which can't pass
    /// a C# delegate parameter). Use StartMiniGame(callback) instead when triggering from code.
    /// </summary>
    public void StartMiniGame()
    {
        StartMiniGame(null);
    }

    /// <summary>
    /// Begins a mini-game session: swaps to the drawing scene, tracks spell attempts against
    /// the success criteria above, then swaps back and invokes onFinished(success) once done.
    /// </summary>
    public void StartMiniGame(Action<bool> onFinished)
    {
        if (!string.IsNullOrEmpty(_originSceneName))
        {
            Debug.LogWarning("TriggerMiniGame: a session is already in progress, ignoring StartMiniGame().", this);
            return;
        }

        _pendingCallback = onFinished;
        _successfulCasts = 0;
        _failedAttempts = 0;
        _sessionEnded = false;
        _originSceneName = SceneManager.GetActiveScene().name;

        SceneManager.sceneLoaded += HandleMiniGameSceneLoaded;
        SceneManager.LoadScene(_miniGameSceneName, LoadSceneMode.Single);
    }

    private void HandleMiniGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _miniGameSceneName) return;

        SceneManager.sceneLoaded -= HandleMiniGameSceneLoaded;

        _activeSpellManager = FindFirstObjectByType<SpellManager>();
        if (_activeSpellManager == null)
        {
            Debug.LogError($"TriggerMiniGame: no SpellManager found in scene '{_miniGameSceneName}'. " +
                           "Make sure the Managers GameObject with SpellManager exists in that scene.", this);
            ReturnToOriginScene(false);
            return;
        }

        _activeSpellManager.OnSpellSuccess += HandleSpellSuccess;
        _activeSpellManager.OnSpellFailed += HandleSpellFailed;
    }

    private void HandleSpellSuccess(SpellData spell, float score, LineRenderer line)
    {
        _successfulCasts++;

        if (_successfulCasts >= _successfulCastsRequired)
        {
            StartCoroutine(EndSessionAfterDelay(true));
        }
    }

    private void HandleSpellFailed(SpellData spell, float score, LineRenderer line)
    {
        _failedAttempts++;

        if (_maxFailedAttempts > 0 && _failedAttempts >= _maxFailedAttempts)
        {
            StartCoroutine(EndSessionAfterDelay(false));
        }
    }

    private IEnumerator EndSessionAfterDelay(bool success)
    {
        // Guard against both a success and a failure crossing their thresholds in overlapping
        // frames (shouldn't normally happen, but cheap to make this idempotent).
        if (_sessionEnded) yield break;
        _sessionEnded = true;

        // SpellManager's own success/failure clear-delay is already showing feedback on the
        // exact stroke; this additional wait is the "let the player see the final result"
        // window before we tear down the whole scene.
        yield return new WaitForSeconds(_resultDisplayDuration);

        if (_activeSpellManager != null)
        {
            _activeSpellManager.OnSpellSuccess -= HandleSpellSuccess;
            _activeSpellManager.OnSpellFailed -= HandleSpellFailed;
            _activeSpellManager = null;
        }

        ReturnToOriginScene(success);
    }

    private void ReturnToOriginScene(bool success)
    {
        string origin = _originSceneName;
        _originSceneName = null;

        if (!string.IsNullOrEmpty(origin))
        {
            SceneManager.LoadScene(origin, LoadSceneMode.Single);
        }

        OnMiniGameFinished?.Invoke(success);
        _pendingCallback?.Invoke(success);
        _pendingCallback = null;
    }
}