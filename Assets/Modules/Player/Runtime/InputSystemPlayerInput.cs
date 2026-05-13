using UnityEngine;

namespace MadSlime.Player
{
    internal sealed class InputSystemPlayerInput : MonoBehaviour, IPlayerInput
    {
        private PlayerControls _controls;

        public Vector2 Direction => _controls.Gameplay.Move.ReadValue<Vector2>();

        private void Awake()
        {
            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            _controls.Enable();
        }

        private void OnDisable()
        {
            _controls.Disable();
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }
    }
}
