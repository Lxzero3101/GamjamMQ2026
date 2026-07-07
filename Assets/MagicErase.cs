using UnityEngine;

public class DestructibleTile : MonoBehaviour
{
    void OnMouseDown()
    {
        if (TurnManager.Instance.IsGameOver) return;

        TurnManager.Instance.UseTurn();
        Destroy(gameObject);
    }
}