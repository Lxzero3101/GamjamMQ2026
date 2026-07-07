using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))] // Ensures a collider is present
public class SpriteButtonTrigger : MonoBehaviour
{
    private void OnMouseDown()
    {
        // Check if the global mini-game manager exists, then start it
        if (TriggerMiniGame.Instance != null)
        {
            TriggerMiniGame.Instance.StartMiniGame();
        }
        else
        {
            Debug.LogError("SpriteButtonTrigger: No TriggerMiniGame instance found in the scene!");
        }
    }
}