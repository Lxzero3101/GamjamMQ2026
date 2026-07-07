using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTile : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name of the scene you want to load.")]
    [SerializeField] private string nextSceneName;

    [Header("Tag Filtering")]
    [Tooltip("The tag assigned to your player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    // This function triggers when another object enters the tile's collider
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that stepped on the tile is the player
        if (other.CompareTag(playerTag))
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Player stepped on the trigger tile. Loading scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next Scene Name is empty! Please assign a scene name in the Inspector.");
        }
    }
}