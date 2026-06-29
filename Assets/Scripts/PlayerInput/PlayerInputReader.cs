using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerInput
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private PlayerInputActions _inputActions;

        public Vector2 MoveInput { get; private set; }
        public event Action MovementKeyPressed;
        public event Action SprintPerformed;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Move.performed += OnMovePerformed;
            _inputActions.Player.Move.canceled += OnMoveCanceled;
            _inputActions.Player.Sprint.performed += OnSprintPerformed;

            _inputActions.Player.Enable();
            MoveInput = Vector2.zero;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMovePerformed;
            _inputActions.Player.Move.canceled -= OnMoveCanceled;
            _inputActions.Player.Sprint.performed -= OnSprintPerformed;

            _inputActions.Player.Disable();

            MoveInput = Vector2.zero;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            bool wasZero = MoveInput == Vector2.zero;
            MoveInput = context.ReadValue<Vector2>();

            if (wasZero == true)
            {
                MovementKeyPressed?.Invoke();
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            MoveInput = Vector2.zero;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            SprintPerformed?.Invoke();
        }
    }
}
