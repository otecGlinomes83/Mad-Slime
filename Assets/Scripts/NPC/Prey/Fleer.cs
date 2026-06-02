using UnityEngine;

namespace NPC.Prey
{
    public class Fleer : MonoBehaviour
    {
        [SerializeField] private Mover _mover;

        public void Tick(Vector3 playerPosition)
        {
            Vector3 offset = playerPosition - transform.position;
            Vector3 direction = offset.normalized;

            _mover.Move(-direction);
        }
    }
}