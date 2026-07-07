using System.Collections.Generic;
using UnityEngine;

public class SwordSlashEffect : MonoBehaviour
{
    [Header("Slash Settings")]
    [SerializeField] private float _slashAngle = 90f;
    [SerializeField] private float _slashDuration = 0.15f;

    [Header("Collision & Damage Settings")]
    [SerializeField] private string _targetTag = "Enemy"; // Set this to "Enemy" or "Player" depending on who is swinging

    private Quaternion _startRotation;
    private Quaternion _endRotation;
    private float _elapsedTime = 0f;
    
    private int _damage = 0;
    private bool _hasHit = false;

    // Public initialization method to safely inject damage from the BattleSystem
    public void Initialize(int damageAmount)
    {
        _damage = damageAmount;
    }

    private void Start()
    {
        _startRotation = transform.rotation;
        _endRotation = _startRotation * Quaternion.Euler(0, 0, -_slashAngle);
        
        if (_slashDuration <= 0f)
        {
            _slashDuration = 0.01f;
        }
        CheckOverlap();
    }
        private void CheckOverlap()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // Get all colliders already overlapping this trigger
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;

        List<Collider2D> results = new List<Collider2D>();
        Physics2D.OverlapCollider(col, filter, results);

        foreach (var hit in results)
        {
            if (!_hasHit && hit.CompareTag(_targetTag))
            {
                Unit targetUnit = hit.GetComponent<Unit>();
                if (targetUnit != null)
                {
                    _hasHit = true;
                    targetUnit.TakeDamage(_damage);
                }
            }
        }
    }

    private void Update()
    {
        if (_elapsedTime < _slashDuration)
        {
            _elapsedTime += Time.deltaTime;
            
            float progress = _elapsedTime / _slashDuration;
            transform.rotation = Quaternion.Slerp(_startRotation, _endRotation, progress);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Prevent hitting multiple times in one swing, or hitting the wrong target
        if (_hasHit || !collision.CompareTag(_targetTag)) 
            return;

        Unit targetUnit = collision.GetComponent<Unit>();
        if (targetUnit != null)
        {
            _hasHit = true;
            targetUnit.TakeDamage(_damage);
        }
    }
}