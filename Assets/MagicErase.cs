using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDestroyer : MonoBehaviour
{
    // Assign the grid's Tilemap
    public Tilemap targetTilemap; 

    void RemoveTile()
    {
        // Detect a left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Allocate mouse screen position
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Change World coordinate to Tilemap cell coordinate
            Vector3Int coordinate = targetTilemap.WorldToCell(mouseWorldPos);

            // Check if a tile exists at the spot
            if (targetTilemap.HasTile(coordinate))
            {
                // Erase the tile
                targetTilemap.SetTile(coordinate, null); 
            }
        }
    }
}