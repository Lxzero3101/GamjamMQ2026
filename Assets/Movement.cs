using System.Collections;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float tileSize = 1f;

    private bool isMoving = false;
    private Vector3 targetPosition;

    void Update()
    {
        // Only accept new input
        if (!isMoving)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Prevent extra movement
            if (horizontal != 0)
            {
                vertical = 0;
            }

            // Calculate the next tile destination if key is pressed
            if (horizontal != 0 || vertical != 0)
            {
                targetPosition = transform.position + new Vector3(horizontal, vertical, 0) * tileSize;
                StartCoroutine(MoveToGrid(targetPosition));
            }
        }
    }

    private IEnumerator MoveToGrid(Vector3 target)
    {
        isMoving = true;

        // Smooth movement
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        transform.position = target;
        isMoving = false;
    }
}