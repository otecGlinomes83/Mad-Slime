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
        private bool _isSubscribed;

        private float _pull;

        public float Pull => _pull;

        [Inject]
        public void Construct(LevelProgress levelProgress, PlayerTier playerTier)
        {
            _levelProgress = levelProgress;
            _playerTier = playerTier;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraImpulseConfig is not assigned. Create a CameraImpulseConfig asset and drag it into the _config field.");
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
            if (Mathf.Approximately(_pull, 0f))
            {
                return;
            }

            float decay = 1f - Mathf.Exp(-_config.RecoverSpeed * Time.deltaTime);
            _pull = Mathf.Lerp(_pull, 0f, decay);

            if (Mathf.Abs(_pull) < 0.01f)
            {
                _pull = 0f;
            }
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
            float strength = _config.MassToPullStrength.Evaluate(definition.BaseMass);
            _pull = Mathf.Min(_pull + strength, _config.MaxPull);
        }

        private void OnTierChanged(ItemTier previousTier, ItemTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            _pull = Mathf.Max(_pull - _config.TierPushStrength, -_config.MaxPush);
        }
    }
}
