using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Hooks into SpellManager's OnSpellSuccess event.
/// Awards 1 point per successful spell match.
/// At 5 points, triggers a win sequence and loads the next scene.
/// </summary>
public class SpellWinCondition : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SpellManager _spellManager;

    [Header("Win Settings")]
    [SerializeField] private int _pointsToWin = 5;
    [SerializeField] private string _nextSceneName; // exact name from Build Settings
    [SerializeField] private float _winDelay = 2f;  // pause before loading next scene

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _pointsText;  // e.g. "Points: 2 / 5"
    [SerializeField] private TextMeshProUGUI _winText;     // shown when player wins

    [Header("Win Message")]
    [SerializeField] private string _winMessage = "✦ You have mastered the spells! ✦";
    [SerializeField] private Color _winMessageColor = new Color(1f, 0.85f, 0.2f);

    private int _currentPoints = 0;
    private bool _hasWon = false;

    private void OnEnable()
    {
        if (_spellManager != null)
            _spellManager.OnSpellSuccess += HandleSpellSuccess;
    }

    private void OnDisable()
    {
        if (_spellManager != null)
            _spellManager.OnSpellSuccess -= HandleSpellSuccess;
    }

    private void Start()
    {
        _currentPoints = 0;
        _hasWon = false;

        UpdatePointsUI();

        if (_winText != null)
            _winText.gameObject.SetActive(false);
    }

    private void HandleSpellSuccess(SpellData spell, float score, LineRenderer line)
    {
        if (_hasWon) return;

        _currentPoints++;
        UpdatePointsUI();

        if (_currentPoints >= _pointsToWin)
        {
            _hasWon = true;
            StartCoroutine(WinSequence());
        }
    }

    private void UpdatePointsUI()
    {
        if (_pointsText != null)
            _pointsText.text = $"Points: {_currentPoints} / {_pointsToWin}";
    }

    private IEnumerator WinSequence()
    {
        // Show win message
        if (_winText != null)
        {
            _winText.text = _winMessage;
            _winText.color = _winMessageColor;
            _winText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(_winDelay);

        // Load next scene
        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            SceneManager.LoadScene(_nextSceneName);
        }
        else
        {
            // Fallback: load next scene by index
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadScene(nextIndex);
            else
                Debug.LogWarning("SpellWinCondition: no next scene found in Build Settings.");
        }
    }
}