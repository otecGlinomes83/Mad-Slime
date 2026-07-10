using Game;
using UnityEngine;
using UnityEngine.AI;

namespace NPC.Enemy
{
    [RequireComponent(typeof(Timer))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Wander : MonoBehaviour
    {
        [SerializeField] private float _idleDuration = 2f;

        [SerializeField] private float _randomPointRadius = 5f;
        [SerializeField] private float _maxSampleDistance = 3f;
        [SerializeField] private int _maxAttempts = 10;

        private NavMeshPath _path;

        private NavMeshAgent _agent;
        private Timer _timer;

        private bool _isWaiting;

        private void Awake()
        {
            _timer = GetComponent<Timer>();
            _agent = GetComponent<NavMeshAgent>();
            _path = new NavMeshPath();  
        }

        private void OnEnable()
        {
            SetNewDestination();
            _isWaiting = false;

            _timer.Finished += OnTimerFinished;
        }

        private void OnDisable()
        {
            _timer.Finished -= OnTimerFinished;
        }

        public void Stop()
        {
            _agent.ResetPath();
            _isWaiting = false;
            _timer.Stop();
        }

        public void Tick()
        {
            if (_isWaiting == true)
            {
                return;
            }

            if (_agent.pathPending)
            {
                return;
            }

            if (_agent.remainingDistance > _agent.stoppingDistance)
            {
                return;
            }

            StartWaiting();
        }

        private void OnTimerFinished()
        {
            _isWaiting = false;
            SetNewDestination();
        }

        private void StartWaiting()
        {
            _isWaiting = true;
            _timer.Setup(_idleDuration);
            _timer.StartCount();
        }

        private void SetNewDestination()
        {
            Vector3 newDestination = GetRandomNavMeshTarget();

            _agent.SetDestination(newDestination);
        }

        private Vector3 GetRandomNavMeshTarget()
        {
            for (int i = 0; i < _maxAttempts; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle;
                Vector3 candidate = transform.position +
                                    new Vector3(randomPoint.x, 0, randomPoint.y).normalized * _randomPointRadius;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _maxSampleDistance, NavMesh.AllAreas))
                {
                    if (NavMesh.CalculatePath(candidate, hit.position, NavMesh.AllAreas, _path))
                    {
                        if (_path.status == NavMeshPathStatus.PathComplete)
                        {
                            return hit.position;
                        }
                    }
                }
            }

            return transform.position;
        }
    }
}