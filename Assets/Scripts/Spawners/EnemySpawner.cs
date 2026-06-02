using Spawners;
using UnityEngine;

namespace NPC.Enemy
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemySpawner : MonoBehaviour, ISpawner
    {
        [SerializeField] private Enemy _prefab;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 20;

        private BoxCollider _spawnZone;
        private GenericSpawner<Enemy> _spawner;

        public void Setup()
        {
            _spawnZone = GetComponent<BoxCollider>();
            _spawner = new GenericSpawner<Enemy>(_prefab, transform, _defaultCapacity, _maxSize);
        }

        public void StartSpawn()
        {
            Enemy enemy = _spawner.Spawn();
            enemy.transform.position = GetRandomSpawnPosition();
        }

        public void StopSpawn()
        {
            _spawner.ReleaseAll();
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Bounds bounds = _spawnZone.bounds;

            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(x, _spawnZone.transform.position.y, z);
        }
    }
}