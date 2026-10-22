using UnityEngine;

namespace Game
{
    public struct ZonePlacement
    {
        private Vector3 _position;
        private int _poolIndex;

        public Vector3 Position => _position;
        public int PoolIndex => _poolIndex;

        public ZonePlacement(Vector3 position, int poolIndex)
        {
            _position = position;
            _poolIndex = poolIndex;
        }
    }
}
