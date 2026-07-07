using UnityEngine;

public enum MusicArc { Arc1, Arc2, Arc3 }

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    [Header("Arc BGM")]
    [SerializeField] private AudioClip arc1Music;
    [SerializeField] private AudioClip arc2Music;
    [SerializeField] private AudioClip arc3Music;

    private MusicArc? currentArc = null;

    private void Awake()
{
    Debug.Log($"[AudioManager] Awake called on {gameObject.name}, InstanceID: {GetInstanceID()}");

    if (Instance != null && Instance != this)
    {
        Debug.Log("[AudioManager] Duplicate found, destroying this one.");
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
    Debug.Log("[AudioManager] DontDestroyOnLoad called, this should persist.");
    // ...rest of your code
}

private void OnDestroy()
{
    Debug.Log($"[AudioManager] OnDestroy called on {gameObject.name}, InstanceID: {GetInstanceID()}");
}

    public void PlayArcMusic(MusicArc arc)
    {
        // Already playing this arc's music -> don't restart it
        if (currentArc.HasValue && currentArc.Value == arc && musicSource.isPlaying)
            return;

        AudioClip clip = arc switch
        {
            MusicArc.Arc1 => arc1Music,
            MusicArc.Arc2 => arc2Music,
            MusicArc.Arc3 => arc3Music,
            _ => null
        };

        if (clip == null) return;

        currentArc = arc;
        musicSource.clip = clip;
        musicSource.Play();
    }
    
}