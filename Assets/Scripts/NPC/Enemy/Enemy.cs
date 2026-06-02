using System;
using System.Threading;
using NPC.Prey;
using UnityEngine;

namespace NPC.Enemy
{
    [RequireComponent(typeof(Wander))]
    [RequireComponent(typeof(Chaser))]
    [RequireComponent(typeof(PlayerSensor))]
    public class Enemy : MonoBehaviour
    {
        private Wander _wander;
        private PlayerSensor _playerSensor;

        private Chaser _chaser;

        private void Awake()
        {
            _wander = GetComponent<Wander>();
            _playerSensor = GetComponent<PlayerSensor>();
            _chaser = GetComponent<Chaser>();
        }

        private void OnEnable()
        {
            _playerSensor.PlayerEntered += OnPlayerEntered;
            _playerSensor.PlayerExited += OnPlayerExited;
        }

        private void OnDisable()
        {
            _playerSensor.PlayerEntered -= OnPlayerEntered;
            _playerSensor.PlayerExited -= OnPlayerExited;
        }

        private void Update()
        {
            if (_playerSensor.IsPlayerInRange)
            {
                _chaser.Tick(_playerSensor.DetectedPlayer.position);
            }
            else
            {
                _wander.Tick();
            }
        }

        private void OnPlayerEntered(Transform player)
        {
            _wander.Stop();
        }

        private void OnPlayerExited()
        {
            // потом добавлю тут некоторое время погони независимо от того, что игрок вышел из зоны видимости
        }
    }
}