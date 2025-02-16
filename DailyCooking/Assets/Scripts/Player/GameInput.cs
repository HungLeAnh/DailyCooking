using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : SimpleSingleton<GameInput>
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAction2;
    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebind;
    public enum Binding
    {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        Interact2,
        Pause,
        Gamepad_Interact,
        Gamepad_Interact2,
        Gamepad_Pause,
    }

    private PlayerAction _actions;

    private void Awake()
    {
        _actions = new PlayerAction();
        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            _actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        _actions.Player.Enable();
        
        _actions.Player.Interact.performed += Interact_performed;
        _actions.Player.Interact2.performed += Interact2_performed;
        _actions.Player.Pause.performed += Pause_performed;
    }
    private void OnDestroy()
    {
        _actions.Player.Interact.performed -= Interact_performed;
        _actions.Player.Interact2.performed -= Interact2_performed;
        _actions.Player.Pause.performed -= Pause_performed;

        _actions.Dispose();
    }
    private void Pause_performed(InputAction.CallbackContext context)
    {
        OnPauseAction?.Invoke(this,EventArgs.Empty);
    }

    private void Interact2_performed(InputAction.CallbackContext context)
    {
        OnInteractAction2?.Invoke(this,EventArgs.Empty);
    }

    private void Interact_performed(InputAction.CallbackContext context)
    {
        OnInteractAction?.Invoke(this,EventArgs.Empty);
    }
    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = _actions.Player.Move.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        return inputVector;
    }

    public string GetBindingText(Binding binding)
    {
        switch (binding)
        {
            default:
            case Binding.Move_Up:
                return _actions.Player.Move.bindings[1].ToDisplayString();

            case Binding.Move_Down:
                return _actions.Player.Move.bindings[2].ToDisplayString();

            case Binding.Move_Left:
                return _actions.Player.Move.bindings[3].ToDisplayString();

            case Binding.Move_Right:
                return _actions.Player.Move.bindings[4].ToDisplayString();

            case Binding.Interact:
                return _actions.Player.Interact.bindings[0].ToDisplayString();

            case Binding.Interact2:
                return _actions.Player.Interact2.bindings[0].ToDisplayString();

            case Binding.Pause:
                return _actions.Player.Pause.bindings[0].ToDisplayString();


            case Binding.Gamepad_Interact:
                return _actions.Player.Interact.bindings[1].ToDisplayString();

            case Binding.Gamepad_Interact2:
                return _actions.Player.Interact2.bindings[1].ToDisplayString();

            case Binding.Gamepad_Pause:
                return _actions.Player.Pause.bindings[1].ToDisplayString();

        }
    }
    public void RebindBinding(Binding binding, Action onActionRebound)
    {
        _actions.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding)
        {
            default:
            case Binding.Move_Up:
                inputAction = _actions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = _actions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = _actions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = _actions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = _actions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.Interact2:
                inputAction = _actions.Player.Interact2;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = _actions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = _actions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Interact2:
                inputAction = _actions.Player.Interact2;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = _actions.Player.Pause;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {
                callback.Dispose();
                _actions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, _actions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }
}
