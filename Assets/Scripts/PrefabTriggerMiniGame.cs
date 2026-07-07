using UnityEngine;

/// <summary>
/// Automatically triggers the TriggerMiniGame session as soon as this prefab 
/// enters the scene at runtime.
/// </summary>
public class PrefabTriggerMiniGame : MonoBehaviour
{
    [Header("Destruction Settings")]
    [Tooltip("If true, this GameObject will destroy itself immediately after triggering the game to clean up the scene.")]
    [SerializeField] private bool _destroyOnTrigger = true;

    private void Start()
    {
        TriggerSession();
    }

    private void TriggerSession()
    {
        // Check if the global singleton instance exists
        if (TriggerMiniGame.Instance != null)
        {
            TriggerMiniGame.Instance.StartMiniGame();
            
            // Clean up this object so it doesn't leave clutter behind 
            // when returning to the original scene
            if (_destroyOnTrigger)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogError($"[PrefabTriggerMiniGame] Failed to start. No instance of 'TriggerMiniGame' found in the scene. " +
                           $"Make sure your manager object exists before instantiating this prefab.", this);
        }
    }
}