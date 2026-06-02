using Collectables;
using UnityEngine;

namespace NPC.Prey
{
    [RequireComponent(typeof(Wander))]
    [RequireComponent(typeof(Fleer))]
    [RequireComponent(typeof(PlayerSensor))]
    public sealed class Prey : MonoBehaviour
    {
        [SerializeField] private Item _item;

        private Fleer _fleer;
        private Wander _wander;
        private PlayerSensor _playerSensor;

        private bool _isCollected;

        private void Awake()
        {
            _fleer = GetComponent<Fleer>();
            _wander = GetComponent<Wander>();
            _playerSensor = GetComponent<PlayerSensor>();
        }

        private void OnEnable()
        {
            _isCollected = false;

            Enable();

            _item.Collected += OnCollect;
            _playerSensor.PlayerEntered += OnPlayerEntered;
        }

        private void OnDisable()
        {
            _item.Collected -= OnCollect;
            _playerSensor.PlayerEntered -= OnPlayerEntered;
        }

        private void Update()
        {
            if (_isCollected == true)
            {
                return;
            }

            if (_playerSensor.IsPlayerInRange)
            {
                _fleer.Tick(_playerSensor.DetectedPlayer.position);
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

        private void OnCollect()
        {
            _isCollected = true;
            Shutdown();
        }

        private void Shutdown()
        {
            _wander.Stop();
            _wander.enabled = false;
            _fleer.enabled = false;
            _playerSensor.enabled = false;
        }

        private void Enable()
        {
            _wander.enabled = true;
            _fleer.enabled = true;
            _playerSensor.enabled = true;
        }
    }
}