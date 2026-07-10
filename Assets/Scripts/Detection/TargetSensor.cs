using HealthSystem;
using Interfaces;
using UnityEngine;

namespace Detection
{
    public sealed class TargetSensor : MonoBehaviour
    {
        private const int BufferSize = 4;

        [SerializeField] private float _radius = 5f;
        [SerializeField] private LayerMask _targetLayer;

        private readonly Collider[] _buffer = new Collider[BufferSize];

        public bool TryDetect(out ITarget target)
        {
            target = null;

            int hitsCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _buffer,
                _targetLayer);

            for (int i = 0; i < hitsCount; i++)
            {
                if (_buffer[i].TryGetComponent(out ITarget found) == false)
                {
                    continue;
                }

                if (found.Health.IsAlive == false)
                {
                    continue;
                }

                target = found;
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}