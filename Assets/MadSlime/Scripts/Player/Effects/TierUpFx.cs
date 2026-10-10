using Skills;
using System;
using UnityEngine;
using VContainer;

namespace Player
{
    public class TierUpFx : MonoBehaviour
    {
        [Tooltip("Разовая (не Loop) партикл-система сцены: мини-взрыв при смене тира. Код только включает проигрывание.")]
        [SerializeField] private ParticleSystem _burst;

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

            if (_burst == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Burst is not assigned. Drag a one-shot (not looping) ParticleSystem into the _burst field.");
            }

            if (_burst.main.loop == true)
            {
                throw new InvalidOperationException(
                    $"{name}: the assigned ParticleSystem is looping. The tier-up burst must be one-shot: uncheck Loop.");
            }

            _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnEnable()
        {
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _playerTier.TierChanged -= OnTierChanged;
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            _burst.Play(true);
        }
    }
}
