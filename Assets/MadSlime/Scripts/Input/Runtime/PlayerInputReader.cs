using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerInput
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerInputActions _inputActions;

        private Vector2 _moveInput;

        public Vector2 MoveInput => _moveInput;
        public event Action MovementKeyPressed;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Move.performed += OnMovePerformed;
            _inputActions.Player.Move.canceled += OnMoveCanceled;

            _inputActions.Player.Enable();
            _moveInput = Vector2.zero;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMovePerformed;
            _inputActions.Player.Move.canceled -= OnMoveCanceled;

            _inputActions.Player.Disable();

            _moveInput = Vector2.zero;
        }

        private void OnDestroy()
        {
            _inputActions.Dispose();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            bool wasZero = MoveInput == Vector2.zero;
            _moveInput = context.ReadValue<Vector2>();

            if (wasZero == true)
            {
                MovementKeyPressed?.Invoke();
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }
    }
}
