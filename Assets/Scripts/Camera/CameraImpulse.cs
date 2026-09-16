using Game;
using Items;
using Player;
using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public sealed class CameraImpulse : MonoBehaviour
    {
        [SerializeField] private CameraImpulseConfig _config;

        private LevelProgress _levelProgress;
        private PlayerTier _playerTier;
        private TierTable _tierTable;
        private bool _isSubscribed;

        private float _pull;
        private float _shake;
        private float _fovKick;

        public float Pull => _pull;
        public float Shake => _shake;
        public float FovKick => _fovKick;

        [Inject]
        public void Construct(LevelProgress levelProgress, PlayerTier playerTier, TierTable tierTable)
        {
            _levelProgress = levelProgress;
            _playerTier = playerTier;
            _tierTable = tierTable;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraImpulseConfig is not assigned. Create a CameraImpulseConfig asset and drag it into the _config field.");
            }

            if (_tierTable == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierTable was not injected. Check that ProjectLifetimeScope has the TierTable asset assigned.");
            }
        }

        private void OnEnable()
        {
            SubscribeIfNeeded();
        }

        private void Start()
        {
            SubscribeIfNeeded();
        }

        private void OnDisable()
        {
            _isSubscribed = false;

            if (_levelProgress != null)
            {
                _levelProgress.ItemCollected -= OnItemCollected;
            }

            if (_playerTier != null)
            {
                _playerTier.TierChanged -= OnTierChanged;
            }
        }

        private void Update()
        {
            _pull = Decay(_pull, _config.RecoverSpeed);
            _shake = Decay(_shake, _config.ShakeRecoverSpeed);
            _fovKick = Decay(_fovKick, _config.FovRecoverSpeed);
        }

        private float Decay(float value, float recoverSpeed)
        {
            float decay = 1f - Mathf.Exp(-recoverSpeed * Time.deltaTime);
            float decayedValue = Mathf.Lerp(value, 0f, decay);

            if (Mathf.Abs(decayedValue) < 0.01f)
            {
                return 0f;
            }

            return decayedValue;
        }

        private void SubscribeIfNeeded()
        {
            if (_isSubscribed == true || _levelProgress == null || _playerTier == null)
            {
                return;
            }

            _isSubscribed = true;
            _levelProgress.ItemCollected += OnItemCollected;
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnItemCollected(ItemDefinition definition)
        {
            float mass = _tierTable.Get(definition.Tier).Mass;
            float strength = _config.MassToPullStrength.Evaluate(mass);
            _pull = Mathf.Min(_pull + strength, _config.MaxPull);

            if (mass < _config.ShakeMassThreshold)
            {
                return;
            }

            _shake = Mathf.Min(_shake + _config.ShakeStrength, _config.MaxShake);
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            _pull = Mathf.Max(_pull - _config.TierPushStrength, -_config.MaxPush);
            _fovKick = _config.TierFovKick;
        }
    }
}
