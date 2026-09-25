using Audio;
using Cysharp.Threading.Tasks;
using Game;
using Roulette;
using Scriptables;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Skins
{
    [RequireComponent(typeof(ModelPlacer))]
    [RequireComponent(typeof(Wallet))]
    public sealed class Shop : MonoBehaviour
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private Button _closeButton;
        [SerializeField] private SfxClip _musicTrack;

        private GameDirector _gameDirector;
        private ModelPlacer _placer;
        private MusicPlayer _musicPlayer;
        private Wallet _wallet;
        private ShopPanel _shopPanel;
        private RouletteService _rouletteService;
        private PlayerProgress _progress;
        private IObjectResolver _resolver;
        private bool _isInitialized;

        [Inject]
        public void Construct(MusicPlayer musicPlayer, ShopPanel shopPanel, ModelPlacer placer,
            GameDirector gameDirector, Wallet wallet, RouletteService rouletteService, PlayerProgress progress,
            IObjectResolver resolver)
        {
            _musicPlayer = musicPlayer;
            _shopPanel = shopPanel;
            _placer = placer;
            _gameDirector = gameDirector;
            _wallet = wallet;
            _rouletteService = rouletteService;
            _progress = progress;
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

            if (_gameDirector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: GameDirector was not injected. Check that ProjectLifetimeScope registers GameDirector and ShopLifetimeScope registers Shop.");
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

            if (_progress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerProgress was not injected. Check that ProjectLifetimeScope registers PlayerProgress and ShopLifetimeScope registers Shop.");
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
        }

        private void OnEnable()
        {
            _progress.Ready += OnSDKDataLoaded;

            _closeButton.onClick.AddListener(Close);
            _shopPanel.ViewSelected += OnViewSelected;

            if (_progress.IsReady)
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
            _progress.Ready -= OnSDKDataLoaded;

            _closeButton.onClick.RemoveListener(Close);
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
            _shopPanel.ShowSkinsTab();
            OnViewSelected(_shopPanel.SelectedView);

            _isInitialized = true;
        }

        private void Close()
        {
            NavigateToPrevious().Forget();
        }

        private async UniTaskVoid NavigateToPrevious()
        {
            if (_gameDirector.IsTransitioning == true)
            {
                return;
            }

            SceneId targetSceneId = _gameDirector.PreviousSceneId;

            if (targetSceneId == SceneId.Shop)
            {
                targetSceneId = SceneId.Menu;
            }

            await _gameDirector.LoadAsync(targetSceneId);
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
