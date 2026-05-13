using System;
using MadSlime.Movement;
using MadSlime.Player;
using UnityEngine;

namespace MadSlime.Game
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Slime _slimePrefab;

        private void Awake()
        {
            Slime slime = Instantiate(_slimePrefab);
            IMover mover = GetComponent<IMover>();
            IPlayerInput playerInput = GetComponent<IPlayerInput>();
            slime.Setup(mover, playerInput);
        }
    }
}