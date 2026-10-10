using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game;
using UI.Animations;
using UnityEngine;
using VContainer;

namespace UI
{
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] private UiEnableTarget[] _sceneStartTargets;
        [SerializeField] private UiEnableTarget[] _sessionStartTargets;

        private GameplaySessionHandler _session;
        private Dictionary<GameObject, UiEnableAnimation> _animations = new Dictionary<GameObject, UiEnableAnimation>();
        private CancellationTokenSource _sceneSequence;
        private CancellationTokenSource _gameplaySequence;
        private bool _isGameplayShown;

        [Inject]
        public void Construct(GameplaySessionHandler session)
        {
            _session = session;
        }

        private void Awake()
        {
            RegisterTargets(_sceneStartTargets);
            RegisterTargets(_sessionStartTargets);
        }

        private void OnEnable()
        {
            _session.SceneSessionPrepared += OnPrepared;
            _session.SceneSessionStarted += OnStarted;
            _session.GameplayStarted += OnGameplayStarted;
            _session.SessionFinished += OnFinished;

            foreach (UiEnableAnimation animation in _animations.Values)
            {
                animation.OutroCompleted += OnTargetHidden;
            }
        }

        private void OnDisable()
        {
            _session.SceneSessionPrepared -= OnPrepared;
            _session.SceneSessionStarted -= OnStarted;
            _session.GameplayStarted -= OnGameplayStarted;
            _session.SessionFinished -= OnFinished;
            CancelSequences();

            foreach (UiEnableAnimation animation in _animations.Values)
            {
                animation.OutroCompleted -= OnTargetHidden;
                animation.Cancel();
            }
        }

        private void OnPrepared()
        {
            CancelSequences();
            _isGameplayShown = false;

            foreach (GameObject target in _animations.Keys)
            {
                target.SetActive(false);
            }
        }

        private void OnStarted()
        {
            _sceneSequence = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ShowSequenceAsync(_sceneStartTargets, _sceneSequence.Token).Forget();
        }

        private void OnGameplayStarted()
        {
            if (_isGameplayShown)
            {
                return;
            }

            _isGameplayShown = true;
            _gameplaySequence = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ShowSequenceAsync(_sessionStartTargets, _gameplaySequence.Token).Forget();
        }

        private void OnFinished()
        {
            CancelSequences();

            foreach (KeyValuePair<GameObject, UiEnableAnimation> target in _animations)
            {
                if (target.Key.activeSelf)
                {
                    target.Value.PlayOutro();
                }
            }
        }

        private async UniTaskVoid ShowSequenceAsync(UiEnableTarget[] targets, CancellationToken cancellationToken)
        {
            try
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(targets[i].Delay), DelayType.Realtime,
                        cancellationToken: cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    GameObject target = targets[i].Target;
                    target.SetActive(true);
                    _animations[target].PlayIntro();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void OnTargetHidden(UiEnableAnimation animation)
        {
            animation.gameObject.SetActive(false);
        }

        private void RegisterTargets(UiEnableTarget[] targets)
        {
            if (targets == null)
            {
                throw new InvalidOperationException($"{name}: HUD target list is required.");
            }

            for (int i = 0; i < targets.Length; i++)
            {
                GameObject target = targets[i].Target;

                if (target == null || target.TryGetComponent(out UiEnableAnimation animation) == false)
                {
                    throw new InvalidOperationException($"{name}: HUD target {i} requires UiEnableAnimation.");
                }

                _animations[target] = animation;
            }
        }

        private void CancelSequences()
        {
            _sceneSequence?.Cancel();
            _sceneSequence?.Dispose();
            _sceneSequence = null;
            _gameplaySequence?.Cancel();
            _gameplaySequence?.Dispose();
            _gameplaySequence = null;
        }
    }
}
