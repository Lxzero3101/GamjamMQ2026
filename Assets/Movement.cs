using UnityEngine;

public class GridMovement : MonoBehaviour
{
    public float gridSize = 1f; // Size of your grid in Unity units
    public float moveSpeed = 5f; // Speed of the transition

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void UpdateMove()
    {
        // If the character is already moving, smoothly interpolate to the target position
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Stop moving once we get close enough to the target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
            return;
        }

        // GetDown ensures only one grid space is traversed per key press
        float inputX = 0f;
        float inputY = 0f;

        if (Input.GetKeyDown(KeyCode.UpArrow)) inputY = 1f;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) inputY = -1f;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) inputX = -1f;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) inputX = 1f;

        // If an arrow key was pressed, calculate the new target position
        if (inputX != 0f || inputY != 0f)
        {
            Vector3 movement = new Vector3(inputX * gridSize, inputY * gridSize, 0f);
            targetPosition = transform.position + movement;
            isMoving = true;
        }
    }
}