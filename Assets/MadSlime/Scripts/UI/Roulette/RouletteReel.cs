using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scriptables;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteReel : MonoBehaviour
    {
        private const float MinTickIntervalSeconds = 0.06f;
        private const int LoopNormalizationTurns = 64;

        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RouletteSectorCard _cardPrefab;
        [SerializeField, Min(1)] private int _visibleRowCount = 3;

        private readonly List<RouletteSectorCard> _cards = new List<RouletteSectorCard>();
        private readonly List<RouletteSectorView> _entries = new List<RouletteSectorView>();
        private readonly List<int> _cardEntryIndices = new List<int>();

        private RouletteConfig _config;
        private SfxPlayer _sfxPlayer;
        private float _cardHeight;
        private float _position;
        private int _lastTickIndex;
        private float _lastTickTime;
        private int _pendingTargetIndex;
        private bool _isSpinning;
        private bool _isBuilt;
        private bool _isIdleLoopStarted;
        private Action _spinCompleted;

        public bool IsSpinning => _isSpinning;

        public int EntryCount => _entries.Count;

        public void Setup(RouletteConfig config, SfxPlayer sfxPlayer)
        {
            _config = config;
            _sfxPlayer = sfxPlayer;
        }

        public void Build(List<RouletteSectorView> entries)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Setup was not called. Pass the RouletteConfig before building.");
            }

            if (_viewport == null || _content == null || _cardPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a reel part is not assigned. Drag the Viewport, Content and the RouletteSectorCard prefab into the fields.");
            }

            if (entries == null || entries.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Build requires at least one entry.");
            }

            _entries.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                _entries.Add(entries[i]);
            }

            EnsureCards();

            _position = 0f;
            _lastTickIndex = 0;
            _isBuilt = true;

            Reposition();
            StartIdleLoop();
        }

        public void Spin(int targetIndex, int entryCount, Action onComplete)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Setup was not called. Pass the RouletteConfig before spinning.");
            }

            if (_isBuilt == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Spin was called before Build.");
            }

            if (_isSpinning == true)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Spin was called while already spinning.");
            }

            if (targetIndex < 0 || targetIndex >= entryCount || entryCount != _entries.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetIndex),
                    targetIndex,
                    "RouletteReel.Spin received a target index outside the entry range.");
            }

            _isSpinning = true;
            _pendingTargetIndex = targetIndex;
            _spinCompleted = onComplete;

            DOTween.Kill(this);

            PlayClip(_config.SpinStartClip);

            int entryTotal = _entries.Count;
            int turns = Random.Range(_config.MinTurns, _config.MaxTurns + 1);
            float windBackPosition = _position + _config.WindBackCards;
            float delta = windBackPosition - targetIndex;
            delta = Mod(delta, entryTotal);
            float targetPosition = windBackPosition - (turns * entryTotal + delta);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            sequence.Append(DOTween.To(ReadPosition, ApplyPosition, windBackPosition, _config.WindBackDuration)
                .SetEase(Ease.InOutQuad));
            sequence.Append(DOTween.To(ReadPosition, ApplyPosition, targetPosition, _config.SpinDuration)
                .SetEase(EvaluateSpinEase));
            sequence.OnComplete(OnSpinCompleted);
        }

        private void EnsureCards()
        {
            if (_cards.Count > 0)
            {
                return;
            }

            _cardHeight = _viewport.rect.height / _visibleRowCount;
            int cardCount = _visibleRowCount + 2;

            for (int i = 0; i < cardCount; i++)
            {
                RouletteSectorCard card = Instantiate(_cardPrefab, _content);
                RectTransform cardTransform = (RectTransform)card.transform;

                cardTransform.anchorMin = new Vector2(0.5f, 0.5f);
                cardTransform.anchorMax = new Vector2(0.5f, 0.5f);
                cardTransform.pivot = new Vector2(0.5f, 0.5f);
                cardTransform.anchoredPosition = Vector2.zero;
                cardTransform.sizeDelta = new Vector2(_cardHeight, _cardHeight);
                cardTransform.localScale = Vector3.one;

                _cards.Add(card);
                _cardEntryIndices.Add(-1);
            }
        }

        private void StartIdleLoop()
        {
            if (_isIdleLoopStarted == true)
            {
                return;
            }

            _isIdleLoopStarted = true;
            RunIdleLoopAsync().Forget();
        }

        private async UniTaskVoid RunIdleLoopAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                while (true)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.IdleStepInterval),
                        DelayType.Realtime,
                        PlayerLoopTiming.Update,
                        cancellationToken);

                    if (_isBuilt == false || _isSpinning == true || isActiveAndEnabled == false)
                    {
                        continue;
                    }

                    IdleStep();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void IdleStep()
        {
            int entryTotal = _entries.Count;
            float normalizationLimit = entryTotal * LoopNormalizationTurns;

            if (_position < -normalizationLimit)
            {
                _position = Mod(_position, entryTotal);
                _lastTickIndex = Mathf.FloorToInt(_position);
            }

            float targetPosition = _position - 1f;

            DOTween.To(ReadPosition, ApplyPosition, targetPosition, _config.IdleStepDuration)
                .SetEase(Ease.OutBack)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Reposition()
        {
            int entryTotal = _entries.Count;
            int baseIndex = Mathf.FloorToInt(_position) - 1;

            for (int k = 0; k < _cards.Count; k++)
            {
                int entryIndex = Mod(baseIndex + k, entryTotal);
                RouletteSectorCard card = _cards[k];

                if (_cardEntryIndices[k] != entryIndex)
                {
                    card.Initialize(_entries[entryIndex]);
                    _cardEntryIndices[k] = entryIndex;
                }

                RectTransform cardTransform = (RectTransform)card.transform;
                Vector2 anchoredPosition = cardTransform.anchoredPosition;
                anchoredPosition.y = (baseIndex + k - _position) * _cardHeight;
                cardTransform.anchoredPosition = anchoredPosition;
            }
        }

        private void ApplyPosition(float value)
        {
            _position = value;
            Reposition();
            TryPlayStepSound();
        }

        private float ReadPosition()
        {
            return _position;
        }

        private float EvaluateSpinEase(float time, float duration, float overshootOrAmplitude, float period)
        {
            float normalized = time / duration;

            return 1f - Mathf.Pow(1f - normalized, _config.SpinEasePower);
        }

        private void TryPlayStepSound()
        {
            if (_config.StepClip == null || _sfxPlayer == null)
            {
                return;
            }

            int tickIndex = Mathf.FloorToInt(_position);

            if (tickIndex == _lastTickIndex || Time.unscaledTime - _lastTickTime < MinTickIntervalSeconds)
            {
                return;
            }

            _lastTickIndex = tickIndex;
            _lastTickTime = Time.unscaledTime;
            _sfxPlayer.PlayUi(_config.StepClip);
        }

        private void PlayClip(SfxClip clip)
        {
            if (clip == null || _sfxPlayer == null)
            {
                return;
            }

            _sfxPlayer.PlayUi(clip);
        }

        private void OnSpinCompleted()
        {
            int entryTotal = _entries.Count;
            _position = _pendingTargetIndex + entryTotal * Mathf.RoundToInt((_position - _pendingTargetIndex) / entryTotal);

            Reposition();

            _isSpinning = false;

            Action completed = _spinCompleted;
            _spinCompleted = null;
            completed?.Invoke();
        }

        private void OnDisable()
        {
            _isSpinning = false;
        }

        private static float Mod(float value, int modulus)
        {
            float remainder = value % modulus;

            if (remainder < 0f)
            {
                remainder += modulus;
            }

            return remainder;
        }

        private static int Mod(int value, int modulus)
        {
            int remainder = value % modulus;

            if (remainder < 0)
            {
                remainder += modulus;
            }

            return remainder;
        }
    }
}
