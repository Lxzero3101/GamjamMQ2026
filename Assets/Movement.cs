using UnityEngine;

public class GridMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float gridSize = 1f;       // Distance of one tile (usually 1f for Unity's Grid system)
    public float moveSpeed = 5f;      // How fast the character slides to the next tile
    
    [Header("Collision Settings")]
    public LayerMask obstacleLayer;   // Set this to your "Obstacles" or "Walls" layer in the Inspector
    public Transform obstacleCheckPoint; // A child GameObject or empty transform used to check ahead

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
        
        // If no separate checkpoint is assigned, use the player's own position
        if (obstacleCheckPoint == null)
        {
            obstacleCheckPoint = this.transform;
        }
    }

    void Update()
    {
        // 1. Handle the sliding movement if already in motion
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
            return;
        }

        // 2. Read arrow key inputs (one press per movement)
        float inputX = 0f;
        float inputY = 0f;

        if (Input.GetKeyDown(KeyCode.UpArrow)) inputY = 1f;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) inputY = -1f;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) inputX = -1f;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) inputX = 1f;

        // 3. Calculate next tile position and check for collisions
        if (inputX != 0f || inputY != 0f)
        {
            Vector3 direction = new Vector3(inputX * gridSize, inputY * gridSize, 0f);
            Vector3 potentialTarget = transform.position + direction;

            if (CanMove(potentialTarget))
            {
                targetPosition = potentialTarget;
                isMoving = true;
            }
        }
    }

    // Helper method to check if the next tile is blocked
    bool CanMove(Vector3 targetPos)
    {
        // Checks a tiny circle (0.2 units radius) at the destination tile
        // Returns true if NO colliders on the obstacleLayer are found
        return !Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer);
    }
}
