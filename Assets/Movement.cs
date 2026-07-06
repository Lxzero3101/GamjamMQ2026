using System.Collections;
using UnityEngine;

public class GridMovement : MonoBehaviour {
    // Hold key for movement
    [SerializeField] private bool repeatMove = false;
    // Time to move from one grid to another
    [SerializeField] private float moveTime = 0.1f;
    // Size of the grid
    [SerializeField] private float gridSize = 1f;
    private bool isMoving = false;

    private void Update() {
        // Allow movement to be one move per time
        if (!isMoving) {
            System.Func<KeyCode, bool> inputMovement;
            if (repeatMove) {
                inputMovement = Input.GetKey;
            } else {
                inputMovement = Input.GetKeyDown;
            }

            // Move in the direction of the arrow key pressed
            if (inputMovement(KeyCode.UpArrow)) {
                StartCoroutine(Move(Vector2.up));
            } else if (inputMovement(KeyCode.DownArrow)) {
                StartCoroutine(Move(Vector2.down));
            } else if (inputMovement(KeyCode.LeftArrow)) {
                StartCoroutine(Move(Vector2.left));
            } else if (inputMovement(KeyCode.RightArrow)) {
                StartCoroutine(Move(Vector2.right));
            }
        }
    }

    // Smooth movement between grids
    private IEnumerator Move(Vector2 direction) {
        isMoving = true;

        // Calculate the current position
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + (direction * gridSize);
        float elapsedTime = 0;
        // Move the object over time to the target position
        while (elapsedTime < moveTime) {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / moveTime;
            transform.position = Vector2.Lerp(startPosition, endPosition, percent);
            yield return null;
        }

        // Ensure the final position is set to the target position
        transform.position = endPosition;

        // Allow to move again after one input
        isMoving = false;
    }
}