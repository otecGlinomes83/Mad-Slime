using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Chaser : MonoBehaviour
{
    [SerializeField] private float _sampleDistance = 2f;

     private NavMeshAgent _agent;
     
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }
    
    public void Tick(Vector3 playerPosition)
    {
        if (NavMesh.SamplePosition(playerPosition, out NavMeshHit hit, _sampleDistance, NavMesh.AllAreas) == true)
        {
            _agent.SetDestination(hit.position);
        }
    }
}