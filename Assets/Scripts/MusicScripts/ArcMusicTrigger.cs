using UnityEngine;

public class ArcMusicTrigger : MonoBehaviour
{
    [SerializeField] private MusicArc arc;

    private void Start()
    {
        AudioManager.Instance.PlayArcMusic(arc);
    }
}