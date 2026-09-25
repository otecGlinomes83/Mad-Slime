using Collectables;
using Game;
using Movement;
using PlayerInput;
using Scriptables;
using System;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Player
{
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(Rotator))]
    [RequireComponent(typeof(PlayerTier))]
    public sealed class Player : MonoBehaviour
    {
        private Mover _mover;
        private Rotator _rotator;
        private PlayerTier _playerTier;
        private LevelProgress _levelProgress;
        private PlayerConfig _playerConfig;
        private TierTable _tierTable;
        private PlayerInputReader _inputReader;
        private Collector _collector;
        private ScalePunch _collectPunch;
        private PlayerUpgrades _upgrades;
        private CollectBurstSpawner _collectBurst;

        [Inject]
        public void Construct(LevelProgress levelProgress, PlayerConfig playerConfig, TierTable tierTable,
            PlayerInputReader inputReader, Collector collector, ScalePunch collectPunch,
            PlayerUpgrades upgrades, CollectBurstSpawner collectBurst)
        {
            _levelProgress = levelProgress;
            _playerConfig = playerConfig;
            _tierTable = tierTable;
            _inputReader = inputReader;
            _collector = collector;
            _collectPunch = collectPunch;
            _upgrades = upgrades;
            _collectBurst = collectBurst;
        }

        private void Awake()
        {
            if (_playerConfig == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope is configured and Player is registered.");
            }

            if (_tierTable == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierTable was not injected. Check that ProjectLifetimeScope has the TierTable asset assigned.");
            }

            if (_inputReader == null)
            {
                throw new InvalidOperationException(
                    $"{name}: InputReader was not injected. Check that GameLifetimeScope registers PlayerInputReader and Player.");
            }

            if (_collector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collector was not injected. Check that GameLifetimeScope registers Collector and Player.");
            }

            if (_collectPunch == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CollectPunch was not injected. Check that GameLifetimeScope registers ScalePunch and Player.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that ProjectLifetimeScope registers PlayerUpgrades.");
            }

            if (_collectBurst == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CollectBurstSpawner was not injected. Check that GameLifetimeScope registers CollectBurstSpawner and Player.");
            }

            _mover = GetComponent<Mover>();
            _rotator = GetComponent<Rotator>();
            _playerTier = GetComponent<PlayerTier>();

            _mover.SetDefaultSpeed(_playerConfig.BaseMoveSpeed * _upgrades.SpeedMultiplier);
            _mover.SetSmoothTime(_playerConfig.MoveSmoothTime);
            _rotator.SetSpeed(_playerConfig.RotationSpeed);
        }

        private void OnEnable()
        {
            _collector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _collector.ItemCollected -= OnItemCollected;
        }

        private void Update()
        {
            Vector3 moveDirection = ConvertToWorldDirection(_inputReader.MoveInput);

            _mover.Move(moveDirection);
            _rotator.Rotate(moveDirection);
        }

        private void OnItemCollected(Items.Item item)
        {
            if (item.Definition.Tier == _playerTier.CurrentTier)
            {
                _collectPunch.Punch();
            }

            bool isQuota = _levelProgress.IsQuotaItem(item.Definition);
            float massMultiplier = isQuota == true ? _upgrades.QuotaMassMultiplier : _upgrades.MassMultiplier;
            int mass = Mathf.Max(1, Mathf.RoundToInt(_tierTable.Get(item.Definition.Tier).Mass * massMultiplier));

            _collectBurst.Play(item.transform.position, 1f + (int)item.Definition.Tier * 0.5f);

            float fillWeight = isQuota == true ? 1f : _upgrades.ForeignFillMultiplier;

            _levelProgress.RegisterCollected(item.Definition, fillWeight);
            _playerTier.Add(mass);
        }

        private Vector3 ConvertToWorldDirection(Vector2 input)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return forward * input.y + right * input.x;
        }
    }
}