using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private int _maxJumps = 1; // 1 = single jump, 2 = double jump, etc.
    [SerializeField] private float _groundCheckDistance = 0.15f;
    [SerializeField] private float _groundCheckExtraRadius = 0.05f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Physics")]
    [SerializeField] private float _gravityScale = 3f;
    [SerializeField] private float _fallMultiplier = 2.5f;
    [SerializeField] private float _lowJumpMultiplier = 2f;

    // Private references
    private Rigidbody2D _rb;
    private CapsuleCollider2D _capsuleCollider;

    // Private state
    private float _horizontalInput;
    private bool _isGrounded;
    private bool _jumpRequested;
    private bool _isFacingRight = true;
    private bool _isRunning;
    private bool _jumpHeld;
    private int _jumpsRemaining;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _capsuleCollider = GetComponent<CapsuleCollider2D>();
        _rb.gravityScale = _gravityScale;
        _rb.freezeRotation = true;
        _jumpsRemaining = _maxJumps;
    }

    private void Update()
    {
        CheckGrounded(); // check ground state BEFORE reading jump input
        GatherInput();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyJump();
        ApplyBetterGravity();
    }

    private void GatherInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Horizontal input (A/D or Left/Right arrows)
        _horizontalInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) _horizontalInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) _horizontalInput = 1f;

        // Run
        _isRunning = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        // Jump (W or Space)
        _jumpHeld = keyboard.wKey.isPressed || keyboard.spaceKey.isPressed;

        bool jumpPressedThisFrame = keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        if (jumpPressedThisFrame && _jumpsRemaining > 0)
        {
            _jumpRequested = true;
        }
    }

    private void CheckGrounded()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * (_capsuleCollider.size.y / 2f);
        float radius = (_capsuleCollider.size.x / 2f) + _groundCheckExtraRadius;
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, Vector2.down, _groundCheckDistance, _groundLayer);
        bool wasGrounded = _isGrounded;
        _isGrounded = hit.collider != null;

        // Refill jumps when we land
        if (_isGrounded && !wasGrounded)
        {
            _jumpsRemaining = _maxJumps;
        }
        else if (_isGrounded && _rb.linearVelocity.y <= 0f)
        {
            // stay topped up while standing still on ground
            _jumpsRemaining = _maxJumps;
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = _isRunning ? _runSpeed : _walkSpeed;
        _rb.linearVelocity = new Vector2(_horizontalInput * targetSpeed, _rb.linearVelocity.y);
    }

    private void ApplyJump()
    {
        if (_jumpRequested)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _jumpsRemaining--;
            _jumpRequested = false;
        }
    }

    private void ApplyBetterGravity()
    {
        if (_rb.linearVelocity.y < 0)
        {
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (_rb.linearVelocity.y > 0 && !_jumpHeld)
        {
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void FlipSprite()
    {
        if (_horizontalInput > 0 && !_isFacingRight) Flip();
        else if (_horizontalInput < 0 && _isFacingRight) Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (_capsuleCollider == null) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector2 origin = (Vector2)transform.position + Vector2.down * (_capsuleCollider.size.y / 2f);
        float radius = (_capsuleCollider.size.x / 2f) + _groundCheckExtraRadius;
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawLine(origin, origin + Vector2.down * _groundCheckDistance);
    }
}