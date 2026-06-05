using System;
using Interfaces;
using UnityEngine;

namespace Detectors
{
    public sealed class TargetSensor : MonoBehaviour
    {
        private const int BufferSize = 4;

        [SerializeField] private float _radius = 5f;
        [SerializeField] private LayerMask _targetLayer;

        private readonly Collider[] _buffer = new Collider[BufferSize];

        private ITarget _detectedTarget;
        private bool _isTargetInRange = false;

        public event Action<ITarget> TargetEntered;
        public event Action TargetExited;

        public bool IsTargetInRange => _isTargetInRange;
        public ITarget DetectedTarget => _detectedTarget;

        private void Update()
        {
            int hitsCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _buffer,
                _targetLayer);

            ITarget target = null;

            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].gameObject.TryGetComponent(out ITarget found) == true)
                {
                    target = found;
                    break;
                }
            }

            if (target != null)
            {
                if (_isTargetInRange == false)
                {
                    _isTargetInRange = true;
                    _detectedTarget = target;

                    TargetEntered?.Invoke(target);
                }
                else
                {
                    _detectedTarget = target;
                }

                return;
            }

            if (_isTargetInRange)
            {
                _isTargetInRange = false;
                _detectedTarget = null;

                TargetExited?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
