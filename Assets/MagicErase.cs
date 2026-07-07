using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDestroyer : MonoBehaviour
{
    public Tilemap targetTilemap; 

    // Update is called once per frame by Unity automatically

    void Update()
    {
        // Detect a left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Get mouse position
            Vector3 mousePosition = Input.mousePosition;

            mousePosition.z = Mathf.Abs(Camera.main.transform.position.z);

            // Change mouse screen position to World position
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePosition);
            
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