using Detectors;
using Interfaces;
using NPC.Enemy;
using UnityEngine;

namespace NPC.Enemy
{
    [RequireComponent(typeof(Wander))]
    [RequireComponent(typeof(Chaser))]
    [RequireComponent(typeof(TargetSensor))]
    public class Enemy : MonoBehaviour
    {
        private Wander _wander;
        private TargetSensor _targetSensor;
        private Chaser _chaser;

        private void Awake()
        {
            _wander = GetComponent<Wander>();
            _targetSensor = GetComponent<TargetSensor>();
            _chaser = GetComponent<Chaser>();
        }

        private void OnEnable()
        {
            _targetSensor.TargetEntered += OnTargetEntered;
        }

        private void OnDisable()
        {
            _targetSensor.TargetEntered -= OnTargetEntered;
        }

        private void Update()
        {
            if (_targetSensor.IsTargetInRange)
            {
                _chaser.Tick(_targetSensor.DetectedTarget.Transform.position);
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
    }
}