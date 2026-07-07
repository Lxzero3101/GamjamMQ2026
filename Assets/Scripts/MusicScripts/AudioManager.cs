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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        musicSource.loop = true;
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