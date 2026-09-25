using Audio;
using Game;
using Roulette;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using YG;

namespace Skins
{
    [RequireComponent(typeof(ModelPlacer))]
    [RequireComponent(typeof(LevelTransitor))]
    [RequireComponent(typeof(Wallet))]
    public sealed class Shop : MonoBehaviour
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _rouletteTabButton;
        [SerializeField] private Button _skinsRouletteButton;
        [SerializeField] private RouletteWindow _rouletteWindowPrefab;
        [SerializeField] private SfxClip _musicTrack;

        private LevelTransitor _levelTransitor;
        private ModelPlacer _placer;
        private MusicPlayer _musicPlayer;
        private Wallet _wallet;
        private ShopPanel _shopPanel;
        private RouletteService _rouletteService;
        private IObjectResolver _resolver;
        private bool _isInitialized;

        [Inject]
        public void Construct(MusicPlayer musicPlayer, ShopPanel shopPanel, ModelPlacer placer,
            LevelTransitor levelTransitor, Wallet wallet, RouletteService rouletteService, IObjectResolver resolver)
        {
            _musicPlayer = musicPlayer;
            _shopPanel = shopPanel;
            _placer = placer;
            _levelTransitor = levelTransitor;
            _wallet = wallet;
            _rouletteService = rouletteService;
            _resolver = resolver;
        }

        private void Awake()
        {
            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MusicPlayer was not injected. ShopLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_shopPanel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopPanel was not injected. Check that ShopLifetimeScope registers ShopPanel and Shop.");
            }

            if (_placer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ModelPlacer was not injected. Check that ShopLifetimeScope registers ModelPlacer and Shop.");
            }

            if (_levelTransitor == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelTransitor was not injected. Check that ShopLifetimeScope registers LevelTransitor and Shop.");
            }

            if (_wallet == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wallet was not injected. Check that ShopLifetimeScope registers Wallet and Shop.");
            }

            if (_rouletteService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteService was not injected. Check that ShopLifetimeScope registers RouletteService and Shop.");
            }

            if (_resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Resolver was not injected. ShopLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
            }

            if (_shopContent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopContent is not assigned. Drag a ShopContent asset into the _shopContent field.");
            }

            if (_closeButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CloseButton is not assigned. Drag a Button into the _closeButton field.");
            }

            if (_rouletteTabButton == null || _skinsRouletteButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a roulette button is not assigned. Drag the Roulette tab button and the Skins roulette button into the fields.");
            }

            if (_rouletteWindowPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWindow prefab is not assigned. Drag the RouletteWindow prefab into the _rouletteWindowPrefab field.");
            }
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSDKDataLoaded;

            _closeButton.onClick.AddListener(Close);
            _rouletteTabButton.onClick.AddListener(OnRouletteTabClicked);
            _skinsRouletteButton.onClick.AddListener(OnSkinsRouletteClicked);
            _shopPanel.ViewSelected += OnViewSelected;

            if (YG2.isSDKEnabled)
            {
                InitializeShop();
            }
        }

        private void Start()
        {
            _musicPlayer.Play(_musicTrack);
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSDKDataLoaded;

            _closeButton.onClick.RemoveListener(Close);
            _rouletteTabButton.onClick.RemoveListener(OnRouletteTabClicked);
            _skinsRouletteButton.onClick.RemoveListener(OnSkinsRouletteClicked);
            _shopPanel.ViewSelected -= OnViewSelected;
        }

        private void OnSDKDataLoaded()
        {
            InitializeShop();
        }

        private void InitializeShop()
        {
            if (_isInitialized)
            {
                return;
            }

            _shopPanel.Initialize(_shopContent.SkinItems, _rouletteService.Config.ExclusiveSkins);
            _shopPanel.ShowSkinsCategory();
            OnViewSelected(_shopPanel.SelectedView);

            _isInitialized = true;
        }

        private void OnRouletteTabClicked()
        {
            OpenRoulette(RouletteWindow.Mode.Main);
        }

        private void OnSkinsRouletteClicked()
        {
            OpenRoulette(RouletteWindow.Mode.Skins);
        }

        private void OpenRoulette(RouletteWindow.Mode mode)
        {
            RouletteWindow window = _resolver.Instantiate(_rouletteWindowPrefab);
            window.Initialize(mode, _shopContent.SkinItems);
        }

        private void Close()
        {
            string previousScene = YG2.saves.PreviousScene;
            _levelTransitor.LoadScene(previousScene);
        }

        private void OnViewSelected(ShopItemView view)
        {
            if (view == null)
            {
                return;
            }

            _placer.SetModel(view.Model);
            _placer.PlayWalk();
        }
    }
}
