using System;
using System.Threading;
using Item;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spawners
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ItemSpawner : MonoBehaviour, ISpawner
    {
        [SerializeField] private Item.Item _prefab;
        [SerializeField] private int _defaultCapacity = 16;
        [SerializeField] private int _maxSize = 64;
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private int _maxSpawns = 32;

        private BoxCollider _spawnZone;
        private GenericSpawner<Item.Item> _spawner;
        private CancellationTokenSource _loopCancellationTokenSource;
        private bool _isSetupFinished;
        private bool _isRunning;

        private void OnDisable()
        {
            StopSpawn();

            if (_spawner != null)
            {
                _spawner.ReleaseAll();
                _spawner.Spawned -= OnSpawned;
                _spawner.Released -= OnReleased;
            }
        }


        public void Setup()
        {
            _spawnZone = GetComponent<BoxCollider>();
            _spawnZone.isTrigger = true;

            _spawner = new GenericSpawner<Item.Item>(_prefab, transform, _defaultCapacity, _maxSize);
            _spawner.Spawned += OnSpawned;
            _spawner.Released += OnReleased;

            _isSetupFinished = true;
        }

        public void StartSpawn()
        {
            if (_isSetupFinished == false)
            {
                throw new InvalidOperationException("ItemSpawner.StartSpawn called before Setup.");
            }

            if (_isRunning == true)
            {
                return;
            }

            CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();
            _loopCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);

            _isRunning = true;
            SpawnLoopAsync(_loopCancellationTokenSource.Token).Forget();
        }

        public void StopSpawn()
        {
            if (_isRunning == false)
            {
                return;
            }

            _loopCancellationTokenSource?.Cancel();
            _loopCancellationTokenSource?.Dispose();
            _loopCancellationTokenSource = null;
            _isRunning = false;
        }

        private void OnSpawned(Item.Item instance)
        {
            instance.Initialize(GetRandomSpawnPosition());
            instance.ReadyToRelease += OnInstanceReadyToRelease;
        }

        private void OnReleased(Item.Item instance)
        {
            instance.ReadyToRelease -= OnInstanceReadyToRelease;
        }

        private async UniTaskVoid SpawnLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval), cancellationToken: cancellationToken);

                    if (_spawner.ActiveCount >= _maxSpawns)
                    {
                        continue;
                    }

                    _spawner.Spawn();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void OnInstanceReadyToRelease(Item.Item instance)
        {
            _spawner.Release(instance);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Bounds bounds = _spawnZone.bounds;

            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float z = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(x, _spawnZone.transform.position.y, z);
        }
    }
}