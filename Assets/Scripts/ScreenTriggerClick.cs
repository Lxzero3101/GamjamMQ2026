using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTriggerClick : MonoBehaviour
{
    [Header("Scene to load (must be in Build Settings)")]
    public string sceneName;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}