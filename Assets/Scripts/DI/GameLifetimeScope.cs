using Audio;
using CameraSystem;
using Collectables;
using Game;
using Movement;
using Player;
using PlayerInput;
using Scriptables;
using System;
using UI;
using UnityEngine;
using VContainer;
using PlayerComponent = Player.Player;

namespace DI
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private TierResolver _tierResolver;
        [SerializeField] private Mover _mover;
        [SerializeField] private ScalePunch _scalePunch;
        [SerializeField] private LevelScaler _levelScaler;
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private QuotaUI _quotaUI;
        [SerializeField] private CameraImpulse _cameraImpulse;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private SkinApplier _skinApplier;
        [SerializeField] private LevelLabelUI _levelLabelUI;
        [SerializeField] private GameplayUIFabric _gameplayUIFabric;
        [SerializeField] private PlayerPickupSound _playerPickupSound;
        [SerializeField] private TierUpSound _tierUpSound;
        [SerializeField] private TimerTickSound _timerTickSound;
        [SerializeField] private GrowthBarView _growthBarView;
        [SerializeField] private MassUI _massUI;
        [SerializeField] private TimerUI _timerUI;
        [SerializeField] private Collector _collector;
        [SerializeField] private Absorber _absorber;
        [SerializeField] private ItemDetector _itemDetector;
        [SerializeField] private AttractableDetector _attractableDetector;
        [SerializeField] private ItemAttractor _itemAttractor;
        [SerializeField] private ItemGhostToggler _itemGhostToggler;
        [SerializeField] private AudioMixerController _audioMixerController;
        [SerializeField] private SoundLimiter _soundLimiter;
        [SerializeField] private UIButtonSound[] _uiButtonSounds;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private LevelTransitor _levelTransitor;
        [SerializeField] private Timer _timer;
        [SerializeField] private PlayerInputReader _inputReader;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAssigned(_playerConfig, nameof(_playerConfig));
            ValidateAssigned(_levelGenerator, nameof(_levelGenerator));
            ValidateAssigned(_player, nameof(_player));
            ValidateAssigned(_playerTier, nameof(_playerTier));
            ValidateAssigned(_tierResolver, nameof(_tierResolver));
            ValidateAssigned(_mover, nameof(_mover));
            ValidateAssigned(_scalePunch, nameof(_scalePunch));
            ValidateAssigned(_levelScaler, nameof(_levelScaler));
            ValidateAssigned(_sessionHandler, nameof(_sessionHandler));
            ValidateAssigned(_quotaUI, nameof(_quotaUI));
            ValidateAssigned(_cameraImpulse, nameof(_cameraImpulse));
            ValidateAssigned(_cameraFollow, nameof(_cameraFollow));
            ValidateAssigned(_skinApplier, nameof(_skinApplier));
            ValidateAssigned(_levelLabelUI, nameof(_levelLabelUI));
            ValidateAssigned(_gameplayUIFabric, nameof(_gameplayUIFabric));
            ValidateAssigned(_playerPickupSound, nameof(_playerPickupSound));
            ValidateAssigned(_tierUpSound, nameof(_tierUpSound));
            ValidateAssigned(_timerTickSound, nameof(_timerTickSound));
            ValidateAssigned(_growthBarView, nameof(_growthBarView));
            ValidateAssigned(_massUI, nameof(_massUI));
            ValidateAssigned(_timerUI, nameof(_timerUI));
            ValidateAssigned(_collector, nameof(_collector));
            ValidateAssigned(_absorber, nameof(_absorber));
            ValidateAssigned(_itemDetector, nameof(_itemDetector));
            ValidateAssigned(_attractableDetector, nameof(_attractableDetector));
            ValidateAssigned(_itemAttractor, nameof(_itemAttractor));
            ValidateAssigned(_itemGhostToggler, nameof(_itemGhostToggler));
            ValidateAssigned(_audioMixerController, nameof(_audioMixerController));
            ValidateAssigned(_soundLimiter, nameof(_soundLimiter));
            ValidateButtons(_uiButtonSounds);
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_levelTransitor, nameof(_levelTransitor));
            ValidateAssigned(_timer, nameof(_timer));
            ValidateAssigned(_inputReader, nameof(_inputReader));

            builder.RegisterInstance(_playerConfig);
            builder.Register<ItemPool>(Lifetime.Scoped);
            builder.Register<QuotaGenerator>(Lifetime.Scoped);

            builder.RegisterComponent(_levelGenerator);
            builder.RegisterComponent(_player);
            builder.RegisterComponent(_playerTier);
            builder.RegisterComponent(_tierResolver);
            builder.RegisterComponent(_mover);
            builder.RegisterComponent(_scalePunch);
            builder.RegisterComponent(_levelScaler);
            builder.RegisterComponent(_sessionHandler);
            builder.RegisterComponent(_quotaUI);
            builder.RegisterComponent(_cameraImpulse);
            builder.RegisterComponent(_cameraFollow);
            builder.RegisterComponent(_skinApplier);
            builder.RegisterComponent(_levelLabelUI);
            builder.RegisterComponent(_gameplayUIFabric);
            builder.RegisterComponent(_playerPickupSound);
            builder.RegisterComponent(_tierUpSound);
            builder.RegisterComponent(_timerTickSound);
            builder.RegisterComponent(_growthBarView);
            builder.RegisterComponent(_massUI);
            builder.RegisterComponent(_timerUI);
            builder.RegisterComponent(_collector);
            builder.RegisterComponent(_absorber);
            builder.RegisterComponent(_itemDetector);
            builder.RegisterComponent(_attractableDetector);
            builder.RegisterComponent(_itemAttractor);
            builder.RegisterComponent(_itemGhostToggler);
            builder.RegisterComponent(_audioMixerController);
            builder.RegisterComponent(_soundLimiter);
            for (int index = 0; index < _uiButtonSounds.Length; index++)
            {
                builder.RegisterComponent(_uiButtonSounds[index]);
            }
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_levelTransitor);
            builder.RegisterComponent(_timer);
            builder.RegisterComponent(_inputReader);
        }

        private void ValidateAssigned(object dependency, string fieldName)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"GameLifetimeScope: '{fieldName}' is not assigned. Select the DI object in the scene and drag the missing reference.");
            }
        }

        private void ValidateButtons(UIButtonSound[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
            {
                throw new InvalidOperationException(
                    $"GameLifetimeScope: '{nameof(_uiButtonSounds)}' is empty. Drag every UIButtonSound component of the scene into the list.");
            }

            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] == null)
                {
                    throw new InvalidOperationException(
                        $"GameLifetimeScope: '{nameof(_uiButtonSounds)}' element {index} is empty. Drag a UIButtonSound component into every slot.");
                }
            }
        }
    }
}
