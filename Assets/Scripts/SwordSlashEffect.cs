using UnityEngine;

public class SwordSlashEffect : MonoBehaviour
{
    [Header("Slash Settings")]
    [SerializeField] private float _slashAngle = 90f;
    [SerializeField] private float _slashDuration = 0.15f;

    private Quaternion _startRotation;
    private Quaternion _endRotation;
    private float _elapsedTime = 0f;

    private void Start()
    {
        // Cache the starting rotation (set by your spawning script)
        _startRotation = transform.rotation;
        
        // Calculate the target rotation (rotating 90 degrees on the Z-axis for 2D)
        _endRotation = _startRotation * Quaternion.Euler(0, 0, -_slashAngle);
        
        // Safety check to avoid division by zero if duration is set to 0
        if (_slashDuration <= 0f)
        {
            _slashDuration = 0.01f;
        }
    }

    private void Update()
    {
        if (_elapsedTime < _slashDuration)
        {
            _elapsedTime += Time.deltaTime;
            
            // Smoothly interpolate the rotation over the duration
            float progress = _elapsedTime / _slashDuration;
            transform.rotation = Quaternion.Slerp(_startRotation, _endRotation, progress);
        }
        else
        {
            // Slash is complete, self-destruct
            Destroy(gameObject);
        }
    }
}