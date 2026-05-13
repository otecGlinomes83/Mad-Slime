using System;
using MadSlime.Movement;
using UnityEngine;

namespace MadSlime.Player
{
    public sealed class Slime : MonoBehaviour
    {
        private IMover _mover;
        private IPlayerInput _input;

        private void Awake()
        {
            _mover = GetComponent<IMover>();

            if (_mover == null)
            {
                throw new InvalidOperationException($"{nameof(Slime)} requires a component implementing {nameof(IMover)} on the same GameObject.");
            }

            _input = GetComponent<IPlayerInput>();

            if (_input == null)
            {
                throw new InvalidOperationException($"{nameof(Slime)} requires a component implementing {nameof(IPlayerInput)} on the same GameObject.");
            }
        }

        private void Update()
        {
            _mover.Move(_input.Direction);
        }

        private void OnValidate()
        {
            if (GetComponent<IMover>() == null)
            {
                Debug.LogError($"{nameof(Slime)} requires a component implementing {nameof(IMover)} on the same GameObject.", this);
            }

            if (GetComponent<IPlayerInput>() == null)
            {
                Debug.LogError($"{nameof(Slime)} requires a component implementing {nameof(IPlayerInput)} on the same GameObject.", this);
            }
        }
    }
}