using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Follow Settings")]
    [SerializeField] private float _smoothTime = 0.15f;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

    [Header("Look Ahead (optional)")]
    [SerializeField] private bool _useLookAhead = false;
    [SerializeField] private float _lookAheadDistance = 2f;
    [SerializeField] private float _lookAheadSmoothTime = 0.3f;

    [Header("Bounds (optional)")]
    [SerializeField] private bool _useBounds = false;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _currentLookAhead = Vector3.zero;
    private Vector3 _lookAheadVelocity = Vector3.zero;
    private Rigidbody2D _targetRb;

    private void Awake()
    {
        // Ensure the camera is not a child of the character so its transform
        // (position/rotation/scale flips) doesn't affect the camera directly.
        transform.SetParent(null);

        if (_target != null)
        {
            _targetRb = _target.GetComponent<Rigidbody2D>();
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desiredPosition = _target.position + _offset;

        if (_useLookAhead)
        {
            float horizontalVelocity = _targetRb != null ? _targetRb.linearVelocity.x : 0f;
            Vector3 targetLookAhead = new Vector3(Mathf.Sign(horizontalVelocity) * _lookAheadDistance, 0f, 0f);

            if (Mathf.Approximately(horizontalVelocity, 0f))
            {
                targetLookAhead = Vector3.zero;
            }

            _currentLookAhead = Vector3.SmoothDamp(_currentLookAhead, targetLookAhead, ref _lookAheadVelocity, _lookAheadSmoothTime);
            desiredPosition += _currentLookAhead;
        }

        if (_useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, _minBounds.y, _maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _smoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_useBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((_minBounds.x + _maxBounds.x) / 2f, (_minBounds.y + _maxBounds.y) / 2f, 0f);
        Vector3 size = new Vector3(_maxBounds.x - _minBounds.x, _maxBounds.y - _minBounds.y, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}