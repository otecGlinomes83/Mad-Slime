using Audio;
using CameraSystem;
using Collectables;
using Game;
using Movement;
using Player;
using PlayerInput;
using Quota;
using Scriptables;
using System;
using System.Collections.Generic;
using UI;
using UI.Animations;
using UnityEngine;
using Upgrades;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private PlayerTier _playerTier;
        [SerializeField] private TierResolver _tierResolver;
        [SerializeField] private Movement.Movement _movement;
        [SerializeField] private Clamper _clamper;
        [SerializeField] private CrawlAnimator _crawlAnimator;
        [SerializeField] private ScalePunch _scalePunch;
        [SerializeField] private MassGainer _massGainer;
        [SerializeField] private CollectAvailability _collectAvailability;
        [SerializeField] private PlayerScaler _playerScaler;
        [SerializeField] private GrowthAnimator _growthAnimator;
        [SerializeField] private GameplayHud _gameplayHud;
        [SerializeField] private SpeedSmoke _speedSmoke;
        [SerializeField] private AdrenalineBoost _adrenalineBoost;
        [SerializeField] private AdrenalineSound _adrenalineSound;
        [SerializeField] private FinalCountdownVignette _finalCountdownVignette;
        [SerializeField] private GameplaySessionHandler _sessionHandler;
        [SerializeField] private QuotaUI _quotaUI;
        [SerializeField] private CameraPullAnimator _cameraPullAnimator;
        [SerializeField] private CameraShakeAnimator _cameraShakeAnimator;
        [SerializeField] private CameraFovKickAnimator _cameraFovKickAnimator;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private SkinApplier _skinApplier;
        [SerializeField] private GameplayUIFabric _gameplayUIFabric;
        [SerializeField] private PlayerPickupSound _playerPickupSound;
        [SerializeField] private TierUpSound _tierUpSound;
        [SerializeField] private TierUpFx _tierUpFx;
        [SerializeField] private TimerTickSound _timerTickSound;
        [SerializeField] private GrowthBarView _growthBarView;
        [SerializeField] private TimerUI _timerUI;
        [SerializeField] private ItemCollector _itemCollector;
        [SerializeField] private Absorber _absorber;
        [SerializeField] private ItemDetector _itemDetector;
        [SerializeField] private AttractableDetector _attractableDetector;
        [SerializeField] private ItemAttractor _itemAttractor;
        [SerializeField] private ItemGhostToggler _itemGhostToggler;
        [SerializeField] private QuotaItemHighlighter _quotaItemHighlighter;
        [SerializeField] private SmellAbilityActivator _smellAbilityActivator;
        [SerializeField] private Pauser _pauser;
        [SerializeField] private Timer _timer;
        [SerializeField] private PlayerInputReader _inputReader;

        [SerializeField] private List<UIButtonSound> _sceneButtonSounds;

        protected override void Configure(IContainerBuilder builder)
        {
            ValidateAll();

            builder.RegisterInstance(_playerConfig);
            builder.Register<CollectedItemsCounter>(Lifetime.Scoped);
            builder.Register<QuotaGenerator>(Lifetime.Scoped);
            builder.Register<UiSpawner>(Lifetime.Scoped);

            builder.RegisterComponent(_levelGenerator);
            builder.RegisterComponent(_playerTier);
            builder.RegisterComponent(_tierResolver);
            builder.RegisterComponent(_movement);
            builder.RegisterComponent(_clamper);
            builder.RegisterComponent(_crawlAnimator);
            builder.RegisterComponent(_scalePunch);
            builder.RegisterComponent(_massGainer);
            builder.RegisterComponent(_collectAvailability);
            builder.RegisterComponent(_playerScaler);
            builder.RegisterComponent(_growthAnimator);
            builder.RegisterComponent(_gameplayHud);
            builder.RegisterComponent(_speedSmoke);
            builder.RegisterComponent(_adrenalineBoost);
            builder.RegisterComponent(_adrenalineSound);
            builder.RegisterComponent(_finalCountdownVignette);
            builder.RegisterComponent(_sessionHandler);
            builder.RegisterComponent(_quotaUI);
            builder.RegisterComponent(_cameraPullAnimator);
            builder.RegisterComponent(_cameraShakeAnimator);
            builder.RegisterComponent(_cameraFovKickAnimator);
            builder.RegisterComponent(_cameraFollow);
            builder.RegisterComponent(_skinApplier);
            builder.RegisterComponent(_gameplayUIFabric);
            builder.RegisterComponent(_playerPickupSound);
            builder.RegisterComponent(_tierUpSound);
            builder.RegisterComponent(_tierUpFx);
            builder.RegisterComponent(_timerTickSound);
            builder.RegisterComponent(_growthBarView);
            builder.RegisterComponent(_timerUI);
            builder.RegisterComponent(_itemCollector);
            builder.RegisterComponent(_absorber);
            builder.RegisterComponent(_itemDetector);
            builder.RegisterComponent(_attractableDetector);
            builder.RegisterComponent(_itemAttractor);
            builder.RegisterComponent(_itemGhostToggler);
            builder.RegisterComponent(_quotaItemHighlighter);
            builder.RegisterComponent(_smellAbilityActivator);
            builder.RegisterComponent(_pauser);
            builder.RegisterComponent(_timer);
            builder.RegisterComponent(_inputReader);

            builder.RegisterBuildCallback(InitializeScene);
        }

        private void InitializeScene(IObjectResolver container)
        {
            container.Resolve<CollectedItemsCounter>();
            container.Resolve<SceneNavigator>().EnsureInitialized();

            for (int i = 0; i < _sceneButtonSounds.Count; i++)
            {
                container.Inject(_sceneButtonSounds[i]);
            }
        }

        private void ValidateAll()
        {
            ValidateAssigned(_playerConfig, nameof(_playerConfig));
            ValidateAssigned(_levelGenerator, nameof(_levelGenerator));
            ValidateAssigned(_playerTier, nameof(_playerTier));
            ValidateAssigned(_tierResolver, nameof(_tierResolver));
            ValidateAssigned(_movement, nameof(_movement));
            ValidateAssigned(_clamper, nameof(_clamper));
            ValidateAssigned(_crawlAnimator, nameof(_crawlAnimator));
            ValidateAssigned(_scalePunch, nameof(_scalePunch));
            ValidateAssigned(_massGainer, nameof(_massGainer));
            ValidateAssigned(_collectAvailability, nameof(_collectAvailability));
            ValidateAssigned(_playerScaler, nameof(_playerScaler));
            ValidateAssigned(_growthAnimator, nameof(_growthAnimator));
            ValidateAssigned(_gameplayHud, nameof(_gameplayHud));
            ValidateAssigned(_speedSmoke, nameof(_speedSmoke));
            ValidateAssigned(_adrenalineBoost, nameof(_adrenalineBoost));
            ValidateAssigned(_adrenalineSound, nameof(_adrenalineSound));
            ValidateAssigned(_finalCountdownVignette, nameof(_finalCountdownVignette));
            ValidateAssigned(_sessionHandler, nameof(_sessionHandler));
            ValidateAssigned(_quotaUI, nameof(_quotaUI));
            ValidateAssigned(_cameraPullAnimator, nameof(_cameraPullAnimator));
            ValidateAssigned(_cameraShakeAnimator, nameof(_cameraShakeAnimator));
            ValidateAssigned(_cameraFovKickAnimator, nameof(_cameraFovKickAnimator));
            ValidateAssigned(_cameraFollow, nameof(_cameraFollow));
            ValidateAssigned(_skinApplier, nameof(_skinApplier));
            ValidateAssigned(_gameplayUIFabric, nameof(_gameplayUIFabric));
            ValidateAssigned(_playerPickupSound, nameof(_playerPickupSound));
            ValidateAssigned(_tierUpSound, nameof(_tierUpSound));
            ValidateAssigned(_tierUpFx, nameof(_tierUpFx));
            ValidateAssigned(_timerTickSound, nameof(_timerTickSound));
            ValidateAssigned(_growthBarView, nameof(_growthBarView));
            ValidateAssigned(_timerUI, nameof(_timerUI));
            ValidateAssigned(_itemCollector, nameof(_itemCollector));
            ValidateAssigned(_absorber, nameof(_absorber));
            ValidateAssigned(_itemDetector, nameof(_itemDetector));
            ValidateAssigned(_attractableDetector, nameof(_attractableDetector));
            ValidateAssigned(_itemAttractor, nameof(_itemAttractor));
            ValidateAssigned(_itemGhostToggler, nameof(_itemGhostToggler));
            ValidateAssigned(_quotaItemHighlighter, nameof(_quotaItemHighlighter));
            ValidateAssigned(_smellAbilityActivator, nameof(_smellAbilityActivator));
            ValidateAssigned(_pauser, nameof(_pauser));
            ValidateAssigned(_timer, nameof(_timer));
            ValidateAssigned(_inputReader, nameof(_inputReader));
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
