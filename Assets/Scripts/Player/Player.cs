using Collectables;
using Game;
using Movement;
using PlayerInput;
using Scriptables;
using System;
using UnityEngine;
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

        [Inject]
        public void Construct(LevelProgress levelProgress, PlayerConfig playerConfig, TierTable tierTable,
            PlayerInputReader inputReader, Collector collector, ScalePunch collectPunch)
        {
            _levelProgress = levelProgress;
            _playerConfig = playerConfig;
            _tierTable = tierTable;
            _inputReader = inputReader;
            _collector = collector;
            _collectPunch = collectPunch;
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

            _mover = GetComponent<Mover>();
            _rotator = GetComponent<Rotator>();
            _playerTier = GetComponent<PlayerTier>();

            _mover.SetDefaultSpeed(_playerConfig.BaseMoveSpeed);
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

            _levelProgress.RegisterCollected(item.Definition);
            _playerTier.Add(_tierTable.Get(item.Definition.Tier).Mass);
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