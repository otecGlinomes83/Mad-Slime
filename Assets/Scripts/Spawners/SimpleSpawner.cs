using System;
using System.Threading;
using Collectables;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spawners
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SimpleSpawner : Spawner<Item>
    {
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private int _maxSpawns = 32;

        private BoxCollider _spawnZone;

        protected override void Awake()
        {
            base.Awake();

            _spawnZone = GetComponent<BoxCollider>();
            _spawnZone.isTrigger = true;
        }

        private void Start()
        {
            SpawnLoopAsync().Forget();
        }

        protected override void OnSpawned(Item instance)
        {
            instance.Initialize(GetRandomSpawnPosition());
            instance.ReadyToRelease += OnInstanceReadyToRelease;
        }

        protected override void OnReleased(Item instance)
        {
            instance.ReadyToRelease -= OnInstanceReadyToRelease;
        }

        private async UniTaskVoid SpawnLoopAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                while (enabled)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval), cancellationToken: cancellationToken);

                    if (ActiveCount >= _maxSpawns)
                    {
                        continue;
                    }

                    Spawn();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void OnInstanceReadyToRelease(Item instance)
        {
            Release(instance);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Bounds bounds = _spawnZone.bounds;

            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float z = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
            float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            return new Vector3(x, y, z);
        }
    }
}