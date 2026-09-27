using Cysharp.Threading.Tasks;
using Game;
using PlayerInput;
using System;
using System.Threading;
using Timer = Game.Timer;
using UnityEngine;
using VContainer;

namespace UI.Animations
{
    public sealed class UiEnableScheduler : MonoBehaviour
    {
        [Tooltip("Включаются при старте сцены, каждый со своей задержкой.")]
        [SerializeField] private UiEnableTarget[] _sceneStartTargets;

        [Tooltip("Включаются при первом касании — на старте игровой сессии.")]
        [SerializeField] private UiEnableTarget[] _sessionStartTargets;

        private LevelProgress _levelProgress;
        private Timer _timer;
        private PlayerInputReader _inputReader;
        private bool _isSessionStarted;
        private bool _isSubscribed;

        [Inject]
        public void Construct(LevelProgress levelProgress, Timer timer, PlayerInputReader inputReader)
        {
            _levelProgress = levelProgress;
            _timer = timer;
            _inputReader = inputReader;
        }

        private void Awake()
        {
            ValidateTargets(_sceneStartTargets, "SceneStartTargets");
            ValidateTargets(_sessionStartTargets, "SessionStartTargets");

            if (_levelProgress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelProgress was not injected. Check that GameLifetimeScope registers UiEnableScheduler.");
            }

            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers UiEnableScheduler.");
            }

            if (_inputReader == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerInputReader was not injected. Check that GameLifetimeScope registers UiEnableScheduler.");
            }
        }

        private void Start()
        {
            PlaySequenceAsync(_sceneStartTargets).Forget();

            _timer.Finished += OnSessionFinished;
            _levelProgress.QuotaCompleted += OnSessionFinished;
            _inputReader.MovementKeyPressed += OnMovementKeyPressed;
            _isSubscribed = true;
        }

        private void OnDestroy()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _isSubscribed = false;
            _timer.Finished -= OnSessionFinished;
            _levelProgress.QuotaCompleted -= OnSessionFinished;
            _inputReader.MovementKeyPressed -= OnMovementKeyPressed;
        }

        private void OnMovementKeyPressed()
        {
            if (_isSessionStarted == true)
            {
                return;
            }

            _isSessionStarted = true;
            PlaySequenceAsync(_sessionStartTargets).Forget();
        }

        private void OnSessionFinished()
        {
            FinishTargets(_sceneStartTargets);
            FinishTargets(_sessionStartTargets);
        }

        private async UniTaskVoid PlaySequenceAsync(UiEnableTarget[] targets)
        {
            CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();

            try
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    float delay = targets[i].Delay;

                    if (delay > 0f)
                    {
                        await UniTask.Delay
                        (
                            (int)(delay * 1000f),
                            DelayType.Realtime,
                            cancellationToken: destroyToken
                        );
                    }

                    targets[i].Target.SetActive(true);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void FinishTargets(UiEnableTarget[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                GameObject target = targets[i].Target;

                if (target.activeSelf == false)
                {
                    continue;
                }

                UiEnableAnimation animation = target.GetComponent<UiEnableAnimation>();

                if (animation != null)
                {
                    animation.PlayOutro();
                }
                else
                {
                    target.SetActive(false);
                }
            }
        }

        private void ValidateTargets(UiEnableTarget[] targets, string fieldName)
        {
            if (targets == null || targets.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: {fieldName} is empty. Run Mad Slime → Setup Game FX or fill the list manually.");
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].Target == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: {fieldName} element {i} is empty. Assign a GameObject to every slot.");
                }
            }
        }
    }
}
