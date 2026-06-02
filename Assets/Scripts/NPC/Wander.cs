using Game;
using UnityEngine;

namespace NPC.Prey
{
    [RequireComponent(typeof(Timer))]
    public sealed class Wander : MonoBehaviour
    {
        [SerializeField] private Mover _mover;
        [SerializeField] private Vector2 _zoneSize = new Vector2(10f, 10f);
        [SerializeField] private float _idleDuration = 2f;

        private const float Threshold = 0.5f;

        private Vector3 _zoneCenter;
        private Vector3 _targetPosition;
        private Timer _timer;
        private bool _isWaiting;

        private void Awake()
        {
            _timer = GetComponent<Timer>();
        }

        private void OnEnable()
        {
            _zoneCenter = transform.position;
            _targetPosition = GetRandomTarget();
            _isWaiting = false;

            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnTimerFinished;
        }

        public void Stop()
        {
            _isWaiting = false;
            _timer.Stop();
        }

        public void Tick()
        {
            if (_isWaiting == true)
            {
                return;
            }

            Vector3 offset = _targetPosition - transform.position;
            offset.y = 0f;

            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance < Threshold * Threshold)
            {
                StartWaiting();
                return;
            }

            Vector3 direction = offset.normalized;
            _mover.Move(direction);
        }

        private void OnTimerFinished()
        {
            _isWaiting = false;
            _targetPosition = GetRandomTarget();
        }

        private void StartWaiting()
        {
            _isWaiting = true;
            _timer.Setup(_idleDuration);
            _timer.StartCount();
        }

        private Vector3 GetRandomTarget()
        {
            float halfX = _zoneSize.x * 0.5f;
            float halfZ = _zoneSize.y * 0.5f;

            float x = _zoneCenter.x + Random.Range(-halfX, halfX);
            float z = _zoneCenter.z + Random.Range(-halfZ, halfZ);

            return new Vector3(x, _zoneCenter.y, z);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center;

            if (Application.isPlaying == true)
            {
                center = _zoneCenter;
            }
            else
            {
                center = transform.position;
            }

            Vector3 size = new Vector3(_zoneSize.x, 0.1f, _zoneSize.y);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);
        }
    }
}