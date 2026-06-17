using Item;
using Detectors;
using Interfaces;
using UnityEngine;

namespace NPC.Prey
{
    [RequireComponent(typeof(Wander))]
    [RequireComponent(typeof(Fleer))]
    [RequireComponent(typeof(TargetSensor))]
    public sealed class Prey : MonoBehaviour
    {
        [SerializeField] private Item.Item _item;

        private Fleer _fleer;
        private Wander _wander;
        private TargetSensor _targetSensor;

        private bool _isCollected;

        private void Awake()
        {
            _fleer = GetComponent<Fleer>();
            _wander = GetComponent<Wander>();
            _targetSensor = GetComponent<TargetSensor>();
        }

        private void OnEnable()
        {
            _isCollected = false;

            Enable();

            _item.Collected += OnCollect;
            _targetSensor.TargetEntered += OnTargetEntered;
        }

        private void OnDisable()
        {
            _item.Collected -= OnCollect;
            _targetSensor.TargetEntered -= OnTargetEntered;
        }

        private void Update()
        {
            if (_isCollected == true)
            {
                return;
            }

            if (_targetSensor.IsTargetInRange)
            {
                _fleer.Tick(_targetSensor.DetectedTarget.Transform.position);
            }
            else
            {
                _wander.Tick();
            }
        }

        private void OnTargetEntered(ITarget target)
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
            _targetSensor.enabled = false;
        }

        private void Enable()
        {
            _wander.enabled = true;
            _fleer.enabled = true;
            _targetSensor.enabled = true;
        }
    }
}
