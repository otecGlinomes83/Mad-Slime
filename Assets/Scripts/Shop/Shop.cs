using Audio;
using Scriptables;
using System;
using Game;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
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
        [SerializeField] private SfxClip _musicTrack;

        private LevelTransitor _levelTransitor;
        private ModelPlacer _placer;
        private MusicPlayer _musicPlayer;
        private Wallet _wallet;
        private ShopPanel _shopPanel;
        private bool _isInitialized;

        [Inject]
        public void Construct(MusicPlayer musicPlayer, ShopPanel shopPanel, ModelPlacer placer,
            LevelTransitor levelTransitor, Wallet wallet)
        {
            _musicPlayer = musicPlayer;
            _shopPanel = shopPanel;
            _placer = placer;
            _levelTransitor = levelTransitor;
            _wallet = wallet;
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
            YG2.onGetSDKData += OnSDKDataLoaded;

            _closeButton.onClick.AddListener(Close);
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

            _shopPanel.Initialize();
            _shopPanel.Show(_shopContent.SkinItems);
            OnViewSelected(_shopPanel.SelectedView);

            _isInitialized = true;
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