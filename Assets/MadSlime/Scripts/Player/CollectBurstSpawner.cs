using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public sealed class CollectBurstSpawner : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _burstPrefab;

        [SerializeField, Min(1)] private int _poolSize = 4;

        private readonly Queue<ParticleSystem> _pool = new Queue<ParticleSystem>();

        private void Awake()
        {
            if (_burstPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Burst prefab is not assigned. Drag a ParticleSystem prefab into the _burstPrefab field.");
            }

            if (_poolSize < 1)
            {
                throw new InvalidOperationException(
                    $"{name}: PoolSize must be at least 1.");
            }

            for (int i = 0; i < _poolSize; i++)
            {
                ParticleSystem burst = Instantiate(_burstPrefab, transform);
                _pool.Enqueue(burst);
            }
        }

        public void Play(Vector3 position, float scale)
        {
            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale),
                    scale,
                    "CollectBurstSpawner.Play requires a positive scale.");
            }

            ParticleSystem burst = _pool.Dequeue();
            _pool.Enqueue(burst);

            burst.transform.position = position;

            ParticleSystem.MainModule main = burst.main;
            main.startSizeMultiplier = scale;

            burst.Play();
        }
    }
}
