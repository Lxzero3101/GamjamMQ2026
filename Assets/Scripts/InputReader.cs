//Minigame: Spell Drawing


using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bridges Unity's New Input System (PlayerControls asset, "Drawing" action map)
/// to plain C# events/properties that the rest of the game consumes.
/// If input systems or platforms ever change, this is the only script that needs rewriting.
/// </summary>
public class InputReader : MonoBehaviour
{
    // ---- Clean events for other scripts to subscribe to ----
    public event Action OnDrawStarted;
    public event Action OnDrawCanceled;

    // ---- Current pointer state ----
    public Vector2 CurrentMouseOrTouchPosition =>
        _playerControls != null ? _playerControls.Drawing.Position.ReadValue<Vector2>() : Vector2.zero;

    public bool IsDrawing { get; private set; }

    // The generated wrapper class from the PlayerControls Input Action Asset
    // (requires "Generate C# Class" ticked in the asset's import settings, class name PlayerControls).
    // Not [SerializeField]: this is a code-generated C# object, not a Unity-serializable reference.
    private PlayerControls _playerControls;

    private void Awake()
    {
        _playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        _playerControls.Drawing.Enable();
        _playerControls.Drawing.Click.performed += HandleClickPerformed;
        _playerControls.Drawing.Click.canceled += HandleClickCanceled;
    }

    private void OnDisable()
    {
        _playerControls.Drawing.Click.performed -= HandleClickPerformed;
        _playerControls.Drawing.Click.canceled -= HandleClickCanceled;
        _playerControls.Drawing.Disable();
    }

    private void OnDestroy()
    {
        // Generated Input Action wrapper classes implement IDisposable —
        // clean up to avoid leaking native input resources.
        _playerControls?.Dispose();
    }

    private void HandleClickPerformed(InputAction.CallbackContext context)
    {
        IsDrawing = true;
        OnDrawStarted?.Invoke();
    }

    private void HandleClickCanceled(InputAction.CallbackContext context)
    {
        IsDrawing = false;
        OnDrawCanceled?.Invoke();
    }
}