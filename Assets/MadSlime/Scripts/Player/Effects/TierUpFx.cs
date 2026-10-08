using Cysharp.Threading.Tasks;
using Scriptables;
using Skills;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace Player
{
    public sealed class TierUpFx : MonoBehaviour
    {
        private const float LifetimeBufferSeconds = 0.5f;

        [Tooltip("Разовый (не Loop!) партикл дыма: мини-взрыв при смене тира. Корень префаба может быть контейнером без ParticleSystem — системы ищутся по детям.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Модель слайма: партикл спавнится её ребёнком и наследует масштаб роста, поэтому накрывает весь слайм.")]
        [SerializeField] private Transform _spawnAnchor;

        private PlayerTier _playerTier;

        [Inject]
        public void Construct(PlayerTier playerTier)
        {
            _playerTier = playerTier;
        }

        private void Awake()
        {
            if (_playerTier == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerTier was not injected. Check that GameLifetimeScope registers PlayerTier and TierUpFx.");
            }

            if (_prefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Particle prefab is not assigned. Drag a one-shot smoke ParticleSystem prefab into the _prefab field.");
            }

            if (_prefab.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                throw new InvalidOperationException(
                    $"{name}: the prefab '{_prefab.name}' has no ParticleSystem inside. Assign a particle prefab.");
            }

            foreach (ParticleSystem system in _prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system.main.loop == true)
                {
                    throw new InvalidOperationException(
                        $"{name}: particle system '{system.name}' in '{_prefab.name}' is looping. " +
                        "The tier-up smoke must be one-shot: uncheck Loop.");
                }
            }

            if (_spawnAnchor == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SpawnAnchor is not assigned. Drag the slime model transform into the _spawnAnchor field.");
            }
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            PlayBurstAsync().Forget();
        }

        private async UniTaskVoid PlayBurstAsync()
        {
            GameObject burstObject = Instantiate(_prefab, _spawnAnchor, false);
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                float duration = 0f;
                float maxLifetime = 0f;

                foreach (ParticleSystem system in burstObject.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = system.main;
                    duration = Mathf.Max(duration, main.duration);
                    maxLifetime = Mathf.Max(maxLifetime, main.startLifetime.constantMax);

                    if (system.isPlaying == false)
                    {
                        system.Play();
                    }
                }

                float lifetime = duration + maxLifetime + LifetimeBufferSeconds;

                await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (burstObject != null)
            {
                Destroy(burstObject);
            }
        }
    }
}
