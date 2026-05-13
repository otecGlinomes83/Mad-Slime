using System;
using UnityEngine;

namespace MadSlime.Movement
{
    [RequireComponent(typeof(CharacterController))]
    internal sealed class CharacterControllerMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _speed;

        private CharacterController _characterController;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_characterController == null)
            {
                throw new InvalidOperationException($"{nameof(CharacterControllerMover)} requires a {nameof(CharacterController)} on the same GameObject.");
            }
        }

        public void Move(Vector2 direction)
        {
            Vector3 motion = new Vector3(direction.x, 0f, direction.y) * _speed;
            _characterController.SimpleMove(motion);
        }
    }
}