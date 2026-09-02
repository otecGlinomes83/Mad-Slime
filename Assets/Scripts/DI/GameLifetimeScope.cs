using CameraSystem;
using Game;
using Player;
using Scriptables;
using Skills;
using System;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using PlayerComponent = Player.Player;
namespace DI
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private QuotaUI _quotaUI;
        [SerializeField] private CameraImpulse _cameraImpulse;
        [SerializeField] private SkinApplier _skinApplier;
        [SerializeField] private LevelLabelUI _levelLabelUI;
        [SerializeField] private SkillUnlocker _skillUnlocker;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_playerConfig, nameof(_playerConfig));
            ValidateAssigned(_levelGenerator, nameof(_levelGenerator));
            ValidateAssigned(_player, nameof(_player));
            ValidateAssigned(_playerTier, nameof(_playerTier));
            ValidateAssigned(_sessionHandler, nameof(_sessionHandler));
            ValidateAssigned(_quotaUI, nameof(_quotaUI));
            ValidateAssigned(_cameraImpulse, nameof(_cameraImpulse));
            ValidateAssigned(_skinApplier, nameof(_skinApplier));
            ValidateAssigned(_levelLabelUI, nameof(_levelLabelUI));
            ValidateAssigned(_skillUnlocker, nameof(_skillUnlocker));

            builder.RegisterInstance(_playerConfig);
            builder.Register<ItemPool>(Lifetime.Scoped);
            builder.Register<QuotaGenerator>(Lifetime.Scoped);

            builder.RegisterComponent(_levelGenerator);
            builder.RegisterComponent(_player);
            builder.RegisterComponent(_playerTier);
            builder.RegisterComponent(_sessionHandler);
            builder.RegisterComponent(_quotaUI);
            builder.RegisterComponent(_cameraImpulse);
            builder.RegisterComponent(_skinApplier);
            builder.RegisterComponent(_levelLabelUI);
            builder.RegisterComponent(_skillUnlocker);
        }

        private void ValidateAssigned(object dependency, string fieldName)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"GameLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }
    }
}
