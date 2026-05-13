using System;
using MadSlime.Movement;
using UnityEngine;

namespace MadSlime.Player
{
    public sealed class Slime : MonoBehaviour
    {
        private IMover _mover;
        private IPlayerInput _playerInput;
        
        private bool _isSetupFinished;

        private void Update()
        {
            if (_isSetupFinished==false) 
                return;

            _mover.Move(_playerInput.Direction);
        }

        public void Setup(IMover mover, IPlayerInput playerInput)
        {
            _mover = mover;
            _playerInput = playerInput;
            _isSetupFinished = true;
        }
    }
}