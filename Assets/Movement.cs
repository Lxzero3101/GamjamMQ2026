using UnityEngine;

public class GridPlayer : MonoBehaviour
{
    [Header("Grid Settings")]
    public float tileSize = 1f;   // World units per tile

    void Update()
    {
        if (TurnManager.Instance.IsGameOver) return;

        Vector3 move = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            move = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            move = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            move = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            move = Vector3.right;

        if (move != Vector3.zero)
        {
            transform.position += move * tileSize;
            TurnManager.Instance.UseTurn();
        }
    }
}