using System;
using Audio;
using Game;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UI
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;

        [SerializeField] private PauseMenu  _pauseMenu;
        [SerializeField] private LeaderboardMenu _leaderboardMenuPrefab;
        [SerializeField] private SfxClip _musicTrack;

        private AudioMixerController _audioMixerController;
        private MusicPlayer _musicPlayer;
        private Pauser _pauser;
        private Wallet _wallet;
        private LevelTransitor _levelTransitor;
        private IObjectResolver _resolver;
        private bool _isSubscribed;

        [Inject]
        public void Construct(Wallet wallet, LevelTransitor levelTransitor, Pauser pauser, AudioMixerController audioMixerController, MusicPlayer musicPlayer, IObjectResolver resolver)
        {
            _wallet = wallet;
            _levelTransitor = levelTransitor;
            _pauser = pauser;
            _audioMixerController = audioMixerController;
            _musicPlayer = musicPlayer;
            _resolver = resolver;
        }

        private void Awake()
        {
            if (_wallet == null || _levelTransitor == null)
            {
                throw new InvalidOperationException(
                    $"{name}: dependencies were not injected. MenuLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicPlayer == null || _resolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MusicPlayer or Resolver were not injected. MenuLifetimeScope must be the first object in the scene hierarchy.");
            }

            if (_musicTrack == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music track is not assigned. Drag a SfxClip asset into the _musicTrack field.");
            }

            if (_playButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayButton is not assigned. Drag a Button into the _playButton field.");
            }

            if (_shopButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: ShopButton is not assigned. Drag a Button into the _shopButton field.");
            }

            if (_leaderboardButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardButton is not assigned. Drag a Button into the _leaderboardButton field.");
            }

            if (_audioMixerController == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MixerController is not assigned. Drag an AudioMixerController into the _mixerController field.");
            }

            if (_leaderboardMenuPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LeaderboardMenuPrefab is not assigned. Drag a LeaderboardMenu prefab into the _leaderboardMenuPrefab field.");
            }

            if (_pauser == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Pauser is not assigned. Drag a Pauser into the _pauser field.");
            }
        }

        private void Start()
        {
            _musicPlayer.Play(_musicTrack);
        }

        private void OnEnable()
        {
            if (_wallet == null)
            {
                return;
            }

            if (_isSubscribed)
            {
                return;
            }

            _isSubscribed = true;

            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _shopButton.onClick.AddListener(OnShopClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }
        
        private void OnDisable()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _isSubscribed = false;

            _playButton.onClick.RemoveListener(OnPlayClicked);
            _shopButton.onClick.RemoveListener(OnShopClicked);
            _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        }

        private void OnSettingsClicked()
        {
            PauseMenu pauseMenu = _resolver.Instantiate(_pauseMenu);
            pauseMenu.Initialize(_pauser,_audioMixerController,false);
        }
        
        private void OnPlayClicked()
        {
            _levelTransitor.LoadGame();
        }

        private void OnShopClicked()
        {
            _levelTransitor.LoadShop();
        }

        private void OnLeaderboardClicked()
        {
            LeaderboardMenu leaderboardMenu = _resolver.Instantiate(_leaderboardMenuPrefab);
            leaderboardMenu.Initialize(_pauser);
        }
    }
}