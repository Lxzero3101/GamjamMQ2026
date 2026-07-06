using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            System.Func<KeyControl, bool> inputMovement;
            if (repeatMove) {
                inputMovement = key => key.isPressed;
            } else {
                inputMovement = key => key.wasPressedThisFrame;
            }

            // Move in the direction of the arrow key pressed
            if (inputMovement(keyboard.UpArrowKey)) {
                StartCoroutine(Move(Vector2.up));
            } else if (inputMovement(keyboard.DownArrowKey)) {
                StartCoroutine(Move(Vector2.down));
            } else if (inputMovement(keyboard.LeftArrowKey)) {
                StartCoroutine(Move(Vector2.left));
            } else if (inputMovement(keyboard.RightArrowKey)) {
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