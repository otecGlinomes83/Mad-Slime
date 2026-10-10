using Audio;
using Cysharp.Threading.Tasks;
using Scriptables;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game
{
    public class ShopSessionHandler : MonoBehaviour
    {
        [SerializeField] private Playlist _musicPlaylist;
        [SerializeField] private Button _closeButton;

        public event Action Prepared;

        public event Action Started;

        public event Action Finished;

        private CancellationTokenSource _sessionSource;
        private bool _isFinished;

        private IMusicPlayer _musicPlayer;
        private SceneNavigator _sceneNavigator;

        [Inject]
        public void Construct(IMusicPlayer musicPlayer, SceneNavigator sceneNavigator)
        {
            _musicPlayer = musicPlayer;
            _sceneNavigator = sceneNavigator;
        }

        private void Awake()
        {
            if (_musicPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IMusicPlayer was not injected. Check that ProjectLifetimeScope registers AudioPlayer.");
            }

            if (_sceneNavigator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SceneNavigator was not injected. Check that ProjectLifetimeScope registers SceneNavigator.");
            }

            if (_musicPlaylist == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Music playlist is not assigned. Drag a Playlist asset into the _musicPlaylist field.");
            }
        }

        private void OnEnable()
        {
            _sessionSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _closeButton.onClick.AddListener(Exit);
        }

        private void OnDisable()
        {
            FinishSession();
            _sceneNavigator.UnregisterSession(RunSession);
            _sessionSource.Dispose();
            _sessionSource = null;
            _closeButton.onClick.RemoveListener(Exit);
        }

        public void Exit()
        {
            if (_sceneNavigator.IsTransitioning == true)
            {
                return;
            }

            SceneId previousSceneId = _sceneNavigator.PreviousSceneId;

            if (previousSceneId == SceneId.Game)
            {
                NavigateToGame().Forget();
                return;
            }

            NavigateToMenu().Forget();
        }

        private void Start()
        {
            _sceneNavigator.RegisterSession(PrepareSessionAsync, RunSession, FinishSession, _sessionSource.Token);
        }

        private UniTask PrepareSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Prepared?.Invoke();
            return UniTask.CompletedTask;
        }

        private void FinishSession()
        {
            if (_isFinished)
            {
                return;
            }

            _isFinished = true;
            _sessionSource?.Cancel();
            Finished?.Invoke();
        }

        private void RunSession()
        {
            _musicPlayer.Play(_musicPlaylist);
            Started?.Invoke();
        }

        private async UniTaskVoid NavigateToGame()
        {
            await _sceneNavigator.LoadGameAsync();
        }

        private async UniTaskVoid NavigateToMenu()
        {
            await _sceneNavigator.LoadMenuAsync();
        }
    }
}
