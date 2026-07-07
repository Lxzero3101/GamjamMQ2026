using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap[] obstacleTilemaps; // Changed to an Array for multiple tilemaps
    [SerializeField] private Grid grid;                 

    private Vector3Int currentGridPosition;

    void Start()
    {
        // Snap the player to the nearest tile center at the start
        currentGridPosition = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(currentGridPosition);
    }

    void Update()
    {
        Vector3Int direction = Vector3Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))    direction = Vector3Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow))  direction = Vector3Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow))  direction = Vector3Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector3Int.right;

        if (direction != Vector3Int.zero)
        {
            AttemptMove(direction);
        }
    }

    private void AttemptMove(Vector3Int direction)
    {
        Vector3Int targetGridPosition = currentGridPosition + direction;

        // Loop through every tilemap in our list
        foreach (Tilemap wallTilemap in obstacleTilemaps)
        {
            // If this tilemap isn't empty, check if it has a tile at the target position
            if (wallTilemap != null && wallTilemap.HasTile(targetGridPosition))
            {
                Debug.Log($"Movement blocked by a wall on {wallTilemap.name}!");
                return; // A wall was found, stop the movement completely
            }
        }

        // If none of the tilemaps had a wall, move the player
        currentGridPosition = targetGridPosition;
        transform.position = grid.GetCellCenterWorld(currentGridPosition);
    }
}