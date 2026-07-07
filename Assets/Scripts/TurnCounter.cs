using UnityEngine;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Turn Settings")]
    public int maxTurns = 25;
    private int currentTurns;

    [Header("UI")]
    public TextMeshProUGUI turnCounterText;

    public bool IsGameOver => currentTurns <= 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentTurns = maxTurns;
        UpdateUI();
    }

    /// <summary>Call this whenever a turn is consumed (move or tile destroy).</summary>
    public void UseTurn()
    {
        if (IsGameOver) return;

        currentTurns--;
        UpdateUI();

        if (IsGameOver)
            OnGameOver();
    }

    void UpdateUI()
    {
        if (turnCounterText != null)
            turnCounterText.text = $"Turns Left: {currentTurns}";
    }

    void OnGameOver()
    {
        Debug.Log("No turns left! Game Over.");
        // Add your game-over logic here (e.g., load scene, show panel)
    }
}