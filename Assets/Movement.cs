using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap obstacleTilemap;
    [SerializeField] private Grid grid;               

    private Vector3Int currentGridPosition;
    private bool isMoving = false;

    void Start()
    {
        // Snap the player to the nearest tile center at the start
        currentGridPosition = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(currentGridPosition);
    }

    void Update()
    {
        // Get discrete input (GetKeyDown ensures it only registers once per press)
        Vector3Int direction = Vector3Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))    direction = Vector3Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow))  direction = Vector3Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow))  direction = Vector3Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector3Int.right;

        // If an arrow key was pressed, attempt to move
        if (direction != Vector3Int.zero)
        {
            AttemptMove(direction);
        }
    }

    private void AttemptMove(Vector3Int direction)
    {
        // Calculate the target grid position
        Vector3Int targetGridPosition = currentGridPosition + direction;

        // Check if there is a wall tile at the target position
        if (obstacleTilemap.HasTile(targetGridPosition))
        {
            return; // Blocking the movement
        }

        // If the path is clear, update the position
        currentGridPosition = targetGridPosition;
        transform.position = grid.GetCellCenterWorld(currentGridPosition);
    }
}